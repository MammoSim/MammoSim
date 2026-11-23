"""Pruebas de GET .../progreso y de que auth no deje el texto del error en los registros."""
from datetime import datetime, timezone
from types import SimpleNamespace

import pytest
from fastapi import HTTPException

from app import auth as auth_mod

BASE = "/sesiones/s1/casos/caso_001"
PRIVADOS_PROGRESO = ("respuesta_correcta", "explicacion", "acierto", "correcta", "opcion_ia", "hallazgos")


def abre_sesion(db, id_sesion="s1", uid="uid_1", estado="en_curso"):
    db.collection("sesiones").document(id_sesion).set(
        {
            "id_sesion": id_sesion, "id_usuario": uid, "id_curso": None,
            "casos_asignados": ["caso_001"], "estado": estado,
            "fecha_inicio": datetime(2026, 11, 1, tzinfo=timezone.utc), "fecha_fin": None,
        }
    )


def responde(c, n, opcion="A"):
    return c.post(f"{BASE}/respuestas", json={"numero_pregunta": n, "opcion_elegida": opcion})


def test_sin_nada_contestado_empieza_en_la_pregunta_1(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    r = c.get(f"{BASE}/progreso")
    assert r.status_code == 200
    assert r.json() == {
        "id_caso": "caso_001", "preguntas_respondidas": [], "siguiente_pregunta": 1,
        "decision_inicial_guardada": False, "decision_final_guardada": False,
        "decision_inicial": None, "decision_final": None,
    }


def test_tras_contestar_dos_sigue_en_la_3(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde(c, 1)
    responde(c, 2)
    cuerpo = c.get(f"{BASE}/progreso").json()
    assert cuerpo["preguntas_respondidas"] == [1, 2] and cuerpo["siguiente_pregunta"] == 3


def test_con_las_7_contestadas_ya_no_hay_siguiente_pregunta(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    for n in range(1, 8):
        responde(c, n)
    cuerpo = c.get(f"{BASE}/progreso").json()
    assert cuerpo["siguiente_pregunta"] is None and len(cuerpo["preguntas_respondidas"]) == 7


def test_refleja_las_decisiones_guardadas(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    for n in range(1, 8):
        responde(c, n)
    c.post(f"{BASE}/decision-inicial", json={"decision": "seguimiento"})
    cuerpo = c.get(f"{BASE}/progreso").json()
    assert cuerpo["decision_inicial_guardada"] is True and cuerpo["decision_inicial"] == "seguimiento"
    assert cuerpo["decision_final_guardada"] is False and cuerpo["decision_final"] is None
    c.post(f"{BASE}/decision-final", json={"decision": "biopsiar"})
    cuerpo = c.get(f"{BASE}/progreso").json()
    assert cuerpo["decision_final_guardada"] is True and cuerpo["decision_final"] == "biopsiar"


def test_no_revela_aciertos_ni_respuestas_correctas(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    for n in range(1, 8):
        responde(c, n)
    c.post(f"{BASE}/decision-inicial", json={"decision": "biopsiar"})
    texto = str(c.get(f"{BASE}/progreso").json())
    for campo in PRIVADOS_PROGRESO:
        assert campo not in texto, f"el progreso filtra {campo}"


def test_sesion_que_no_existe_da_404(cliente):
    c, _ = cliente
    assert c.get(f"{BASE}/progreso").status_code == 404


def test_sesion_de_otro_estudiante_da_403(cliente):
    c, estado = cliente
    abre_sesion(estado["db"], uid="otro")
    assert c.get(f"{BASE}/progreso").status_code == 403


def test_sesion_completada_da_403(cliente):
    c, estado = cliente
    abre_sesion(estado["db"], estado="completada")
    assert c.get(f"{BASE}/progreso").status_code == 403


def test_un_docente_no_puede_ver_el_progreso(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    estado["rol"] = "docente"
    assert c.get(f"{BASE}/progreso").status_code == 403


def test_caso_que_no_esta_en_la_sesion_da_422(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    assert c.get("/sesiones/s1/casos/caso_002/progreso").status_code == 422


def test_auth_no_deja_el_texto_del_error_en_los_registros(monkeypatch, capsys):
    """El texto del error de Firebase puede traer pedazos del token."""
    def falla(_token):
        raise ValueError("Can't parse segment: b'PEDAZO-SECRETO-DEL-TOKEN'")

    monkeypatch.setattr(auth_mod, "init_firebase", lambda: None)
    monkeypatch.setattr(auth_mod.fb_auth, "verify_id_token", falla)
    cred = SimpleNamespace(credentials="token.falso.xyz")
    with pytest.raises(HTTPException) as e:
        auth_mod.get_token_data(cred)
    assert e.value.status_code == 401 and e.value.detail == "Token inválido o vencido"
    salida = capsys.readouterr().out
    assert "ValueError" in salida and "verificar el token" in salida
    assert "PEDAZO-SECRETO" not in salida and "token.falso" not in salida

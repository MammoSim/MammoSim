"""GET /sesiones/{id_sesion}/casos/{id_caso}/progreso (endpoint 22).

Unity lo usa para retomar un caso cuando ya hay una sesión activa (por ejemplo, al recargar la
página): le dice qué preguntas ya se respondieron, cuál sigue y si las decisiones ya se guardaron.
Nunca debe revelar respuestas correctas, explicaciones ni lo que recomienda la IA.
Se ejecuta desde la raíz del repo:  python -m pytest tests
"""
import json
from datetime import datetime, timezone

BASE = "/sesiones/s1/casos/caso_001"
CLAVES = {
    "id_caso", "preguntas_respondidas", "siguiente_pregunta",
    "decision_inicial_guardada", "decision_final_guardada",
    "decision_inicial", "decision_final",
}
PRIVADOS = (
    "respuesta_correcta", "explicacion", "opcion_ia", "opcion_ia_incorrecta",
    "recomendacion_ia_correcta", "recomendacion_ia_incorrecta",
    "hallazgos", "retroalimentacion", "correcta", "ia_correcta", "siguio_ia", "nota_caso",
)


def abre_sesion(db, id_sesion="s1", uid="uid_1", estado="en_curso", casos=("caso_001",)):
    db.collection("sesiones").document(id_sesion).set(
        {
            "id_sesion": id_sesion, "id_usuario": uid, "id_curso": None,
            "casos_asignados": list(casos), "estado": estado,
            "fecha_inicio": datetime(2026, 11, 1, tzinfo=timezone.utc), "fecha_fin": None,
        }
    )


def responde(c, numeros, id_sesion="s1"):
    for n in numeros:
        r = c.post(f"/sesiones/{id_sesion}/casos/caso_001/respuestas",
                   json={"numero_pregunta": n, "opcion_elegida": "A"})
        assert r.status_code == 201


def progreso(c, id_sesion="s1", id_caso="caso_001"):
    return c.get(f"/sesiones/{id_sesion}/casos/{id_caso}/progreso")


def test_sin_respuestas_empieza_en_la_pregunta_1(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    r = progreso(c)
    assert r.status_code == 200
    assert r.json() == {
        "id_caso": "caso_001", "preguntas_respondidas": [], "siguiente_pregunta": 1,
        "decision_inicial_guardada": False, "decision_final_guardada": False,
        "decision_inicial": None, "decision_final": None,
    }


def test_tras_tres_respuestas_sigue_la_cuarta(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde(c, [1, 2, 3])
    cuerpo = progreso(c).json()
    assert cuerpo["preguntas_respondidas"] == [1, 2, 3]
    assert cuerpo["siguiente_pregunta"] == 4


def test_las_preguntas_respondidas_salen_ordenadas_aunque_se_respondan_desordenadas(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde(c, [3, 1, 2])
    cuerpo = progreso(c).json()
    assert cuerpo["preguntas_respondidas"] == [1, 2, 3]
    assert cuerpo["siguiente_pregunta"] == 4


def test_la_primera_pregunta_pendiente_es_la_que_falta_si_se_salto_una(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde(c, [1, 3])
    assert progreso(c).json()["siguiente_pregunta"] == 2


def test_con_las_7_respondidas_ya_no_hay_siguiente_pregunta(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde(c, range(1, 8))
    cuerpo = progreso(c).json()
    assert cuerpo["preguntas_respondidas"] == [1, 2, 3, 4, 5, 6, 7]
    assert cuerpo["siguiente_pregunta"] is None
    assert cuerpo["decision_inicial_guardada"] is False


def test_tras_la_decision_inicial_la_devuelve_y_la_final_sigue_pendiente(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde(c, range(1, 8))
    assert c.post(f"{BASE}/decision-inicial", json={"decision": "biopsiar"}).status_code == 201
    cuerpo = progreso(c).json()
    assert cuerpo["decision_inicial_guardada"] is True and cuerpo["decision_inicial"] == "biopsiar"
    assert cuerpo["decision_final_guardada"] is False and cuerpo["decision_final"] is None


def test_tras_la_decision_final_devuelve_las_dos(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde(c, range(1, 8))
    c.post(f"{BASE}/decision-inicial", json={"decision": "biopsiar"})
    assert c.post(f"{BASE}/decision-final", json={"decision": "seguimiento"}).status_code == 201
    cuerpo = progreso(c).json()
    assert cuerpo["decision_inicial"] == "biopsiar" and cuerpo["decision_final"] == "seguimiento"
    assert cuerpo["decision_inicial_guardada"] and cuerpo["decision_final_guardada"]


def test_solo_cuenta_las_respuestas_de_esa_sesion_y_ese_caso(cliente):
    c, estado = cliente
    abre_sesion(estado["db"], id_sesion="s1")
    abre_sesion(estado["db"], id_sesion="s2")
    responde(c, [1, 2, 3], id_sesion="s2")
    assert progreso(c, id_sesion="s1").json()["preguntas_respondidas"] == []
    assert progreso(c, id_sesion="s2").json()["preguntas_respondidas"] == [1, 2, 3]


def test_trae_exactamente_los_campos_que_unity_espera(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    assert set(progreso(c).json()) == CLAVES


def test_nunca_revela_respuestas_ni_la_ia(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde(c, range(1, 8))
    c.post(f"{BASE}/decision-inicial", json={"decision": "biopsiar"})
    c.post(f"{BASE}/decision-final", json={"decision": "biopsiar"})
    texto = json.dumps(progreso(c).json())
    for campo in PRIVADOS:
        assert f'"{campo}"' not in texto, f"/progreso filtra {campo}"


# ---------- reglas de acceso (las mismas del resto de rutas del caso) ----------
def test_una_sesion_inexistente_da_404(cliente):
    c, _ = cliente
    assert progreso(c, id_sesion="no_existe").status_code == 404


def test_una_sesion_de_otro_estudiante_da_403(cliente):
    c, estado = cliente
    abre_sesion(estado["db"], uid="otro_uid")
    assert progreso(c).status_code == 403


def test_una_sesion_que_ya_no_esta_en_curso_da_403(cliente):
    c, estado = cliente
    abre_sesion(estado["db"], estado="completada")
    assert progreso(c).status_code == 403


def test_un_caso_que_no_esta_en_la_sesion_da_422(cliente):
    c, estado = cliente
    abre_sesion(estado["db"], casos=("caso_002",))
    assert progreso(c, id_caso="caso_001").status_code == 422


def test_un_docente_no_puede_consultar_el_progreso(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    estado["rol"] = "docente"
    assert progreso(c).status_code == 403

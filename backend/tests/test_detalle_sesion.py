"""Pruebas de GET /sesiones/{id}/detalle (repaso de una sesión completada)."""
from datetime import datetime, timezone

from app.services.puntuacion import nota_caso

URL = "/sesiones/s1/detalle"


def guarda(db, coleccion, id_, datos):
    db.collection(coleccion).document(id_).set(datos)


def sesion(estado="completada", uid="uid_1"):
    return {
        "id_sesion": "s1", "id_usuario": uid, "id_curso": None,
        "casos_asignados": ["caso_001"], "estado": estado,
        "fecha_inicio": datetime(2026, 11, 1, tzinfo=timezone.utc),
        "fecha_fin": datetime(2026, 11, 1, 1, tzinfo=timezone.utc) if estado == "completada" else None,
    }


def completa(db, casos, decision_inicial=None, decision_final=None, elegidas=None):
    """Deja una sesión terminada. Por defecto contesta todo bien."""
    caso = casos["caso_001"]
    correcta = caso["respuesta_correcta"]
    ini = decision_inicial or correcta
    guarda(db, "sesiones", "s1", sesion())
    for p in caso["preguntas"]:
        elegida = (elegidas or {}).get(p["numero"], p["respuesta_correcta"])
        guarda(db, "respuestas", f"s1_caso_001_p{p['numero']}", {
            "id_sesion": "s1", "id_usuario": "uid_1", "id_caso": "caso_001",
            "numero_pregunta": p["numero"], "opcion_elegida": elegida,
            "correcta": elegida == p["respuesta_correcta"],
        })
    guarda(db, "decisiones_iniciales", "s1_caso_001", {
        "id_sesion": "s1", "id_caso": "caso_001", "decision": ini, "acierto": ini == correcta,
    })
    from app.services.tutor import evaluar_decision_final
    fin = decision_final or ini
    guarda(db, "decisiones_finales", "s1_caso_001", {
        "id_sesion": "s1", "id_caso": "caso_001", **evaluar_decision_final(caso, ini, fin),
    })
    guarda(db, "resultados", "s1", {"id_usuario": "uid_1", "puntuacion": 7.5})


def test_devuelve_el_repaso_completo(cliente, casos):
    c, estado = cliente
    completa(estado["db"], casos)
    r = c.get(URL)
    assert r.status_code == 200
    s = r.json()
    assert s["estado"] == "completada" and s["puntuacion"] == 7.5
    caso = s["casos"][0]
    caso_real = casos["caso_001"]
    assert caso["id_caso"] == "caso_001" and caso["titulo"] == caso_real["titulo"]
    assert len(caso["preguntas"]) == 7
    p1 = caso["preguntas"][0]
    assert set(p1) == {
        "numero", "enunciado", "opciones", "opcion_elegida",
        "respuesta_correcta", "correcta", "explicacion",
    }
    assert p1["enunciado"] == caso_real["preguntas"][0]["enunciado"]
    assert p1["opciones"][0] == {"id": "A", "texto": caso_real["preguntas"][0]["opciones"]["A"]}
    assert p1["explicacion"] == caso_real["preguntas"][0]["explicacion"]
    assert caso["hallazgos"] == caso_real["hallazgos"]
    assert caso["retroalimentacion"] == caso_real["retroalimentacion"]
    assert caso["respuesta_correcta"] == caso_real["respuesta_correcta"]


def test_marca_lo_elegido_y_lo_correcto(cliente, casos):
    c, estado = cliente
    mala = next(o for o in "ABCD" if o != casos["caso_001"]["preguntas"][2]["respuesta_correcta"])
    completa(estado["db"], casos, elegidas={3: mala})
    preguntas = c.get(URL).json()["casos"][0]["preguntas"]
    p3 = preguntas[2]
    assert p3["opcion_elegida"] == mala and p3["correcta"] is False
    assert p3["respuesta_correcta"] == casos["caso_001"]["preguntas"][2]["respuesta_correcta"]
    assert preguntas[0]["correcta"] is True


def test_decisiones_y_recomendacion_del_tutor(cliente, casos):
    c, estado = cliente
    caso = casos["caso_001"]
    # acierta al inicio -> la IA defiende una opción incorrecta; el estudiante la sigue
    completa(estado["db"], casos, decision_final=caso["opcion_ia_incorrecta"])
    d = c.get(URL).json()["casos"][0]
    assert d["decision_inicial"] == {"decision": caso["respuesta_correcta"], "acierto": True}
    assert d["decision_final"] == {"decision": caso["opcion_ia_incorrecta"], "acierto": False}
    assert d["recomendacion_ia"] == {
        "opcion_ia": caso["opcion_ia_incorrecta"],
        "texto": caso["recomendacion_ia_incorrecta"],
        "ia_correcta": False, "siguio_ia": True,
    }


def test_la_nota_del_caso_es_la_de_la_formula(cliente, casos):
    c, estado = cliente
    completa(estado["db"], casos)
    assert c.get(URL).json()["casos"][0]["nota_caso"] == nota_caso(7, True, True)


def test_sesion_en_curso_da_409_y_no_adelanta_respuestas(cliente, casos):
    c, estado = cliente
    guarda(estado["db"], "sesiones", "s1", sesion(estado="en_curso"))
    r = c.get(URL)
    assert r.status_code == 409
    assert "explicacion" not in r.text and "respuesta_correcta" not in r.text


def test_sesion_de_otro_estudiante_da_403(cliente, casos):
    c, estado = cliente
    guarda(estado["db"], "sesiones", "s1", sesion(uid="uid_otro"))
    assert c.get(URL).status_code == 403


def test_sesion_que_no_existe_da_404(cliente):
    c, _ = cliente
    assert c.get("/sesiones/nada/detalle").status_code == 404


def test_un_docente_no_puede_usarlo(cliente, casos):
    c, estado = cliente
    completa(estado["db"], casos)
    estado["rol"] = "docente"
    assert c.get(URL).status_code == 403

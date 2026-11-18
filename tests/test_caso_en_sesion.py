"""Pruebas de los 5 endpoints de dentro de un caso (13 a 17)."""
from datetime import datetime, timezone

BASE = "/sesiones/s1/casos/caso_001"
# caso_001: la decisión correcta es "biopsiar"; la IA incorrecta fija es "seguimiento".
# Respuestas correctas de las 7 preguntas: B, C, D, B, C, D, D
CORRECTAS = ["B", "C", "D", "B", "C", "D", "D"]
OPCIONES = ["A", "B", "C", "D"]


def abre_sesion(db, id_sesion="s1", uid="uid_1", estado="en_curso", casos=("caso_001",)):
    db.collection("sesiones").document(id_sesion).set(
        {
            "id_sesion": id_sesion, "id_usuario": uid, "id_curso": None,
            "casos_asignados": list(casos), "estado": estado,
            "fecha_inicio": datetime(2026, 11, 1, tzinfo=timezone.utc), "fecha_fin": None,
        }
    )


def responde(c, n, opcion):
    return c.post(f"{BASE}/respuestas", json={"numero_pregunta": n, "opcion_elegida": opcion})


def responde_todas(c, acertando=7):
    """Responde las 7; acierta las primeras `acertando` y falla el resto."""
    for n in range(1, 8):
        buena = CORRECTAS[n - 1]
        eleccion = buena if n <= acertando else next(o for o in OPCIONES if o != buena)
        assert responde(c, n, eleccion).status_code == 201


def decide(c, ruta, decision):
    return c.post(f"{BASE}/{ruta}", json={"decision": decision})


# ---------- 13. respuestas ----------
def test_responder_correcta_guarda_y_explica(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    r = responde(c, 1, "B")
    assert r.status_code == 201
    cuerpo = r.json()
    assert cuerpo["correcta"] is True and cuerpo["respuesta_correcta"] == "B"
    assert cuerpo["explicacion"] and cuerpo["mensaje"].startswith("Correcto")
    doc = estado["db"].collection("respuestas").document("s1_caso_001_p1").get().to_dict()
    assert doc["correcta"] is True and doc["id_usuario"] == "uid_1"
    assert doc["opcion_elegida"] == "B" and doc["numero_pregunta"] == 1


def test_responder_incorrecta_dice_cual_era_la_correcta(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    r = responde(c, 1, "A")
    assert r.status_code == 201
    assert r.json()["correcta"] is False
    assert r.json()["mensaje"].startswith("Incorrecto")
    assert r.json()["respuesta_correcta"] == "B"


def test_no_se_puede_responder_dos_veces_la_misma_pregunta(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    assert responde(c, 1, "A").status_code == 201
    assert responde(c, 1, "B").status_code == 409  # no vale probar hasta acertar
    guardada = estado["db"].collection("respuestas").document("s1_caso_001_p1").get()
    assert guardada.to_dict()["opcion_elegida"] == "A"  # la primera es la que cuenta


def test_la_correcta_la_calcula_el_servidor_no_el_cliente(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    r = c.post(f"{BASE}/respuestas",
               json={"numero_pregunta": 1, "opcion_elegida": "A", "correcta": True})
    assert r.json()["correcta"] is False


# ---------- reglas comunes (sesión y caso) ----------
def test_sesion_que_no_existe_da_404(cliente):
    c, estado = cliente
    assert responde(c, 1, "B").status_code == 404


def test_sesion_de_otro_estudiante_da_403(cliente):
    c, estado = cliente
    abre_sesion(estado["db"], uid="uid_otro")
    assert responde(c, 1, "B").status_code == 403


def test_sesion_completada_da_403(cliente):
    c, estado = cliente
    abre_sesion(estado["db"], estado="completada")
    assert responde(c, 1, "B").status_code == 403


def test_caso_que_no_esta_en_la_sesion_da_422(cliente):
    c, estado = cliente
    abre_sesion(estado["db"], casos=["caso_002"])
    assert responde(c, 1, "B").status_code == 422


def test_un_docente_no_entra_a_las_rutas_del_caso(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    estado["rol"] = "docente"
    assert responde(c, 1, "B").status_code == 403


# ---------- 14. decisión inicial ----------
def test_decision_inicial_exige_las_7_respuestas(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    for n in range(1, 7):
        responde(c, n, "A")
    r = decide(c, "decision-inicial", "biopsiar")
    assert r.status_code == 409
    assert estado["db"].collection("decisiones_iniciales").stream() == []


def test_decision_inicial_guarda_acierto_y_no_lo_revela(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde_todas(c)
    r = decide(c, "decision-inicial", "biopsiar")
    assert r.status_code == 201 and r.json() == {"guardada": True}
    doc = estado["db"].collection("decisiones_iniciales").document("s1_caso_001").get().to_dict()
    assert doc["acierto"] is True and doc["decision"] == "biopsiar"
    assert doc["id_usuario"] == "uid_1"


def test_decision_inicial_mala_guarda_acierto_false(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde_todas(c)
    decide(c, "decision-inicial", "ignorar")
    doc = estado["db"].collection("decisiones_iniciales").document("s1_caso_001").get().to_dict()
    assert doc["acierto"] is False


def test_decision_inicial_no_se_puede_repetir(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde_todas(c)
    assert decide(c, "decision-inicial", "ignorar").status_code == 201
    assert decide(c, "decision-inicial", "biopsiar").status_code == 409


def test_decision_invalida_da_422(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde_todas(c)
    assert decide(c, "decision-inicial", "operar").status_code == 422


# ---------- 15. recomendación de la IA ----------
def test_recomendacion_exige_la_decision_inicial(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    assert c.get(f"{BASE}/recomendacion-ia").status_code == 409


def test_si_acerto_la_ia_defiende_la_opcion_incorrecta(cliente, casos):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde_todas(c)
    decide(c, "decision-inicial", "biopsiar")
    r = c.get(f"{BASE}/recomendacion-ia")
    assert r.status_code == 200
    assert r.json()["opcion_ia"] == "seguimiento"
    assert r.json()["texto"] == casos["caso_001"]["recomendacion_ia_incorrecta"]


def test_si_fallo_la_ia_recomienda_la_correcta(cliente, casos):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde_todas(c)
    decide(c, "decision-inicial", "ignorar")
    r = c.get(f"{BASE}/recomendacion-ia")
    assert r.json()["opcion_ia"] == "biopsiar"
    assert r.json()["texto"] == casos["caso_001"]["recomendacion_ia_correcta"]


def test_la_recomendacion_nunca_dice_si_es_correcta_ni_escribe(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde_todas(c)
    decide(c, "decision-inicial", "biopsiar")
    r = c.get(f"{BASE}/recomendacion-ia")
    assert set(r.json()) == {"opcion_ia", "texto"}
    assert estado["db"].collection("decisiones_finales").stream() == []


# ---------- 16. decisión final ----------
def test_decision_final_exige_la_inicial(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    assert decide(c, "decision-final", "biopsiar").status_code == 409


def test_acierta_y_mantiene_su_decision(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde_todas(c)
    decide(c, "decision-inicial", "biopsiar")
    assert decide(c, "decision-final", "biopsiar").status_code == 201
    doc = estado["db"].collection("decisiones_finales").document("s1_caso_001").get().to_dict()
    assert doc["decision"] == "biopsiar" and doc["acierto"] is True
    assert doc["opcion_ia"] == "seguimiento" and doc["ia_correcta"] is False
    assert doc["siguio_ia"] is False


def test_acierta_y_se_deja_llevar_por_la_ia_incorrecta(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde_todas(c)
    decide(c, "decision-inicial", "biopsiar")
    decide(c, "decision-final", "seguimiento")
    doc = estado["db"].collection("decisiones_finales").document("s1_caso_001").get().to_dict()
    assert doc["acierto"] is False and doc["siguio_ia"] is True and doc["ia_correcta"] is False


def test_falla_y_la_ia_lo_corrige(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde_todas(c)
    decide(c, "decision-inicial", "ignorar")
    decide(c, "decision-final", "biopsiar")
    doc = estado["db"].collection("decisiones_finales").document("s1_caso_001").get().to_dict()
    assert doc["acierto"] is True and doc["siguio_ia"] is True and doc["ia_correcta"] is True


def test_decision_final_no_se_puede_repetir(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde_todas(c)
    decide(c, "decision-inicial", "biopsiar")
    assert decide(c, "decision-final", "biopsiar").status_code == 201
    assert decide(c, "decision-final", "ignorar").status_code == 409


# ---------- 17. retroalimentación ----------
def test_retroalimentacion_exige_la_decision_final(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde_todas(c)
    decide(c, "decision-inicial", "biopsiar")
    assert c.get(f"{BASE}/retroalimentacion").status_code == 409


def test_retroalimentacion_con_nota_maxima(cliente, casos):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde_todas(c, acertando=7)
    decide(c, "decision-inicial", "biopsiar")
    decide(c, "decision-final", "biopsiar")
    r = c.get(f"{BASE}/retroalimentacion")
    assert r.status_code == 200
    cuerpo = r.json()
    assert cuerpo["nota_caso"] == 10.0
    assert cuerpo["respuesta_correcta"] == "biopsiar"
    assert cuerpo["hallazgos"] == casos["caso_001"]["hallazgos"]
    assert cuerpo["retroalimentacion"] == casos["caso_001"]["retroalimentacion"]
    assert [p["numero"] for p in cuerpo["preguntas"]] == [1, 2, 3, 4, 5, 6, 7]
    assert all(p["correcta"] for p in cuerpo["preguntas"])
    assert cuerpo["ia_correcta"] is False and cuerpo["siguio_ia"] is False


def test_retroalimentacion_calcula_la_nota_con_el_desglose(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde_todas(c, acertando=5)           # 5 puntos
    decide(c, "decision-inicial", "biopsiar")  # +0,5
    decide(c, "decision-final", "seguimiento")  # +0 (siguió a la IA incorrecta)
    r = c.get(f"{BASE}/retroalimentacion").json()
    assert r["nota_caso"] == round((5 + 0.5) * 1.25, 2)  # 6,88
    assert r["siguio_ia"] is True and r["ia_correcta"] is False
    assert [p["correcta"] for p in r["preguntas"]] == [True] * 5 + [False] * 2


def test_retroalimentacion_cuando_la_ia_corrige(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    responde_todas(c, acertando=7)
    decide(c, "decision-inicial", "ignorar")   # 0
    decide(c, "decision-final", "biopsiar")    # +0,5
    r = c.get(f"{BASE}/retroalimentacion").json()
    assert r["nota_caso"] == round((7 + 0.5) * 1.25, 2)  # 9,38
    assert r["ia_correcta"] is True and r["siguio_ia"] is True


def test_el_flujo_completo_no_filtra_la_respuesta_antes_de_tiempo(cliente):
    """Antes de la decisión final no hay ningún endpoint que devuelva la respuesta correcta
    de la decisión: ni la recomendación de la IA ni la decisión inicial."""
    c, estado = cliente
    abre_sesion(estado["db"])
    responde_todas(c)
    inicial = decide(c, "decision-inicial", "ignorar")
    assert "acierto" not in inicial.json() and "respuesta_correcta" not in inicial.json()
    rec = c.get(f"{BASE}/recomendacion-ia").json()
    assert "ia_correcta" not in rec
    assert c.get(f"{BASE}/retroalimentacion").status_code == 409

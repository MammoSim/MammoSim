"""Pruebas de POST /sesiones/{id}/cerrar y GET /sesiones/{id}/resultado."""
from datetime import datetime, timezone


def abre_sesion(db, id_sesion="s1", uid="uid_1", estado="en_curso", casos=("caso_001",),
                id_curso=None):
    db.collection("sesiones").document(id_sesion).set(
        {
            "id_sesion": id_sesion, "id_usuario": uid, "id_curso": id_curso,
            "casos_asignados": list(casos), "estado": estado,
            "fecha_inicio": datetime(2026, 11, 1, tzinfo=timezone.utc), "fecha_fin": None,
        }
    )


def juega_caso(c, casos, id_caso, preguntas_buenas=7, inicial_buena=True, final_buena=True,
               id_sesion="s1"):
    """Juega un caso entero por la API, como lo haría el estudiante."""
    caso = casos[id_caso]
    base = f"/sesiones/{id_sesion}/casos/{id_caso}"
    for p in caso["preguntas"]:
        buena = p["respuesta_correcta"]
        eleccion = buena if p["numero"] <= preguntas_buenas else next(
            o for o in "ABCD" if o != buena
        )
        r = c.post(f"{base}/respuestas",
                   json={"numero_pregunta": p["numero"], "opcion_elegida": eleccion})
        assert r.status_code == 201

    correcta = caso["respuesta_correcta"]
    mala = next(d for d in ("biopsiar", "seguimiento", "ignorar", "estudio_adicional")
                if d != correcta)
    assert c.post(f"{base}/decision-inicial",
                  json={"decision": correcta if inicial_buena else mala}).status_code == 201
    assert c.post(f"{base}/decision-final",
                  json={"decision": correcta if final_buena else mala}).status_code == 201


# ---------- POST /sesiones/{id}/cerrar ----------
def test_cierra_una_sesion_perfecta_con_10(cliente, casos):
    c, estado = cliente
    abre_sesion(estado["db"])
    juega_caso(c, casos, "caso_001")
    r = c.post("/sesiones/s1/cerrar")
    assert r.status_code == 200
    assert r.json() == {
        "id_usuario": "uid_1", "id_curso": None, "total_casos": 1,
        "aciertos_iniciales": 1, "aciertos_finales": 1, "preguntas_acertadas": 7,
        "veces_siguio_ia": 0, "siguio_ia_correcta": 0, "siguio_ia_incorrecta": 0,
        "puntuacion": 10.0,
    }


def test_al_cerrar_la_sesion_queda_completada_con_fecha_fin_y_resultado(cliente, casos):
    c, estado = cliente
    abre_sesion(estado["db"])
    juega_caso(c, casos, "caso_001")
    c.post("/sesiones/s1/cerrar")
    sesion = estado["db"].collection("sesiones").document("s1").get().to_dict()
    assert sesion["estado"] == "completada" and sesion["fecha_fin"] is not None
    resultado = estado["db"].collection("resultados").document("s1").get().to_dict()
    assert resultado["puntuacion"] == 10.0 and resultado["fecha_calculo"]


def test_calcula_el_resultado_de_dos_casos_con_uso_de_la_ia(cliente, casos):
    c, estado = cliente
    abre_sesion(estado["db"], casos=["caso_001", "caso_002"], id_curso="curso_1")
    juega_caso(c, casos, "caso_001")  # perfecto; la IA defendía la incorrecta y no la siguió
    # inicial mala -> la IA la corrige y el estudiante la sigue; no acierta preguntas
    juega_caso(c, casos, "caso_002", preguntas_buenas=0, inicial_buena=False, final_buena=True)
    r = c.post("/sesiones/s1/cerrar").json()
    assert r["id_curso"] == "curso_1" and r["total_casos"] == 2
    assert r["preguntas_acertadas"] == 7
    assert r["aciertos_iniciales"] == 1 and r["aciertos_finales"] == 2
    assert r["veces_siguio_ia"] == 1
    assert r["siguio_ia_correcta"] == 1 and r["siguio_ia_incorrecta"] == 0
    assert r["puntuacion"] == round((7 + 0.5 * 1 + 0.5 * 2) / 16 * 10, 2)  # 5,31


def test_seguir_a_la_ia_incorrecta_se_cuenta_aparte(cliente, casos):
    c, estado = cliente
    abre_sesion(estado["db"])
    juega_caso(c, casos, "caso_001", inicial_buena=True, final_buena=False)  # siguió a la IA mala
    r = c.post("/sesiones/s1/cerrar").json()
    assert r["veces_siguio_ia"] == 1
    assert r["siguio_ia_correcta"] == 0 and r["siguio_ia_incorrecta"] == 1
    assert r["aciertos_finales"] == 0


def test_no_se_puede_cerrar_a_medias(cliente, casos):
    c, estado = cliente
    abre_sesion(estado["db"], casos=["caso_001", "caso_002"])
    juega_caso(c, casos, "caso_001")  # falta el caso_002
    r = c.post("/sesiones/s1/cerrar")
    assert r.status_code == 409
    assert estado["db"].collection("sesiones").document("s1").get().to_dict()["estado"] == "en_curso"
    assert not estado["db"].collection("resultados").document("s1").get().exists


def test_cerrar_una_sesion_sin_empezar_ningun_caso_da_409(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    assert c.post("/sesiones/s1/cerrar").status_code == 409


def test_cerrar_dos_veces_da_409_y_no_cambia_el_resultado(cliente, casos):
    c, estado = cliente
    abre_sesion(estado["db"])
    juega_caso(c, casos, "caso_001")
    primero = c.post("/sesiones/s1/cerrar").json()
    assert c.post("/sesiones/s1/cerrar").status_code == 409
    assert estado["db"].collection("resultados").document("s1").get().to_dict()["puntuacion"] \
        == primero["puntuacion"]


def test_cerrar_la_sesion_de_otro_estudiante_da_403(cliente):
    c, estado = cliente
    abre_sesion(estado["db"], uid="uid_otro")
    assert c.post("/sesiones/s1/cerrar").status_code == 403


def test_cerrar_una_sesion_que_no_existe_da_404(cliente):
    c, estado = cliente
    assert c.post("/sesiones/no_existe/cerrar").status_code == 404


def test_un_docente_no_puede_cerrar_sesiones(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    estado["rol"] = "docente"
    assert c.post("/sesiones/s1/cerrar").status_code == 403


def test_al_cerrar_se_puede_abrir_otra_sesion(cliente, casos):
    c, estado = cliente
    abre_sesion(estado["db"])
    juega_caso(c, casos, "caso_001")
    assert c.post("/sesiones", json={"id_curso": None, "casos": ["caso_002"]}).status_code == 409
    c.post("/sesiones/s1/cerrar")
    assert c.post("/sesiones", json={"id_curso": None, "casos": ["caso_002"]}).status_code == 201
    assert c.get("/sesiones/activa").json()["casos_asignados"] == ["caso_002"]


# ---------- GET /sesiones/{id}/resultado ----------
def test_ver_resultado_de_una_sesion_completada(cliente, casos):
    c, estado = cliente
    abre_sesion(estado["db"])
    juega_caso(c, casos, "caso_001")
    cerrado = c.post("/sesiones/s1/cerrar").json()
    r = c.get("/sesiones/s1/resultado")
    assert r.status_code == 200
    assert r.json() == cerrado
    assert "fecha_calculo" not in r.json()


def test_ver_resultado_de_una_sesion_en_curso_da_409(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    assert c.get("/sesiones/s1/resultado").status_code == 409


def test_ver_resultado_de_otro_estudiante_da_403(cliente):
    c, estado = cliente
    abre_sesion(estado["db"], uid="uid_otro", estado="completada")
    assert c.get("/sesiones/s1/resultado").status_code == 403


def test_ver_resultado_de_una_sesion_que_no_existe_da_404(cliente):
    c, estado = cliente
    assert c.get("/sesiones/no_existe/resultado").status_code == 404


def test_un_docente_no_puede_ver_resultados_de_estudiantes(cliente):
    c, estado = cliente
    abre_sesion(estado["db"], estado="completada")
    estado["rol"] = "docente"
    assert c.get("/sesiones/s1/resultado").status_code == 403

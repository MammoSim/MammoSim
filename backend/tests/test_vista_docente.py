"""Pruebas de GET /cursos/mio/sesiones y GET /cursos/mio/sesiones/{id} (vista del docente)."""
from datetime import datetime, timedelta, timezone

INICIO = datetime(2026, 11, 1, tzinfo=timezone.utc)


def como_docente(estado, con_curso=True):
    """uid_1 pasa a ser el docente; opcionalmente con su curso (curso_1)."""
    estado["rol"] = "docente"
    if con_curso:
        estado["db"].collection("cursos").document("curso_1").set(
            {"codigo": "MS-ABC234", "nombre": "GPTI", "id_docente": "uid_1",
             "fecha_creacion": INICIO}
        )


def estudiante(db, uid, nombre):
    db.collection("usuarios").document(uid).set(
        {"id_usuario": uid, "nombre_completo": nombre, "rol": "estudiante"}
    )


def sesion(db, id_sesion, uid="est_1", id_curso="curso_1", estado="en_curso", dias=0,
           casos=("caso_001",), puntuacion=None):
    db.collection("sesiones").document(id_sesion).set(
        {
            "id_sesion": id_sesion, "id_usuario": uid, "id_curso": id_curso,
            "casos_asignados": list(casos), "estado": estado,
            "fecha_inicio": INICIO + timedelta(days=dias),
            "fecha_fin": INICIO + timedelta(days=dias, hours=1) if estado == "completada" else None,
        }
    )
    if puntuacion is not None:
        db.collection("resultados").document(id_sesion).set({"puntuacion": puntuacion})


def juega(db, id_sesion, id_caso, buenas=7, inicial=True, final=True, siguio=False,
          ia_correcta=False, opcion_ia="seguimiento", uid="est_1"):
    """Deja guardado lo que el estudiante hizo en un caso (sin pasar por la API)."""
    for n in range(1, 8):
        db.collection("respuestas").document(f"{id_sesion}_{id_caso}_p{n}").set(
            {"id_sesion": id_sesion, "id_usuario": uid, "id_caso": id_caso,
             "numero_pregunta": n, "opcion_elegida": "A", "correcta": n <= buenas}
        )
    db.collection("decisiones_iniciales").document(f"{id_sesion}_{id_caso}").set(
        {"id_sesion": id_sesion, "id_usuario": uid, "id_caso": id_caso,
         "decision": "biopsiar" if inicial else "ignorar", "acierto": inicial}
    )
    db.collection("decisiones_finales").document(f"{id_sesion}_{id_caso}").set(
        {"id_sesion": id_sesion, "id_usuario": uid, "id_caso": id_caso,
         "decision": "biopsiar" if final else "seguimiento", "acierto": final,
         "opcion_ia": opcion_ia, "ia_correcta": ia_correcta, "siguio_ia": siguio}
    )


# ---------- GET /cursos/mio/sesiones ----------
def test_lista_solo_las_sesiones_de_su_curso_con_nombre_y_puntuacion(cliente):
    c, estado = cliente
    como_docente(estado)
    db = estado["db"]
    estudiante(db, "est_1", "Ana Pérez")
    estudiante(db, "est_2", "Luis Gómez")
    sesion(db, "s1", "est_1", estado="completada", dias=0, puntuacion=8.5)
    sesion(db, "s2", "est_2", dias=2)  # en curso, sin puntuación
    sesion(db, "s3", "est_1", id_curso=None, dias=3)  # práctica libre: no se ve
    sesion(db, "s4", "est_1", id_curso="curso_ajeno", dias=4)  # de otro curso: no se ve
    r = c.get("/cursos/mio/sesiones")
    assert r.status_code == 200
    filas = r.json()
    assert [f["id_sesion"] for f in filas] == ["s2", "s1"]  # la más reciente primero
    assert filas[0]["nombre_estudiante"] == "Luis Gómez" and filas[0]["estado"] == "en_curso"
    assert filas[0]["puntuacion"] is None
    assert filas[1]["nombre_estudiante"] == "Ana Pérez" and filas[1]["puntuacion"] == 8.5


def test_se_puede_filtrar_por_estudiante(cliente):
    c, estado = cliente
    como_docente(estado)
    db = estado["db"]
    estudiante(db, "est_1", "Ana Pérez")
    estudiante(db, "est_2", "Luis Gómez")
    sesion(db, "s1", "est_1")
    sesion(db, "s2", "est_2", dias=1)
    r = c.get("/cursos/mio/sesiones", params={"id_usuario": "est_2"})
    assert [f["id_sesion"] for f in r.json()] == ["s2"]


def test_lista_vacia_si_nadie_ha_hecho_sesiones(cliente):
    c, estado = cliente
    como_docente(estado)
    assert c.get("/cursos/mio/sesiones").json() == []


def test_docente_sin_curso_da_404(cliente):
    c, estado = cliente
    como_docente(estado, con_curso=False)
    assert c.get("/cursos/mio/sesiones").status_code == 404


def test_un_estudiante_no_ve_las_sesiones_del_curso(cliente):
    c, estado = cliente  # rol estudiante por defecto
    assert c.get("/cursos/mio/sesiones").status_code == 403


def test_no_ve_las_sesiones_del_curso_de_otro_docente(cliente):
    c, estado = cliente
    como_docente(estado)
    db = estado["db"]
    db.collection("cursos").document("curso_otro").set(
        {"codigo": "MS-ZZZZZZ", "nombre": "Otro", "id_docente": "doc_otro",
         "fecha_creacion": INICIO}
    )
    estudiante(db, "est_1", "Ana Pérez")
    sesion(db, "s9", "est_1", id_curso="curso_otro")
    assert c.get("/cursos/mio/sesiones").json() == []


# ---------- GET /cursos/mio/sesiones/{id} ----------
def test_detalle_con_respuestas_decisiones_y_nota_de_cada_caso(cliente):
    c, estado = cliente
    como_docente(estado)
    db = estado["db"]
    estudiante(db, "est_1", "Ana Pérez")
    sesion(db, "s1", "est_1", estado="completada", casos=["caso_001", "caso_002"], puntuacion=6.0)
    # caso_001: 5 preguntas buenas, inicial buena, final mala porque siguió a la IA incorrecta
    juega(db, "s1", "caso_001", buenas=5, inicial=True, final=False, siguio=True,
          ia_correcta=False, opcion_ia="seguimiento")
    # caso_002: perfecto
    juega(db, "s1", "caso_002", buenas=7, inicial=True, final=True, siguio=False)
    r = c.get("/cursos/mio/sesiones/s1")
    assert r.status_code == 200
    d = r.json()
    assert d["nombre_estudiante"] == "Ana Pérez" and d["estado"] == "completada"
    assert d["puntuacion"] == 6.0 and d["fecha_fin"]
    uno, dos = d["casos"]
    assert uno["id_caso"] == "caso_001" and uno["titulo"]
    assert [x["numero_pregunta"] for x in uno["respuestas"]] == [1, 2, 3, 4, 5, 6, 7]
    assert [x["correcta"] for x in uno["respuestas"]] == [True] * 5 + [False] * 2
    assert uno["decision_inicial"] == {"decision": "biopsiar", "acierto": True}
    assert uno["decision_final"]["siguio_ia"] is True
    assert uno["decision_final"]["ia_correcta"] is False
    assert uno["nota_caso"] == round((5 + 0.5) * 1.25, 2)
    assert dos["nota_caso"] == 10.0


def test_un_caso_sin_terminar_aparece_sin_decision_final_ni_nota(cliente):
    c, estado = cliente
    como_docente(estado)
    db = estado["db"]
    estudiante(db, "est_1", "Ana Pérez")
    sesion(db, "s1", "est_1", casos=["caso_001"])
    r = c.get("/cursos/mio/sesiones/s1").json()
    caso = r["casos"][0]
    assert caso["respuestas"] == []
    assert caso["decision_inicial"] is None and caso["decision_final"] is None
    assert caso["nota_caso"] is None
    assert r["estado"] == "en_curso" and r["puntuacion"] is None


def test_detalle_de_sesion_de_otro_curso_da_404(cliente):
    c, estado = cliente
    como_docente(estado)
    sesion(estado["db"], "s1", id_curso="curso_ajeno")
    assert c.get("/cursos/mio/sesiones/s1").status_code == 404


def test_detalle_de_practica_libre_da_404(cliente):
    c, estado = cliente
    como_docente(estado)
    sesion(estado["db"], "s1", id_curso=None)
    assert c.get("/cursos/mio/sesiones/s1").status_code == 404


def test_detalle_de_sesion_que_no_existe_da_404(cliente):
    c, estado = cliente
    como_docente(estado)
    assert c.get("/cursos/mio/sesiones/no_existe").status_code == 404


def test_un_estudiante_no_ve_el_detalle(cliente):
    c, estado = cliente
    sesion(estado["db"], "s1")
    assert c.get("/cursos/mio/sesiones/s1").status_code == 403


def test_docente_sin_curso_no_ve_detalle(cliente):
    c, estado = cliente
    como_docente(estado, con_curso=False)
    sesion(estado["db"], "s1")
    assert c.get("/cursos/mio/sesiones/s1").status_code == 404


def test_el_detalle_no_trae_explicaciones_ni_hallazgos(cliente):
    c, estado = cliente
    como_docente(estado)
    db = estado["db"]
    sesion(db, "s1", "est_1")
    juega(db, "s1", "caso_001")
    texto = c.get("/cursos/mio/sesiones/s1").text
    for campo in ("explicacion", "hallazgos", "retroalimentacion", "recomendacion_ia"):
        assert campo not in texto

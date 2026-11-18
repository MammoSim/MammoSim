"""Pruebas de POST /inscripciones, DELETE /inscripciones/{id_curso} y GET /inscripciones/mias."""
from datetime import datetime, timezone


def crea_curso(db, id_curso="curso_1", codigo="MS-ABC234", nombre="Gestión de Proyectos"):
    db.collection("cursos").document(id_curso).set(
        {
            "codigo": codigo, "nombre": nombre, "id_docente": "doc_1",
            "fecha_creacion": datetime(2026, 11, 1, tzinfo=timezone.utc),
        }
    )


def inscripcion(db, id_usuario, id_curso):
    return db.collection("inscripciones").document(f"{id_usuario}_{id_curso}").get()


# ---------- POST /inscripciones ----------
def test_el_estudiante_se_une_con_el_codigo(cliente):
    c, estado = cliente
    crea_curso(estado["db"])
    r = c.post("/inscripciones", json={"codigo": "MS-ABC234"})
    assert r.status_code == 200
    assert r.json() == {
        "id_curso": "curso_1", "codigo": "MS-ABC234",
        "nombre": "Gestión de Proyectos", "id_docente": "doc_1",
    }
    guardada = inscripcion(estado["db"], "uid_1", "curso_1")
    assert guardada.exists
    datos = guardada.to_dict()
    assert datos["activo"] is True
    assert datos["id_usuario"] == "uid_1"  # sale del token
    assert datos["fecha_union"]


def test_el_codigo_se_acepta_en_minusculas_y_con_espacios(cliente):
    c, estado = cliente
    crea_curso(estado["db"])
    r = c.post("/inscripciones", json={"codigo": "  ms-abc234 "})
    assert r.status_code == 200
    assert inscripcion(estado["db"], "uid_1", "curso_1").exists


def test_codigo_que_no_existe_da_404_y_no_guarda(cliente):
    c, estado = cliente
    crea_curso(estado["db"])
    assert c.post("/inscripciones", json={"codigo": "MS-ZZZZZZ"}).status_code == 404
    assert estado["db"].collection("inscripciones").stream() == []


def test_unirse_dos_veces_no_duplica_la_inscripcion(cliente):
    c, estado = cliente
    crea_curso(estado["db"])
    assert c.post("/inscripciones", json={"codigo": "MS-ABC234"}).status_code == 200
    assert c.post("/inscripciones", json={"codigo": "MS-ABC234"}).status_code == 200
    assert len(estado["db"].collection("inscripciones").stream()) == 1


def test_un_docente_no_puede_unirse_a_un_curso(cliente):
    c, estado = cliente
    estado["rol"] = "docente"
    crea_curso(estado["db"])
    assert c.post("/inscripciones", json={"codigo": "MS-ABC234"}).status_code == 403


# ---------- DELETE /inscripciones/{id_curso} ----------
def test_salirse_pone_activo_en_false_y_conserva_el_documento(cliente):
    c, estado = cliente
    crea_curso(estado["db"])
    c.post("/inscripciones", json={"codigo": "MS-ABC234"})
    r = c.delete("/inscripciones/curso_1")
    assert r.status_code == 204
    guardada = inscripcion(estado["db"], "uid_1", "curso_1")
    assert guardada.exists  # el historial se conserva
    assert guardada.to_dict()["activo"] is False


def test_salirse_de_un_curso_donde_no_estaba_da_404(cliente):
    c, estado = cliente
    crea_curso(estado["db"])
    assert c.delete("/inscripciones/curso_1").status_code == 404


def test_salirse_dos_veces_da_404_la_segunda(cliente):
    c, estado = cliente
    crea_curso(estado["db"])
    c.post("/inscripciones", json={"codigo": "MS-ABC234"})
    assert c.delete("/inscripciones/curso_1").status_code == 204
    assert c.delete("/inscripciones/curso_1").status_code == 404


def test_volver_a_unirse_reactiva_y_conserva_la_fecha_original(cliente):
    c, estado = cliente
    crea_curso(estado["db"])
    c.post("/inscripciones", json={"codigo": "MS-ABC234"})
    fecha = inscripcion(estado["db"], "uid_1", "curso_1").to_dict()["fecha_union"]
    c.delete("/inscripciones/curso_1")
    assert c.post("/inscripciones", json={"codigo": "MS-ABC234"}).status_code == 200
    datos = inscripcion(estado["db"], "uid_1", "curso_1").to_dict()
    assert datos["activo"] is True
    assert datos["fecha_union"] == fecha
    assert len(estado["db"].collection("inscripciones").stream()) == 1


def test_no_puede_salirse_de_la_inscripcion_de_otro_estudiante(cliente):
    c, estado = cliente
    crea_curso(estado["db"])
    estado["db"].collection("inscripciones").document("uid_otro_curso_1").set(
        {"id_usuario": "uid_otro", "id_curso": "curso_1", "activo": True}
    )
    assert c.delete("/inscripciones/curso_1").status_code == 404  # busca uid_1_curso_1
    assert inscripcion(estado["db"], "uid_otro", "curso_1").to_dict()["activo"] is True


# ---------- GET /inscripciones/mias ----------
def test_mis_cursos_lista_solo_los_activos_y_propios_por_nombre(cliente):
    c, estado = cliente
    db = estado["db"]
    crea_curso(db, "curso_1", "MS-AAAAAA", "Zoología")
    crea_curso(db, "curso_2", "MS-BBBBBB", "Anatomía")
    crea_curso(db, "curso_3", "MS-CCCCCC", "Biología")
    crea_curso(db, "curso_4", "MS-DDDDDD", "Química")
    for codigo in ("MS-AAAAAA", "MS-BBBBBB", "MS-CCCCCC"):
        c.post("/inscripciones", json={"codigo": codigo})
    c.delete("/inscripciones/curso_3")  # se salió de Biología
    db.collection("inscripciones").document("uid_otro_curso_4").set(  # de otra persona
        {"id_usuario": "uid_otro", "id_curso": "curso_4", "activo": True}
    )
    r = c.get("/inscripciones/mias")
    assert r.status_code == 200
    assert [x["nombre"] for x in r.json()] == ["Anatomía", "Zoología"]


def test_mis_cursos_vacio_si_no_esta_inscrito(cliente):
    c, estado = cliente
    assert c.get("/inscripciones/mias").json() == []


def test_un_docente_no_puede_ver_mis_cursos(cliente):
    c, estado = cliente
    estado["rol"] = "docente"
    assert c.get("/inscripciones/mias").status_code == 403


# ---------- Conexión con las sesiones ----------
def test_inscrito_puede_abrir_sesion_del_curso_y_al_salirse_ya_no(cliente):
    c, estado = cliente
    crea_curso(estado["db"])
    c.post("/inscripciones", json={"codigo": "MS-ABC234"})
    r = c.post("/sesiones", json={"id_curso": "curso_1", "casos": ["caso_001"]})
    assert r.status_code == 201
    estado["db"].collection("sesiones").document(r.json()["id_sesion"]).set(
        {**r.json(), "estado": "completada"}
    )  # se cierra para poder abrir otra
    c.delete("/inscripciones/curso_1")
    r = c.post("/sesiones", json={"id_curso": "curso_1", "casos": ["caso_001"]})
    assert r.status_code == 403

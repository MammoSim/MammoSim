"""Pruebas de GET /casos/{id_caso}: el caso solo se entrega dentro de una sesión en curso."""
from datetime import datetime, timezone

PRIVADOS = (
    "respuesta_correcta", "explicacion", "opcion_ia_incorrecta",
    "recomendacion_ia_correcta", "recomendacion_ia_incorrecta",
    "hallazgos", "retroalimentacion",
)


def _claves(obj):
    """Todos los nombres de campo que aparecen en el objeto, a cualquier nivel."""
    if isinstance(obj, dict):
        for k, v in obj.items():
            yield k
            yield from _claves(v)
    elif isinstance(obj, list):
        for v in obj:
            yield from _claves(v)


def abre_sesion(db, id_sesion="s1", uid="uid_1", estado="en_curso", casos=("caso_001",)):
    db.collection("sesiones").document(id_sesion).set(
        {
            "id_sesion": id_sesion, "id_usuario": uid, "id_curso": None,
            "casos_asignados": list(casos), "estado": estado,
            "fecha_inicio": datetime(2026, 11, 1, tzinfo=timezone.utc), "fecha_fin": None,
        }
    )


def test_entrega_el_caso_dentro_de_la_sesion_en_curso(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    r = c.get("/casos/caso_001")
    assert r.status_code == 200
    caso = r.json()
    assert caso["id_caso"] == "caso_001"
    assert len(caso["preguntas"]) == 8  # 7 técnicas + la decisión diagnóstica (Unity)
    assert set(caso["preguntas"][0]) == {"numero", "tipo", "enunciado", "opciones"}
    assert caso["enunciado_decision"] and len(caso["opciones_decision"]) == 4
    assert caso["datos_estudio"] and caso["edad_simulada"]


def test_nunca_revela_la_respuesta_ni_la_ia_ni_la_explicacion(cliente):
    c, estado = cliente
    abre_sesion(estado["db"], casos=[f"caso_00{n}" for n in range(1, 8)])
    for n in range(1, 8):
        r = c.get(f"/casos/caso_00{n}")
        assert r.status_code == 200
        filtradas = set(_claves(r.json())) & set(PRIVADOS)
        assert not filtradas, f"caso_00{n} filtra {filtradas}"


def test_sin_sesion_en_curso_da_403(cliente):
    c, estado = cliente
    assert c.get("/casos/caso_001").status_code == 403


def test_caso_que_no_esta_en_su_sesion_da_403(cliente):
    c, estado = cliente
    abre_sesion(estado["db"], casos=["caso_001"])
    assert c.get("/casos/caso_002").status_code == 403


def test_sesion_completada_ya_no_da_acceso(cliente):
    c, estado = cliente
    abre_sesion(estado["db"], estado="completada")
    assert c.get("/casos/caso_001").status_code == 403


def test_la_sesion_de_otro_estudiante_no_da_acceso(cliente):
    c, estado = cliente
    abre_sesion(estado["db"], uid="uid_otro")
    assert c.get("/casos/caso_001").status_code == 403


def test_caso_que_no_existe_da_404(cliente):
    c, estado = cliente
    abre_sesion(estado["db"])
    assert c.get("/casos/caso_999").status_code == 404


def test_sin_registrarse_da_403(cliente):
    c, estado = cliente
    estado["rol"] = None
    abre_sesion(estado["db"])
    r = c.get("/casos/caso_001")
    assert r.status_code == 403
    assert r.json()["detail"] == "Primero debes registrarte"


def test_un_docente_no_puede_ver_casos_para_resolver(cliente):
    c, estado = cliente
    estado["rol"] = "docente"  # un docente no tiene sesiones propias
    assert c.get("/casos/caso_001").status_code == 403

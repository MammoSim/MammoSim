"""Integridad de los 7 casos de scripts/casos_clinicos.json (RF-004.1, RNF-001.1)."""
import pytest

DECISIONES = {"biopsiar", "seguimiento", "ignorar", "estudio_adicional"}
IDS = [f"caso_00{n}" for n in range(1, 8)]
OBLIGATORIOS = (
    "id_caso", "titulo", "descripcion_breve", "lado", "vistas", "dificultad", "edad_simulada",
    "modelo_3d_ref", "datos_estudio", "preguntas", "enunciado_decision", "opciones_decision",
    "hallazgos", "respuesta_correcta", "recomendacion_ia_correcta", "opcion_ia_incorrecta",
    "recomendacion_ia_incorrecta", "retroalimentacion", "fuente",
)


def test_hay_7_casos_con_id_igual_a_su_clave(casos):
    assert sorted(casos) == IDS
    assert all(c["id_caso"] == k for k, c in casos.items())


@pytest.mark.parametrize("id_caso", IDS)
def test_el_caso_trae_todos_los_campos_y_ninguno_vacio(casos, id_caso):
    for campo in OBLIGATORIOS:
        assert casos[id_caso].get(campo) not in (None, "", [], {}), f"{id_caso}: falta {campo}"


@pytest.mark.parametrize("id_caso", IDS)
def test_las_7_preguntas_estan_completas(casos, id_caso):
    preguntas = casos[id_caso]["preguntas"]
    assert [p["numero"] for p in preguntas] == [1, 2, 3, 4, 5, 6, 7]
    for p in preguntas:
        assert p["enunciado"].strip() and p["explicacion"].strip()
        assert list(p["opciones"]) == ["A", "B", "C", "D"]
        assert all(t.strip() for t in p["opciones"].values())
        assert p["respuesta_correcta"] in p["opciones"]


@pytest.mark.parametrize("id_caso", IDS)
def test_la_decision_tiene_4_opciones_y_una_correcta(casos, id_caso):
    c = casos[id_caso]
    valores = [o["valor"] for o in c["opciones_decision"]]
    assert set(valores) == DECISIONES and len(valores) == 4
    assert c["respuesta_correcta"] in valores


@pytest.mark.parametrize("id_caso", IDS)
def test_la_opcion_incorrecta_de_la_ia_es_valida_y_distinta_de_la_correcta(casos, id_caso):
    c = casos[id_caso]
    assert c["opcion_ia_incorrecta"] in DECISIONES
    assert c["opcion_ia_incorrecta"] != c["respuesta_correcta"]
    assert c["recomendacion_ia_correcta"] != c["recomendacion_ia_incorrecta"]


def test_la_dificultad_es_una_de_las_tres_y_hay_de_cada_una(casos):
    niveles = {c["dificultad"] for c in casos.values()}
    assert niveles == {"facil", "medio", "dificil"}


def test_cada_caso_indica_su_fuente_publica_anonimizada(casos):
    """RNF-001.1: casos de una base pública y anonimizada, con su procedencia."""
    for k, c in casos.items():
        assert "CBIS-DDSM" in c["fuente"], k

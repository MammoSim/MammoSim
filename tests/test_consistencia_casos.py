"""Consistencia entre los casos de Unity y los de Firestore.

Compara  unity/MammoSim/Assets/Resources/Casos/casos_mammosim.json  (lo que usa MockApi)
con  backend/scripts/casos_clinicos.json  (lo que sirve el backend; fixture `casos`).
Va en tests/ de la raíz porque necesita ver backend/ y unity/ a la vez.
Se ejecuta desde la raíz del repo:  python -m pytest tests
Si difieren en preguntas, opciones o respuestas, el estudiante vería algo distinto según
use el mock o la API real.
"""
import json
from pathlib import Path

import pytest

UNITY = (
    Path(__file__).resolve().parents[1]
    / "unity" / "MammoSim" / "Assets" / "Resources" / "Casos" / "casos_mammosim.json"
)

# Campos que deben ser idénticos en ambos archivos.
CAMPOS_IGUALES = (
    "descripcion_breve", "lado", "vistas", "edad_simulada", "modelo_3d_ref",
    "respuesta_correcta", "opcion_ia_incorrecta",
    "recomendacion_ia_correcta", "recomendacion_ia_incorrecta", "retroalimentacion",
)


@pytest.fixture(scope="module")
def unity() -> dict:
    if not UNITY.exists():
        pytest.skip(f"No existe {UNITY}")
    datos = json.loads(UNITY.read_text(encoding="utf-8"))
    return {c["id_caso"]: c for c in datos["casos"]}


def _tecnicas(caso_unity):
    return [p for p in caso_unity["preguntas"] if p["tipo"] == "tecnica"]


def _diagnostica(caso_unity):
    return [p for p in caso_unity["preguntas"] if p["tipo"] == "diagnostica"]


def test_unity_y_backend_tienen_los_mismos_casos(casos, unity):
    assert sorted(casos) == sorted(unity)


@pytest.mark.parametrize("campo", CAMPOS_IGUALES)
def test_campo_igual_en_los_7_casos(casos, unity, campo):
    distintos = [c for c in casos if casos[c][campo] != unity[c][campo]]
    assert not distintos, f"'{campo}' difiere en {distintos}"


def test_cada_caso_de_unity_tiene_7_tecnicas_y_1_diagnostica(unity):
    for cid, caso in unity.items():
        assert len(_tecnicas(caso)) == 7, cid
        assert len(_diagnostica(caso)) == 1, cid
        assert _diagnostica(caso)[0]["numero"] == 8, cid


def test_preguntas_tecnicas_iguales(casos, unity):
    for cid, backend in casos.items():
        for b, u in zip(backend["preguntas"], _tecnicas(unity[cid])):
            donde = f"{cid} p{b['numero']}"
            assert b["numero"] == u["numero"], donde
            assert b["enunciado"] == u["enunciado"], donde
            # Backend: {"A": texto}; Unity: [{"id": "A", "texto": texto}]
            assert b["opciones"] == {o["id"]: o["texto"] for o in u["opciones"]}, donde
            assert [o["id"] for o in u["opciones"]] == ["A", "B", "C", "D"], donde
            assert b["respuesta_correcta"] == u["respuesta_correcta"], donde
            assert b["explicacion"] == u["explicacion"], donde


def test_decision_igual_en_texto_orden_y_respuesta(casos, unity):
    for cid, backend in casos.items():
        d = _diagnostica(unity[cid])[0]
        assert backend["enunciado_decision"] == d["enunciado"], cid
        # Mismo orden de opciones: es el que ve el estudiante.
        assert [(o["valor"], o["texto"]) for o in backend["opciones_decision"]] == \
               [(o["id"], o["texto"]) for o in d["opciones"]], cid
        assert backend["respuesta_correcta"] == d["respuesta_correcta"], cid
        assert backend["retroalimentacion"] == d["explicacion"], cid


def test_datos_estudio_igual(casos, unity):
    # Backend: lista de líneas; Unity: una cadena con saltos de línea.
    for cid, backend in casos.items():
        assert "\n".join(backend["datos_estudio"]) == unity[cid]["datos_estudio"], cid


# ---- DEFECTO CONOCIDO: en casos_mammosim.json todos los casos traen "dificultad": "" ----
# xfail estricto: la suite sigue en verde y esta prueba avisa (XPASS) cuando se corrija el JSON de Unity.
@pytest.mark.xfail(strict=True, reason="casos_mammosim.json trae dificultad vacia en los 7 casos")
def test_dificultad_igual_y_no_vacia(casos, unity):
    vacios = [c for c in unity if not unity[c]["dificultad"]]
    assert not vacios, f"dificultad vacía en {vacios}"
    for cid in casos:
        assert casos[cid]["dificultad"] == unity[cid]["dificultad"], cid


# ---- Las dos copias de casos_clinicos.json (raíz y backend) no deben divergir ----
RAIZ = Path(__file__).resolve().parents[1] / "scripts" / "casos_clinicos.json"
BACKEND = Path(__file__).resolve().parents[1] / "backend" / "scripts" / "casos_clinicos.json"


def test_las_dos_copias_de_casos_clinicos_son_identicas():
    if not (RAIZ.exists() and BACKEND.exists()):
        pytest.skip("Falta una de las dos copias de casos_clinicos.json")
    assert json.loads(RAIZ.read_text(encoding="utf-8")) == json.loads(BACKEND.read_text(encoding="utf-8"))

"""Tutor: la regla se cumple en TODAS las combinaciones de decisiones de los 7 casos (RF-004.1, RF-005.2)."""
import copy

import pytest

from app.services.tutor import evaluar_decision_final, recomendar

DECISIONES = ["biopsiar", "seguimiento", "ignorar", "estudio_adicional"]
IDS = [f"caso_00{n}" for n in range(1, 8)]


@pytest.mark.parametrize("id_caso", IDS)
def test_las_16_combinaciones_cumplen_las_reglas(casos, id_caso):
    caso = casos[id_caso]
    for inicial in DECISIONES:
        for final in DECISIONES:
            r = evaluar_decision_final(caso, inicial, final)
            assert r["acierto"] == (final == caso["respuesta_correcta"])
            assert r["siguio_ia"] == (final == r["opcion_ia"])
            assert r["ia_correcta"] == (inicial != caso["respuesta_correcta"])
            if r["ia_correcta"]:
                assert r["opcion_ia"] == caso["respuesta_correcta"]
            else:
                assert r["opcion_ia"] != caso["respuesta_correcta"]


def test_si_acerto_y_siguio_a_la_ia_la_decision_final_es_incorrecta(casos):
    for caso in casos.values():
        r = evaluar_decision_final(caso, caso["respuesta_correcta"], caso["opcion_ia_incorrecta"])
        assert r["siguio_ia"] and not r["acierto"]


def test_la_recomendacion_solo_trae_los_tres_campos(casos):
    for caso in casos.values():
        assert set(recomendar(caso, "ignorar")) == {"opcion_ia", "texto", "ia_correcta"}


def test_la_evaluacion_final_solo_trae_los_campos_esperados(casos):
    r = evaluar_decision_final(casos["caso_001"], "ignorar", "biopsiar")
    assert set(r) == {"decision", "acierto", "opcion_ia", "ia_correcta", "siguio_ia"}


def test_recomendar_no_modifica_el_caso(casos):
    caso = casos["caso_001"]
    antes = copy.deepcopy(caso)
    recomendar(caso, "ignorar")
    evaluar_decision_final(caso, "biopsiar", "seguimiento")
    assert caso == antes


def test_una_decision_inicial_desconocida_se_trata_como_incorrecta(casos):
    """Comportamiento actual del servicio (la API ya la rechaza con 422 antes de llegar aquí)."""
    rec = recomendar(casos["caso_001"], "operar")
    assert rec["ia_correcta"] is True and rec["opcion_ia"] == casos["caso_001"]["respuesta_correcta"]

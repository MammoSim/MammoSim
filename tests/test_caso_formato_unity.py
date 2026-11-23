"""Formato del caso que el backend entrega a Unity (services/casos.py: caso_para_resolver).

Unity espera una sola lista de 8 preguntas: las 7 técnicas (opciones A-D) y, como pregunta 8,
la decisión diagnóstica. Cada pregunta trae numero, tipo, enunciado y opciones [{id, texto}].
Nunca viaja la respuesta correcta ni la IA. Se ejecuta desde la raíz:  python -m pytest tests
"""
from app.services.casos import caso_para_resolver

TIPOS = ["tecnica"] * 7 + ["diagnostica"]


def test_entrega_8_preguntas_ordenadas_del_1_al_8(casos):
    for caso in casos.values():
        preguntas = caso_para_resolver(caso)["preguntas"]
        assert [p["numero"] for p in preguntas] == list(range(1, 9)), caso["id_caso"]


def test_las_7_primeras_son_tecnicas_y_la_8_es_diagnostica(casos):
    for caso in casos.values():
        preguntas = caso_para_resolver(caso)["preguntas"]
        assert [p["tipo"] for p in preguntas] == TIPOS, caso["id_caso"]


def test_cada_pregunta_trae_solo_numero_tipo_enunciado_y_opciones(casos):
    for caso in casos.values():
        for p in caso_para_resolver(caso)["preguntas"]:
            assert set(p) == {"numero", "tipo", "enunciado", "opciones"}, caso["id_caso"]
            assert p["enunciado"], caso["id_caso"]


def test_las_opciones_son_una_lista_de_id_y_texto(casos):
    for caso in casos.values():
        for p in caso_para_resolver(caso)["preguntas"]:
            assert isinstance(p["opciones"], list), caso["id_caso"]
            assert all(set(o) == {"id", "texto"} and o["texto"] for o in p["opciones"])


def test_las_tecnicas_tienen_las_opciones_A_B_C_D_en_orden(casos):
    for caso in casos.values():
        for p in caso_para_resolver(caso)["preguntas"][:7]:
            assert [o["id"] for o in p["opciones"]] == ["A", "B", "C", "D"], caso["id_caso"]


def test_la_tecnica_conserva_el_texto_de_cada_opcion(casos):
    caso = casos["caso_001"]
    entregada = caso_para_resolver(caso)["preguntas"][0]
    original = caso["preguntas"][0]
    assert {o["id"]: o["texto"] for o in entregada["opciones"]} == original["opciones"]
    assert entregada["enunciado"] == original["enunciado"]


def test_la_pregunta_8_usa_el_enunciado_y_las_opciones_de_la_decision(casos):
    for caso in casos.values():
        p8 = caso_para_resolver(caso)["preguntas"][7]
        assert p8["enunciado"] == caso["enunciado_decision"], caso["id_caso"]
        assert [(o["id"], o["texto"]) for o in p8["opciones"]] == [
            (o["valor"], o["texto"]) for o in caso["opciones_decision"]
        ], caso["id_caso"]


def test_las_cuatro_decisiones_posibles_estan_en_la_pregunta_8(casos):
    esperadas = {"biopsiar", "seguimiento", "estudio_adicional", "ignorar"}
    for caso in casos.values():
        ids = {o["id"] for o in caso_para_resolver(caso)["preguntas"][7]["opciones"]}
        assert ids == esperadas, caso["id_caso"]


def test_no_modifica_el_caso_original(casos):
    caso = casos["caso_001"]
    antes = repr(caso)
    caso_para_resolver(caso)
    assert repr(caso) == antes

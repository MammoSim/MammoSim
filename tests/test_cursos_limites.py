"""Cursos: nombre, código de acceso y colisiones de código."""
import pytest

from app.services.cursos import ALFABETO, generar_codigo


def como_docente(estado):
    estado["rol"] = "docente"


def test_el_alfabeto_no_tiene_caracteres_que_se_confundan():
    assert not set("01ILO") & set(ALFABETO)
    assert len(ALFABETO) == len(set(ALFABETO))


def test_los_codigos_solo_usan_el_alfabeto_sin_confusiones():
    for _ in range(500):
        codigo = generar_codigo()
        assert codigo.startswith("MS-") and len(codigo) == 9
        assert set(codigo[3:]) <= set(ALFABETO)


def test_un_nombre_de_dos_caracteres_se_acepta(cliente):
    c, estado = cliente
    como_docente(estado)
    assert c.post("/cursos", json={"nombre": "AB"}).status_code == 201


def test_un_nombre_de_solo_espacios_da_422(cliente):
    c, estado = cliente
    como_docente(estado)
    assert c.post("/cursos", json={"nombre": "   "}).status_code == 422


def test_el_cuerpo_no_puede_imponer_el_docente_ni_el_codigo(cliente):
    c, estado = cliente
    como_docente(estado)
    r = c.post("/cursos", json={"nombre": "Mi curso", "id_docente": "otro", "codigo": "MS-AAAAAA"})
    assert r.status_code == 201
    assert r.json()["id_docente"] == "uid_1"
    assert r.json()["codigo"] != "MS-AAAAAA"


def test_si_el_codigo_ya_existe_se_genera_otro(cliente, monkeypatch):
    c, estado = cliente
    como_docente(estado)
    estado["db"].collection("cursos").document("ajeno").set(
        {"codigo": "MS-AAAAAA", "nombre": "De otro", "id_docente": "otro_docente"}
    )
    codigos = iter(["MS-AAAAAA", "MS-BBBBBB"])
    monkeypatch.setattr("app.routers.cursos.generar_codigo", lambda: next(codigos))
    r = c.post("/cursos", json={"nombre": "Mi curso"})
    assert r.status_code == 201 and r.json()["codigo"] == "MS-BBBBBB"


def test_si_los_10_intentos_chocan_responde_500_y_no_guarda(cliente, monkeypatch):
    c, estado = cliente
    como_docente(estado)
    estado["db"].collection("cursos").document("ajeno").set(
        {"codigo": "MS-AAAAAA", "nombre": "De otro", "id_docente": "otro_docente"}
    )
    monkeypatch.setattr("app.routers.cursos.generar_codigo", lambda: "MS-AAAAAA")
    assert c.post("/cursos", json={"nombre": "Mi curso"}).status_code == 500
    assert len(estado["db"].collection("cursos").stream()) == 1

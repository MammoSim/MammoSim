"""Registro (POST /auth/registro): valores límite y datos que el cliente no puede imponer. RF-001.1"""
import pytest

from apoyo_backend import CUERPO, guardado, registro  # noqa: F401  (usa su fixture)


@pytest.mark.parametrize("semestre", [1, 14])
def test_los_semestres_limite_se_aceptan(registro, semestre):
    c, db, _ = registro
    r = c.post("/auth/registro", json={**CUERPO, "semestre": semestre})
    assert r.status_code == 201
    assert r.json()["usuario"]["semestre"] == semestre


def test_nombre_de_un_caracter_da_422_y_de_dos_se_acepta(registro):
    c, db, _ = registro
    assert c.post("/auth/registro", json={**CUERPO, "nombre_completo": "A"}).status_code == 422
    assert guardado(db, "nuevo_1") is None
    assert c.post("/auth/registro", json={**CUERPO, "nombre_completo": "Al"}).status_code == 201


def test_sin_el_campo_acepto_terminos_da_422_y_no_guarda(registro):
    c, db, _ = registro
    cuerpo = {k: v for k, v in CUERPO.items() if k != "acepto_terminos"}
    assert c.post("/auth/registro", json=cuerpo).status_code == 422
    assert guardado(db, "nuevo_1") is None


def test_semestre_que_no_es_numero_da_422(registro):
    c, db, _ = registro
    assert c.post("/auth/registro", json={**CUERPO, "semestre": "ocho"}).status_code == 422
    assert guardado(db, "nuevo_1") is None


def test_la_carrera_se_guarda_sin_espacios_sobrantes(registro):
    c, db, _ = registro
    c.post("/auth/registro", json={**CUERPO, "carrera": "  Medicina  "})
    assert guardado(db, "nuevo_1")["carrera"] == "Medicina"


def test_el_docente_autorizado_no_recibe_aviso(registro):
    c, db, token = registro
    token.update(uid="doc_1", email="profe@usc.edu.co")
    r = c.post("/auth/registro", json=CUERPO)
    assert r.json()["usuario"]["rol"] == "docente"
    assert r.json()["aviso"] is None


def test_el_cuerpo_no_puede_imponer_uid_ni_correo(registro):
    """El uid y el correo salen del token, nunca del cuerpo."""
    c, db, _ = registro
    r = c.post("/auth/registro", json={**CUERPO, "id_usuario": "otro", "correo": "otro@x.com"})
    assert r.status_code == 201
    assert r.json()["usuario"]["id_usuario"] == "nuevo_1"
    assert r.json()["usuario"]["correo"] == "marlin@correo.com"
    assert guardado(db, "otro") is None

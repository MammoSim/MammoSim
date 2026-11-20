"""Autenticación: lo que pasa cuando el token de Firebase es inválido o trae datos de más."""
import pytest
from fastapi.testclient import TestClient

from app import auth
from app.firebase import get_db
from app.main import app
from apoyo_backend import FakeDb


@pytest.fixture
def sin_overrides(monkeypatch):
    """Sin sustituir get_token_data: se prueba la verificación real, con Firebase simulado."""
    db = FakeDb({"usuarios": {"u1": {
        "id_usuario": "u1", "nombre_completo": "Ana", "correo": "ana@x.com",
        "rol": "estudiante", "carrera": "Medicina", "semestre": 5, "acepto_terminos": True}}})
    app.dependency_overrides[get_db] = lambda: db
    monkeypatch.setattr(auth, "init_firebase", lambda: None)
    yield TestClient(app), monkeypatch
    app.dependency_overrides.clear()


def test_token_invalido_responde_401(sin_overrides):
    c, mp = sin_overrides

    def falla(_token):
        raise ValueError("token malo")

    mp.setattr(auth.fb_auth, "verify_id_token", falla)
    r = c.get("/usuarios/me", headers={"Authorization": "Bearer malo"})
    assert r.status_code == 401
    assert r.json()["detail"] == "Token inválido o vencido"


def test_encabezado_que_no_es_bearer_responde_401(sin_overrides):
    c, _ = sin_overrides
    assert c.get("/usuarios/me", headers={"Authorization": "Basic abc"}).status_code == 401


def test_token_valido_entrega_el_perfil(sin_overrides):
    c, mp = sin_overrides
    mp.setattr(auth.fb_auth, "verify_id_token", lambda _t: {"uid": "u1", "email": "ANA@x.com"})
    r = c.get("/usuarios/me", headers={"Authorization": "Bearer bueno"})
    assert r.status_code == 200 and r.json()["id_usuario"] == "u1"


def test_el_rol_sale_de_la_base_y_no_del_token(sin_overrides):
    """Un token que diga role = docente no da acceso: el rol se lee de usuarios."""
    c, mp = sin_overrides
    mp.setattr(auth.fb_auth, "verify_id_token",
               lambda _t: {"uid": "u1", "email": "ana@x.com", "role": "docente", "rol": "docente"})
    assert c.get("/cursos/mio", headers={"Authorization": "Bearer x"}).status_code == 403


def test_el_correo_del_usuario_actual_se_pasa_a_minusculas(sin_overrides):
    _, mp = sin_overrides
    mp.setattr(auth.fb_auth, "verify_id_token", lambda _t: {"uid": "u1", "email": "ANA@X.COM"})
    db = FakeDb({"usuarios": {"u1": {"rol": "estudiante"}}})
    actual = auth.get_usuario_actual({"uid": "u1", "email": "ANA@X.COM"}, db)
    assert actual.correo == "ana@x.com" and actual.rol == "estudiante"


def test_usuario_sin_documento_queda_sin_rol():
    actual = auth.get_usuario_actual({"uid": "nadie", "email": "n@x.com"}, FakeDb({}))
    assert actual.rol is None

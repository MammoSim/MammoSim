"""Pruebas de seguridad de la API (RNF de seguridad): autenticación, entradas maliciosas, CORS y fugas de información.

Se ejecutan con la base falsa (FakeDb), sin Firebase ni red:
    python -m pytest tests/seguridad -v
"""
import json

import pytest
from fastapi.testclient import TestClient

from app import auth
from app.firebase import get_db
from app.main import app
from apoyo_backend import FakeDb, casos, cliente  # noqa: F401  (fixtures reutilizados)

LIBRES = {"/salud", "/docs", "/redoc", "/openapi.json", "/docs/oauth2-redirect"}


def _rutas_protegidas():
    """Rutas tomadas del esquema OpenAPI (incluye las de los routers)."""
    salida = []
    for path, ops in app.openapi()["paths"].items():
        if path in LIBRES:
            continue
        for m in ops:
            salida.append((m.upper(), path))
    return sorted(salida)


RUTAS = _rutas_protegidas()


def _url(path):
    import re
    return re.sub(r"\{[^}]+\}", "x", path)


@pytest.fixture
def anonimo(casos):
    db = FakeDb({"casos_clinicos": casos, "usuarios": {}})
    app.dependency_overrides[get_db] = lambda: db
    yield TestClient(app)
    app.dependency_overrides.clear()


def test_hay_al_menos_22_rutas_protegidas():
    assert len(RUTAS) >= 22, len(RUTAS)


@pytest.mark.parametrize("metodo,ruta", RUTAS)
def test_sin_token_responde_401(anonimo, metodo, ruta):
    r = anonimo.request(metodo, _url(ruta), json={} if metodo in ("POST", "PUT", "PATCH") else None)
    assert r.status_code == 401, (metodo, ruta, r.status_code)


@pytest.mark.parametrize("metodo,ruta", RUTAS)
def test_token_invalido_responde_401(anonimo, monkeypatch, metodo, ruta):
    monkeypatch.setattr(auth, "init_firebase", lambda: None)

    def falla(_t):
        raise ValueError("token manipulado")

    monkeypatch.setattr(auth.fb_auth, "verify_id_token", falla)
    r = anonimo.request(metodo, _url(ruta), headers={"Authorization": "Bearer abc.def.ghi"},
                        json={} if metodo in ("POST", "PUT", "PATCH") else None)
    assert r.status_code == 401, (metodo, ruta, r.status_code)
    assert "abc.def.ghi" not in r.text


PAYLOADS_RUTA = [
    "../../etc/passwd", "' OR '1'='1", "<script>alert(1)</script>", "%00", "a" * 3000,
    "caso_001;DROP TABLE", "${jndi:ldap://x}",
]


@pytest.mark.parametrize("payload", PAYLOADS_RUTA)
@pytest.mark.parametrize("ruta", ["/casos/{p}", "/sesiones/{p}/resultado", "/sesiones/{p}/detalle",
                                   "/sesiones/{p}/casos/{p}/progreso", "/cursos/mio/sesiones/{p}"])
def test_ids_maliciosos_no_causan_error_500(cliente, payload, ruta):
    c, estado = cliente
    r = c.get(ruta.replace("{p}", payload))
    assert r.status_code < 500, (ruta, r.status_code)
    assert r.status_code != 200
    assert "Traceback" not in r.text


CUERPOS = [
    {}, None, [], "texto", 123, {"nombre_completo": None, "carrera": None, "semestre": None, "acepto_terminos": None},
    {"nombre_completo": "x" * 100000, "carrera": "y" * 100000, "semestre": 8, "acepto_terminos": True},
    {"nombre_completo": "' OR 1=1 --", "carrera": "<script>alert(1)</script>", "semestre": 8, "acepto_terminos": True},
    {"nombre_completo": {"a": {"b": {"c": 1}}}, "carrera": [1, 2], "semestre": "ocho", "acepto_terminos": "si"},
    {"nombre_completo": "Ana", "carrera": "Med", "semestre": 10 ** 30, "acepto_terminos": True},
    {"nombre_completo": "Ana", "carrera": "Med", "semestre": -1, "acepto_terminos": True},
    {"nombre": "x", "id_docente": "otro", "codigo": "MS-AAAAAA", "casos": ["a"], "id_caso": ["a"]},
]
POSTS = ["/auth/registro", "/cursos", "/sesiones", "/inscripciones"]


@pytest.mark.parametrize("cuerpo", CUERPOS, ids=lambda c: type(c).__name__ + str(abs(hash(json.dumps(c, default=str))) % 1000))
@pytest.mark.parametrize("ruta", POSTS)
def test_cuerpos_raros_nunca_dan_500(cliente, ruta, cuerpo):
    c, estado = cliente
    for rol in ("estudiante", "docente"):
        estado["rol"] = rol
        r = c.post(ruta, json=cuerpo)
        assert r.status_code < 500, (ruta, rol, r.status_code, r.text[:200])
        assert "Traceback" not in r.text


@pytest.mark.parametrize("ruta", POSTS)
def test_json_mal_formado_da_422(cliente, ruta):
    c, _ = cliente
    r = c.post(ruta, content=b"{esto no es json", headers={"Content-Type": "application/json"})
    assert r.status_code == 422


@pytest.mark.parametrize("metodo,ruta", [("PUT", "/casos"), ("DELETE", "/casos"), ("PATCH", "/usuarios/me"),
                                          ("DELETE", "/sesiones/mias"), ("POST", "/salud")])
def test_metodo_no_permitido_da_405(cliente, metodo, ruta):
    c, _ = cliente
    assert c.request(metodo, ruta).status_code == 405


def test_un_error_404_no_filtra_detalles_internos(cliente):
    c, _ = cliente
    r = c.get("/ruta/que/no/existe")
    assert r.status_code == 404
    for pista in ("Traceback", 'File "', "firebase", "site-packages"):
        assert pista not in r.text


def test_cors_no_permite_un_origen_desconocido(cliente):
    c, _ = cliente
    r = c.options("/casos", headers={"Origin": "https://sitio-malicioso.example",
                                     "Access-Control-Request-Method": "GET"})
    assert "access-control-allow-origin" not in {k.lower() for k in r.headers}


def test_cors_permite_el_origen_del_frontend(cliente):
    c, _ = cliente
    r = c.options("/casos", headers={"Origin": "http://localhost:5173",
                                     "Access-Control-Request-Method": "GET"})
    assert r.headers.get("access-control-allow-origin") == "http://localhost:5173"


def test_el_perfil_no_expone_datos_de_otros_usuarios(cliente):
    c, estado = cliente
    estado["db"].collection("usuarios").document("uid_2").set({
        "id_usuario": "uid_2", "nombre_completo": "Otra Persona", "correo": "otra@correo.com",
        "rol": "estudiante", "carrera": "Medicina", "semestre": 3, "acepto_terminos": True})
    r = c.get("/usuarios/me")
    assert "otra@correo.com" not in r.text and "Otra Persona" not in r.text


def test_la_documentacion_automatica_esta_publica_informativo(cliente):
    """Hallazgo informativo: /docs y /openapi.json responden sin token (normal en desarrollo)."""
    c, _ = cliente
    assert c.get("/openapi.json").status_code in (200, 404)


@pytest.mark.xfail(strict=True, reason="Defecto de bajo riesgo: la API no envía X-Content-Type-Options: nosniff")
def test_encabezado_x_content_type_options(cliente):
    c, _ = cliente
    assert c.get("/salud").headers.get("x-content-type-options") == "nosniff"

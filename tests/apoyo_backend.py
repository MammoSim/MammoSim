"""Apoyo para las pruebas de la carpeta tests/ de la raíz.

Reutiliza la base falsa (FakeDb) y los fixtures de backend/tests/conftest.py
sin copiarlos, para que no se desactualicen cuando el backend cambie.
"""
import importlib.util
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from app.auth import get_token_data
from app.firebase import get_db
from app.main import app

_RUTA = Path(__file__).resolve().parents[1] / "backend" / "tests" / "conftest.py"
_spec = importlib.util.spec_from_file_location("backend_conftest", _RUTA)
_backend = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_backend)

FakeDb = _backend.FakeDb
casos = _backend.casos
cliente = _backend.cliente

CUERPO = {
    "nombre_completo": "  Marlin Pérez  ",
    "carrera": "Ingeniería de Sistemas",
    "semestre": 8,
    "acepto_terminos": True,
}


@pytest.fixture
def registro():
    """Cliente con una base vacía y un token que se puede cambiar."""
    db = FakeDb({"docentes_autorizados": {"profe@usc.edu.co": {"activo": True}}})
    token = {"uid": "nuevo_1", "email": "Marlin@Correo.com"}
    app.dependency_overrides[get_db] = lambda: db
    app.dependency_overrides[get_token_data] = lambda: token
    yield TestClient(app), db, token
    app.dependency_overrides.clear()


def guardado(db, uid):
    return db.collection("usuarios").document(uid).get().to_dict()

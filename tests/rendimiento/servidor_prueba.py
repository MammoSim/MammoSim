"""Levanta la API de MammoSim con base de datos simulada (sin Firebase) para pruebas de rendimiento."""
import json, sys
from pathlib import Path
RAIZ = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(RAIZ / "backend")); sys.path.insert(0, str(RAIZ / "backend" / "tests"))
from conftest import FakeDb
from app.auth import UsuarioActual, get_token_data, get_usuario_actual
from app.firebase import get_db
from app.main import app
casos = json.loads((RAIZ / "backend/scripts/casos_clinicos.json").read_text(encoding="utf-8"))
db = FakeDb({"casos_clinicos": casos, "usuarios": {"uid_1": {
    "id_usuario": "uid_1", "nombre_completo": "Ana Prueba", "correo": "ana@correo.com",
    "rol": "estudiante", "carrera": "Medicina", "semestre": 8, "acepto_terminos": True}},
    "sesiones": {"ses_1": {"id_sesion": "ses_1", "id_usuario": "uid_1", "estado": "en_curso", "fecha_inicio": "2026-11-26T10:00:00Z", "casos_asignados": list(casos.keys())}}})
app.dependency_overrides[get_db] = lambda: db
app.dependency_overrides[get_token_data] = lambda: {"uid": "uid_1", "email": "ana@correo.com"}
app.dependency_overrides[get_usuario_actual] = lambda: UsuarioActual(uid="uid_1", correo="ana@correo.com", rol="estudiante")

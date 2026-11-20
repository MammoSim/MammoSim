"""Configuración: orígenes permitidos (CORS)."""
from app.config import Settings


def test_los_origenes_se_separan_por_comas_y_se_limpian():
    s = Settings(_env_file=None, cors_origins="http://a.com, http://b.com ,,  ")
    assert s.origenes_permitidos == ["http://a.com", "http://b.com"]


def test_origen_por_defecto_es_el_de_vite(monkeypatch):
    monkeypatch.delenv("CORS_ORIGINS", raising=False)
    assert Settings(_env_file=None).origenes_permitidos == ["http://localhost:5173"]


def test_sin_origenes_la_lista_queda_vacia():
    assert Settings(_env_file=None, cors_origins=" , ").origenes_permitidos == []

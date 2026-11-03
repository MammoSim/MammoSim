"""Endpoints 1 y 2: usuarios."""
from fastapi import APIRouter, Depends, HTTPException

from app.auth import UsuarioActual, get_token_data, get_usuario_actual
from app.firebase import get_db
from app.schemas import RegistroIn, RegistroOut, UsuarioOut
from app.utils import pendiente

router = APIRouter(tags=["usuarios"])


@router.post("/auth/registro", response_model=RegistroOut, status_code=201)
def registrar(
    datos: RegistroIn, token: dict = Depends(get_token_data), db=Depends(get_db)
):
    """1. Crea usuarios/{uid}.

    TODO:
    - 422 si acepto_terminos es False.
    - 409 si usuarios/{uid} ya existe.
    - Rol: docente si el correo (en minúsculas) está en docentes_autorizados con
      activo=True; si no, estudiante (con aviso si el correo es institucional).
    - Guardar fecha_aceptacion y fecha_registro.
    """
    pendiente()


@router.get("/usuarios/me", response_model=UsuarioOut)
def mi_perfil(
    usuario: UsuarioActual = Depends(get_usuario_actual), db=Depends(get_db)
):
    """2. Perfil del usuario que inició sesión (ya implementado, sirve de modelo)."""
    doc = db.collection("usuarios").document(usuario.uid).get()
    if not doc.exists:
        raise HTTPException(404, "Usuario no registrado")
    return doc.to_dict()

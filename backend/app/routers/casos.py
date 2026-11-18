"""Endpoints 8 y 9: casos clínicos."""
from fastapi import APIRouter, Depends, HTTPException

from app.auth import UsuarioActual, requiere_registrado
from app.firebase import get_db
from app.routers.sesiones import _sesion_en_curso
from app.schemas import CasoResumen
from app.services.casos import caso_para_resolver, resumen_caso

router = APIRouter(tags=["casos"])


@router.get("/casos", response_model=list[CasoResumen])
def listar_casos(
    usuario: UsuarioActual = Depends(requiere_registrado), db=Depends(get_db)
):
    """8. Lista de casos sin ningún campo que revele la respuesta.
    Cualquier rol, pero exige estar registrado (403 si no)."""
    docs = db.collection("casos_clinicos").stream()
    casos = [resumen_caso(d.to_dict()) for d in docs]
    return sorted(casos, key=lambda c: c["id_caso"])


@router.get("/casos/{id_caso}")
def ver_caso(
    id_caso: str,
    usuario: UsuarioActual = Depends(requiere_registrado),
    db=Depends(get_db),
):
    """9. El caso para resolverlo, sin respuestas ni explicaciones.
    Solo se entrega si el caso está en la sesión en_curso del estudiante
    (403 si no); sin esa comprobación se podrían leer las preguntas fuera del examen."""
    doc = db.collection("casos_clinicos").document(id_caso).get()
    if not doc.exists:
        raise HTTPException(404, "El caso no existe")

    sesion = _sesion_en_curso(db, usuario.uid)
    if sesion is None or id_caso not in sesion.to_dict()["casos_asignados"]:
        raise HTTPException(403, "Ese caso no está en tu sesión en curso")

    return caso_para_resolver(doc.to_dict())  # lista blanca: solo sale lo permitido

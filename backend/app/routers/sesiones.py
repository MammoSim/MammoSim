"""Endpoints 10, 11, 12, 18 y 21: sesiones y resultados.

MODO EXAMEN: una vez que el estudiante empieza una sesión no puede salirse
ni repetirla. Solo puede haber una sesión en_curso por estudiante y no se
puede cerrar a medias.
"""
from fastapi import APIRouter, Depends

from app.auth import UsuarioActual, requiere_estudiante
from app.firebase import get_db
from app.schemas import ResultadoOut, SesionActivaOut, SesionIn, SesionOut
from app.utils import pendiente

router = APIRouter(tags=["sesiones"])


@router.post("/sesiones", response_model=SesionOut, status_code=201)
def crear_sesion(
    datos: SesionIn,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """10. TODO:
    - 409 si ya tiene una sesión en_curso.
    - Si trae id_curso: la inscripción debe estar activa (403 si no).
    - Validar que los casos existan.
    - Crear sesiones/{id} con estado='en_curso', casos_asignados y fecha_inicio."""
    pendiente()


# OJO: estas dos rutas fijas van ANTES de las que llevan {id_sesion}.
@router.get("/sesiones/mias", response_model=list[SesionOut])
def mis_sesiones(
    id_curso: str | None = None,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """11. TODO: historial, de la más reciente a la más antigua (índice
    id_usuario + fecha_inicio). Incluye las prácticas libres."""
    pendiente()


@router.get("/sesiones/activa", response_model=SesionActivaOut)
def sesion_activa(
    estudiante: UsuarioActual = Depends(requiere_estudiante), db=Depends(get_db)
):
    """21. TODO: la sesión en_curso, con los casos ya terminados y el caso donde va,
    para continuar la misma si cerró la ventana. 404 si no tiene."""
    pendiente()


@router.post("/sesiones/{id_sesion}/cerrar", response_model=ResultadoOut)
def cerrar_sesion(
    id_sesion: str,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """12. TODO:
    - 403 si la sesión no es suya.
    - 409 si ya está completada o si falta terminar algún caso.
    - estado='completada', fecha_fin; crear resultados/{id_sesion} usando
      services.puntuacion.puntuacion_sesion."""
    pendiente()


@router.get("/sesiones/{id_sesion}/resultado", response_model=ResultadoOut)
def ver_resultado(
    id_sesion: str,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """18. TODO: resultados/{id_sesion}; 409 si la sesión no está completada."""
    pendiente()

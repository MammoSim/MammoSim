"""Endpoints 3 a 7 (cursos e inscripciones) y 19 y 20 (vista del docente)."""
from fastapi import APIRouter, Depends

from app.auth import UsuarioActual, requiere_docente, requiere_estudiante
from app.firebase import get_db
from app.schemas import CursoIn, CursoOut, InscripcionIn
from app.utils import pendiente

router = APIRouter(tags=["cursos"])


@router.post("/cursos", response_model=CursoOut, status_code=201)
def crear_curso(
    datos: CursoIn, docente: UsuarioActual = Depends(requiere_docente), db=Depends(get_db)
):
    """3. TODO: 409 si el docente ya tiene curso; generar un codigo único (ej. GPTI-2026B)."""
    pendiente()


@router.get("/cursos/mio", response_model=CursoOut)
def mi_curso(docente: UsuarioActual = Depends(requiere_docente), db=Depends(get_db)):
    """4. TODO: curso con id_docente == uid; 404 si no tiene."""
    pendiente()


@router.post("/inscripciones", response_model=CursoOut)
def unirse_a_curso(
    datos: InscripcionIn,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """5. TODO: buscar el curso por codigo (404); crear o reactivar
    inscripciones/{uid}_{id_curso} con activo=True."""
    pendiente()


@router.delete("/inscripciones/{id_curso}", status_code=204)
def salirse_del_curso(
    id_curso: str,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """6. TODO: activo=False (el historial se conserva); 404 si no estaba inscrito."""
    pendiente()


@router.get("/inscripciones/mias", response_model=list[CursoOut])
def mis_cursos(
    estudiante: UsuarioActual = Depends(requiere_estudiante), db=Depends(get_db)
):
    """7. TODO: cursos donde la inscripción esté activa."""
    pendiente()


@router.get("/cursos/mio/sesiones")
def sesiones_de_mi_curso(
    docente: UsuarioActual = Depends(requiere_docente), db=Depends(get_db)
):
    """19. TODO: sesiones con id_curso == el de su curso, con nombre del estudiante,
    estado, fecha_inicio y puntuacion. Puede pedir índice (id_curso + fecha_inicio)."""
    pendiente()


@router.get("/cursos/mio/sesiones/{id_sesion}")
def detalle_de_sesion(
    id_sesion: str,
    docente: UsuarioActual = Depends(requiere_docente),
    db=Depends(get_db),
):
    """20. TODO: respuestas, decisiones inicial y final, siguio_ia y nota de cada
    caso. 404 si la sesión no es de su curso."""
    pendiente()

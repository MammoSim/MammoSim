"""Modelos de entrada y salida de la API (validan lo que entra y sale)."""
from datetime import datetime
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field

Decision = Literal["biopsiar", "seguimiento", "ignorar", "estudio_adicional"]
Opcion = Literal["A", "B", "C", "D"]
Rol = Literal["estudiante", "docente"]
EstadoSesion = Literal["en_curso", "completada"]


# ---------- Usuarios ----------
class RegistroIn(BaseModel):
    # Se recortan los espacios ANTES de validar el largo mínimo.
    model_config = ConfigDict(str_strip_whitespace=True)

    nombre_completo: str = Field(min_length=2)
    carrera: str = Field(min_length=1)
    semestre: int | None = Field(default=None, ge=1, le=14)
    acepto_terminos: bool


class UsuarioOut(BaseModel):
    id_usuario: str
    nombre_completo: str
    correo: str
    rol: Rol
    carrera: str
    semestre: int | None = None
    acepto_terminos: bool


class RegistroOut(BaseModel):
    usuario: UsuarioOut
    aviso: str | None = None


# ---------- Cursos ----------
class CursoIn(BaseModel):
    # Se recortan los espacios antes de validar: "   " cuenta como vacío (422).
    model_config = ConfigDict(str_strip_whitespace=True)

    nombre: str = Field(min_length=2)


class CursoOut(BaseModel):
    id_curso: str
    codigo: str
    nombre: str
    id_docente: str


class InscripcionIn(BaseModel):
    codigo: str


# ---------- Casos ----------
class CasoResumen(BaseModel):
    id_caso: str
    titulo: str
    descripcion_breve: str
    lado: str
    vistas: list[str]
    dificultad: str
    modelo_3d_ref: str


# ---------- Sesiones ----------
class SesionIn(BaseModel):
    id_curso: str | None = None  # None = practica solo
    casos: list[str] = Field(min_length=1)  # casos que elige el estudiante


class SesionOut(BaseModel):
    id_sesion: str
    id_usuario: str
    id_curso: str | None = None
    casos_asignados: list[str]
    estado: EstadoSesion
    fecha_inicio: datetime
    fecha_fin: datetime | None = None


class SesionActivaOut(SesionOut):
    casos_terminados: list[str]
    caso_actual: str | None = None


# ---------- Dentro de un caso ----------
class RespuestaIn(BaseModel):
    numero_pregunta: int = Field(ge=1, le=7)
    opcion_elegida: Opcion


class RespuestaOut(BaseModel):
    correcta: bool
    respuesta_correcta: Opcion
    explicacion: str
    mensaje: str  # "Correcto. ..." o "Incorrecto. La respuesta correcta es ..."


class DecisionIn(BaseModel):
    decision: Decision


class GuardadaOut(BaseModel):
    guardada: bool = True


class RecomendacionOut(BaseModel):
    opcion_ia: Decision
    texto: str  # NUNCA incluye si la recomendación es la correcta


class PreguntaResultado(BaseModel):
    numero: int
    correcta: bool


class RetroalimentacionOut(BaseModel):
    hallazgos: str
    retroalimentacion: str
    respuesta_correcta: Decision
    preguntas: list[PreguntaResultado]
    ia_correcta: bool
    siguio_ia: bool
    nota_caso: float = Field(ge=0, le=10)


# ---------- Resultados ----------
class ResultadoOut(BaseModel):
    id_usuario: str
    id_curso: str | None = None
    total_casos: int
    aciertos_iniciales: int
    aciertos_finales: int
    preguntas_acertadas: int
    veces_siguio_ia: int
    siguio_ia_correcta: int
    siguio_ia_incorrecta: int
    puntuacion: float = Field(ge=0, le=10)


# ---------- Vista del docente ----------
class SesionDocenteOut(BaseModel):
    id_sesion: str
    id_usuario: str
    nombre_estudiante: str | None = None
    estado: EstadoSesion
    fecha_inicio: datetime
    puntuacion: float | None = None  # solo cuando la sesión está completada


class RespuestaDetalle(BaseModel):
    numero_pregunta: int
    opcion_elegida: Opcion
    correcta: bool


class DecisionInicialDetalle(BaseModel):
    decision: Decision
    acierto: bool


class DecisionFinalDetalle(BaseModel):
    decision: Decision
    acierto: bool
    opcion_ia: Decision
    ia_correcta: bool
    siguio_ia: bool


class CasoDetalle(BaseModel):
    id_caso: str
    titulo: str | None = None
    respuestas: list[RespuestaDetalle]
    decision_inicial: DecisionInicialDetalle | None = None
    decision_final: DecisionFinalDetalle | None = None
    nota_caso: float | None = None  # solo cuando el caso ya está terminado


class SesionDetalleOut(BaseModel):
    id_sesion: str
    id_usuario: str
    nombre_estudiante: str | None = None
    estado: EstadoSesion
    fecha_inicio: datetime
    fecha_fin: datetime | None = None
    puntuacion: float | None = None
    casos: list[CasoDetalle]

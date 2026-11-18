"""Endpoints 10, 11, 12, 18 y 21: sesiones y resultados.

MODO EXAMEN: una vez que el estudiante empieza una sesión no puede salirse
ni repetirla. Solo puede haber una sesión en_curso por estudiante y no se
puede cerrar a medias.
"""
from datetime import datetime, timezone

from fastapi import APIRouter, Depends, HTTPException
from google.cloud.firestore_v1.base_query import FieldFilter

from app.auth import UsuarioActual, requiere_estudiante
from app.firebase import get_db
from app.schemas import ResultadoOut, SesionActivaOut, SesionIn, SesionOut
from app.services.puntuacion import puntuacion_sesion

router = APIRouter(tags=["sesiones"])


def _sesion_en_curso(db, uid):
    """Devuelve el documento de la sesión en_curso del estudiante, o None."""
    docs = (
        db.collection("sesiones")
        .where(filter=FieldFilter("id_usuario", "==", uid))
        .where(filter=FieldFilter("estado", "==", "en_curso"))
        .limit(1)
        .stream()
    )
    for doc in docs:
        return doc
    return None


@router.post("/sesiones", response_model=SesionOut, status_code=201)
def crear_sesion(
    datos: SesionIn,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """10. Abre una sesión de examen (modo examen: una sola en curso a la vez)."""
    # Validaciones: si algo no cumple, se corta aquí con un código de error.
    if len(set(datos.casos)) != len(datos.casos):
        raise HTTPException(422, "No repitas casos en la sesión")

    if _sesion_en_curso(db, estudiante.uid) is not None:
        raise HTTPException(409, "Ya tienes una sesión en curso")

    # Si la sesión es de un curso, el estudiante debe estar inscrito (activo).
    if datos.id_curso is not None:
        inscripcion = (
            db.collection("inscripciones")
            .document(f"{estudiante.uid}_{datos.id_curso}")
            .get()
        )
        if not inscripcion.exists or inscripcion.to_dict().get("activo") is not True:
            raise HTTPException(403, "No estás inscrito en ese curso")

    for id_caso in datos.casos:
        if not db.collection("casos_clinicos").document(id_caso).get().exists:
            raise HTTPException(404, f"El caso {id_caso} no existe")

    # Se guarda la sesión; el ID lo genera Firestore y se copia dentro del documento.
    ref = db.collection("sesiones").document()
    sesion = {
        "id_sesion": ref.id,
        "id_usuario": estudiante.uid,
        "id_curso": datos.id_curso,
        "casos_asignados": datos.casos,
        "estado": "en_curso",
        "fecha_inicio": datetime.now(timezone.utc),
        "fecha_fin": None,
    }
    ref.set(sesion)
    return sesion


# OJO: estas dos rutas fijas van ANTES de las que llevan {id_sesion}.
@router.get("/sesiones/mias", response_model=list[SesionOut])
def mis_sesiones(
    id_curso: str | None = None,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """11. Historial del estudiante, de la más reciente a la más antigua.
    Incluye las prácticas libres; con ?id_curso= se filtra por un curso."""
    consulta = db.collection("sesiones").where(
        filter=FieldFilter("id_usuario", "==", estudiante.uid)
    )
    if id_curso is not None:
        consulta = consulta.where(filter=FieldFilter("id_curso", "==", id_curso))
    sesiones = [doc.to_dict() for doc in consulta.stream()]
    # Un estudiante tiene pocas sesiones: se ordena aquí y no hace falta índice.
    return sorted(sesiones, key=lambda s: s["fecha_inicio"], reverse=True)


@router.get("/sesiones/activa", response_model=SesionActivaOut)
def sesion_activa(
    estudiante: UsuarioActual = Depends(requiere_estudiante), db=Depends(get_db)
):
    """21. La sesión en_curso, con los casos ya terminados y el caso donde va,
    para continuar la misma si cerró la ventana. 404 si no tiene."""
    doc = _sesion_en_curso(db, estudiante.uid)
    if doc is None:
        raise HTTPException(404, "No tienes una sesión en curso")
    sesion = doc.to_dict()

    # Un caso está terminado cuando ya existe su decisión final.
    terminados = [
        id_caso
        for id_caso in sesion["casos_asignados"]
        if db.collection("decisiones_finales")
        .document(f"{sesion['id_sesion']}_{id_caso}")
        .get()
        .exists
    ]
    pendientes = [c for c in sesion["casos_asignados"] if c not in terminados]
    sesion["casos_terminados"] = terminados
    sesion["caso_actual"] = pendientes[0] if pendientes else None
    return sesion


def _sesion_propia(db, id_sesion: str, uid: str) -> dict:
    """La sesión, o corta con 404 (no existe) o 403 (es de otro estudiante)."""
    doc = db.collection("sesiones").document(id_sesion).get()
    if not doc.exists:
        raise HTTPException(404, "La sesión no existe")
    sesion = doc.to_dict()
    if sesion["id_usuario"] != uid:
        raise HTTPException(403, "Esa sesión no es tuya")
    return sesion


@router.post("/sesiones/{id_sesion}/cerrar", response_model=ResultadoOut)
def cerrar_sesion(
    id_sesion: str,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """12. Cierra la sesión y calcula su resultado. No se puede cerrar a medias."""
    sesion = _sesion_propia(db, id_sesion, estudiante.uid)
    if sesion["estado"] != "en_curso":
        raise HTTPException(409, "La sesión ya estaba completada")

    casos = sesion["casos_asignados"]
    iniciales, finales = [], []
    for id_caso in casos:
        final = db.collection("decisiones_finales").document(f"{id_sesion}_{id_caso}").get()
        inicial = db.collection("decisiones_iniciales").document(f"{id_sesion}_{id_caso}").get()
        if not final.exists or not inicial.exists:
            raise HTTPException(409, "Todavía falta terminar algún caso de la sesión")
        iniciales.append(inicial.to_dict())
        finales.append(final.to_dict())

    respuestas = (
        db.collection("respuestas")
        .where(filter=FieldFilter("id_sesion", "==", id_sesion))
        .stream()
    )
    preguntas_acertadas = sum(1 for r in respuestas if r.to_dict()["correcta"])
    aciertos_iniciales = sum(1 for d in iniciales if d["acierto"])
    aciertos_finales = sum(1 for d in finales if d["acierto"])
    siguio = [d for d in finales if d["siguio_ia"]]

    resultado = {
        "id_usuario": estudiante.uid,
        "id_curso": sesion["id_curso"],
        "total_casos": len(casos),
        "aciertos_iniciales": aciertos_iniciales,
        "aciertos_finales": aciertos_finales,
        "preguntas_acertadas": preguntas_acertadas,
        "veces_siguio_ia": len(siguio),
        "siguio_ia_correcta": sum(1 for d in siguio if d["ia_correcta"]),
        "siguio_ia_incorrecta": sum(1 for d in siguio if not d["ia_correcta"]),
        "puntuacion": puntuacion_sesion(
            preguntas_acertadas, aciertos_iniciales, aciertos_finales, len(casos)
        ),
    }
    # Primero el resultado y luego se cierra: si algo falla en medio, la sesión
    # sigue en_curso y se puede volver a intentar sin dejar nada a medias.
    ahora = datetime.now(timezone.utc)
    db.collection("resultados").document(id_sesion).set({**resultado, "fecha_calculo": ahora})
    db.collection("sesiones").document(id_sesion).set(
        {**sesion, "estado": "completada", "fecha_fin": ahora}
    )
    return resultado


@router.get("/sesiones/{id_sesion}/resultado", response_model=ResultadoOut)
def ver_resultado(
    id_sesion: str,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """18. El resultado de una sesión ya completada."""
    sesion = _sesion_propia(db, id_sesion, estudiante.uid)
    if sesion["estado"] != "completada":
        raise HTTPException(409, "La sesión todavía no está completada")
    doc = db.collection("resultados").document(id_sesion).get()
    if not doc.exists:
        raise HTTPException(404, "Esa sesión no tiene resultado")
    return doc.to_dict()

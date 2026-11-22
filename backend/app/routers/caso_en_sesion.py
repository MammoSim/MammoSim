"""Endpoints 13 a 17: lo que pasa DENTRO de un caso.

Todas las rutas exigen: la sesión es del estudiante, está en_curso y el caso
está en casos_asignados. Orden que el servidor hace cumplir:
  1. 7 respuestas  ->  2. decisión inicial  ->  3. recomendación IA
  ->  4. decisión final  ->  5. retroalimentación.
Los documentos se crean una sola vez (repetir = 409).
"""
from datetime import datetime, timezone

from fastapi import APIRouter, Depends, HTTPException
from google.cloud.firestore_v1.base_query import FieldFilter

from app.auth import UsuarioActual, requiere_estudiante
from app.firebase import get_db
from app.schemas import (
    DecisionIn,
    GuardadaOut,
    RecomendacionOut,
    RespuestaIn,
    RespuestaOut,
    RetroalimentacionOut,
)
from app.services.casos import feedback_pregunta, pregunta_de
from app.services.puntuacion import nota_caso
from app.services.tutor import evaluar_decision_final, recomendar

router = APIRouter(prefix="/sesiones/{id_sesion}/casos/{id_caso}", tags=["dentro de un caso"])


def _contexto(db, id_sesion: str, id_caso: str, uid: str):
    """Comprobaciones comunes. Devuelve (caso, id_documento) o corta con un error."""
    sesion = db.collection("sesiones").document(id_sesion).get()
    if not sesion.exists:
        raise HTTPException(404, "La sesión no existe")
    datos = sesion.to_dict()
    if datos["id_usuario"] != uid:
        raise HTTPException(403, "Esa sesión no es tuya")
    if datos["estado"] != "en_curso":
        raise HTTPException(403, "La sesión ya no está en curso")
    if id_caso not in datos["casos_asignados"]:
        raise HTTPException(422, "Ese caso no está en tu sesión")

    caso = db.collection("casos_clinicos").document(id_caso).get()
    if not caso.exists:
        raise HTTPException(404, "El caso no existe")
    return caso.to_dict(), f"{id_sesion}_{id_caso}"


def _respuestas_del_caso(db, id_sesion: str, id_caso: str) -> list[dict]:
    consulta = (
        db.collection("respuestas")
        .where(filter=FieldFilter("id_sesion", "==", id_sesion))
        .where(filter=FieldFilter("id_caso", "==", id_caso))
    )
    return [d.to_dict() for d in consulta.stream()]


def _decision(db, coleccion: str, id_doc: str):
    """El documento de la decisión (inicial o final) o None si todavía no existe."""
    doc = db.collection(coleccion).document(id_doc).get()
    return doc.to_dict() if doc.exists else None


@router.post("/respuestas", response_model=RespuestaOut, status_code=201)
def responder_pregunta(
    id_sesion: str,
    id_caso: str,
    datos: RespuestaIn,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """13. Guarda la respuesta (una sola vez) y devuelve la explicación de inmediato."""
    caso, _ = _contexto(db, id_sesion, id_caso, estudiante.uid)
    pregunta = pregunta_de(caso, datos.numero_pregunta)

    ref = db.collection("respuestas").document(
        f"{id_sesion}_{id_caso}_p{datos.numero_pregunta}"
    )
    if ref.get().exists:
        raise HTTPException(409, "Esa pregunta ya fue respondida")

    feedback = feedback_pregunta(pregunta, datos.opcion_elegida)
    ref.set(
        {
            "id_sesion": id_sesion,
            "id_usuario": estudiante.uid,  # sale del token
            "id_caso": id_caso,
            "numero_pregunta": datos.numero_pregunta,
            "opcion_elegida": datos.opcion_elegida,
            "correcta": feedback["correcta"],  # lo calcula el servidor
            "fecha": datetime.now(timezone.utc),
        }
    )
    return feedback


@router.post("/decision-inicial", response_model=GuardadaOut, status_code=201)
def decision_inicial(
    id_sesion: str,
    id_caso: str,
    datos: DecisionIn,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """14. Exige las 7 respuestas. Guarda si acertó, pero NO se lo dice."""
    caso, id_doc = _contexto(db, id_sesion, id_caso, estudiante.uid)
    if len(_respuestas_del_caso(db, id_sesion, id_caso)) < 7:
        raise HTTPException(409, "Primero responde las 7 preguntas del caso")

    ref = db.collection("decisiones_iniciales").document(id_doc)
    if ref.get().exists:
        raise HTTPException(409, "Ya tomaste la decisión inicial de este caso")

    ref.set(
        {
            "id_sesion": id_sesion,
            "id_usuario": estudiante.uid,
            "id_caso": id_caso,
            "decision": datos.decision,
            "acierto": datos.decision == caso["respuesta_correcta"],
            "fecha": datetime.now(timezone.utc),
        }
    )
    return {"guardada": True}


@router.get("/recomendacion-ia", response_model=RecomendacionOut)
def recomendacion_ia(
    id_sesion: str,
    id_caso: str,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """15. Solo después de la decisión inicial. No escribe nada y nunca dice
    si la recomendación es la correcta."""
    caso, id_doc = _contexto(db, id_sesion, id_caso, estudiante.uid)
    inicial = _decision(db, "decisiones_iniciales", id_doc)
    if inicial is None:
        raise HTTPException(409, "Primero toma la decisión inicial")
    rec = recomendar(caso, inicial["decision"])
    return {"opcion_ia": rec["opcion_ia"], "texto": rec["texto"]}


@router.post("/decision-final", response_model=GuardadaOut, status_code=201)
def decision_final(
    id_sesion: str,
    id_caso: str,
    datos: DecisionIn,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """16. Exige la decisión inicial. Se guarda siempre, aunque no cambie de opinión."""
    caso, id_doc = _contexto(db, id_sesion, id_caso, estudiante.uid)
    inicial = _decision(db, "decisiones_iniciales", id_doc)
    if inicial is None:
        raise HTTPException(409, "Primero toma la decisión inicial")

    ref = db.collection("decisiones_finales").document(id_doc)
    if ref.get().exists:
        raise HTTPException(409, "Ya tomaste la decisión final de este caso")

    ref.set(
        {
            "id_sesion": id_sesion,
            "id_usuario": estudiante.uid,
            "id_caso": id_caso,
            **evaluar_decision_final(caso, inicial["decision"], datos.decision),
            "fecha": datetime.now(timezone.utc),
        }
    )
    return {"guardada": True}


@router.get("/retroalimentacion", response_model=RetroalimentacionOut)
def retroalimentacion(
    id_sesion: str,
    id_caso: str,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """17. Solo después de la decisión final. La nota se calcula al vuelo."""
    caso, id_doc = _contexto(db, id_sesion, id_caso, estudiante.uid)
    final = _decision(db, "decisiones_finales", id_doc)
    if final is None:
        raise HTTPException(409, "Primero toma la decisión final")
    inicial = _decision(db, "decisiones_iniciales", id_doc)

    respuestas = sorted(
        _respuestas_del_caso(db, id_sesion, id_caso), key=lambda r: r["numero_pregunta"]
    )
    acertadas = sum(1 for r in respuestas if r["correcta"])
    return {
        "hallazgos": caso["hallazgos"],
        "retroalimentacion": caso["retroalimentacion"],
        "respuesta_correcta": caso["respuesta_correcta"],
        "preguntas": [
            {"numero": r["numero_pregunta"], "correcta": r["correcta"]} for r in respuestas
        ],
        "ia_correcta": final["ia_correcta"],
        "siguio_ia": final["siguio_ia"],
        "nota_caso": nota_caso(acertadas, inicial["acierto"], final["acierto"]),
    }

@router.get("/progreso")
def progreso_caso(
    id_sesion: str,
    id_caso: str,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """Consulta el progreso sin revelar respuestas correctas."""

    _, id_doc = _contexto(
        db, id_sesion, id_caso, estudiante.uid
    )

    respuestas = _respuestas_del_caso(
        db, id_sesion, id_caso
    )

    numeros = sorted({
        r["numero_pregunta"] for r in respuestas
    })

    inicial = _decision(
        db, "decisiones_iniciales", id_doc
    )

    final = _decision(
        db, "decisiones_finales", id_doc
    )

    pendientes = [
        n for n in range(1, 8)
        if n not in numeros
    ]

    
    return {
        "id_caso": id_caso,
        "preguntas_respondidas": numeros,
        "siguiente_pregunta": (
            pendientes[0] if pendientes else None
        ),
        "decision_inicial_guardada": inicial is not None,
        "decision_final_guardada": final is not None,
        "decision_inicial": (
            inicial["decision"] if inicial else None
        ),
        "decision_final": (
            final["decision"] if final else None
        ),
    }


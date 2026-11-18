"""Endpoints 3 a 7 (cursos e inscripciones) y 19 y 20 (vista del docente)."""
from datetime import datetime, timezone

from fastapi import APIRouter, Depends, HTTPException
from google.cloud.firestore_v1.base_query import FieldFilter

from app.auth import UsuarioActual, requiere_docente, requiere_estudiante
from app.firebase import get_db
from app.schemas import (
    CursoIn,
    CursoOut,
    InscripcionIn,
    SesionDetalleOut,
    SesionDocenteOut,
)
from app.services.cursos import generar_codigo
from app.services.puntuacion import nota_caso

router = APIRouter(tags=["cursos"])


def _curso_del_docente(db, uid):
    """El único curso del docente (o None). Un docente tiene un solo curso."""
    consulta = db.collection("cursos").where(filter=FieldFilter("id_docente", "==", uid))
    for doc in consulta.limit(1).stream():
        return doc
    return None


def _a_curso_out(doc) -> dict:
    """El ID del curso es el del documento, no un campo guardado."""
    datos = doc.to_dict()
    return {
        "id_curso": doc.id,
        "codigo": datos["codigo"],
        "nombre": datos["nombre"],
        "id_docente": datos["id_docente"],
    }


@router.post("/cursos", response_model=CursoOut, status_code=201)
def crear_curso(
    datos: CursoIn, docente: UsuarioActual = Depends(requiere_docente), db=Depends(get_db)
):
    """3. Crea el curso del docente con un código único generado por el servidor."""
    if _curso_del_docente(db, docente.uid) is not None:
        raise HTTPException(409, "Ya tienes un curso creado")

    # El código lo genera el servidor; se repite si ya lo tiene otro curso.
    for _ in range(10):
        codigo = generar_codigo()
        repetido = db.collection("cursos").where(filter=FieldFilter("codigo", "==", codigo))
        if not list(repetido.limit(1).stream()):
            break
    else:
        raise HTTPException(500, "No se pudo generar un código único, intenta de nuevo")

    ref = db.collection("cursos").document()  # ID automático
    ref.set(
        {
            "codigo": codigo,
            "nombre": datos.nombre.strip(),
            "id_docente": docente.uid,
            "fecha_creacion": datetime.now(timezone.utc),
        }
    )
    return _a_curso_out(ref.get())


@router.get("/cursos/mio", response_model=CursoOut)
def mi_curso(docente: UsuarioActual = Depends(requiere_docente), db=Depends(get_db)):
    """4. El curso del docente que hace la petición."""
    doc = _curso_del_docente(db, docente.uid)
    if doc is None:
        raise HTTPException(404, "Todavía no tienes un curso")
    return _a_curso_out(doc)


def _inscripcion(db, uid, id_curso):
    """Referencia a inscripciones/{uid}_{id_curso}: un documento por estudiante y curso."""
    return db.collection("inscripciones").document(f"{uid}_{id_curso}")


@router.post("/inscripciones", response_model=CursoOut)
def unirse_a_curso(
    datos: InscripcionIn,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """5. Se une a un curso con su código; si ya había estado, la reactiva."""
    codigo = datos.codigo.strip().upper()
    consulta = db.collection("cursos").where(filter=FieldFilter("codigo", "==", codigo))
    encontrados = list(consulta.limit(1).stream())
    if not encontrados:
        raise HTTPException(404, "No existe un curso con ese código")
    curso = encontrados[0]

    ref = _inscripcion(db, estudiante.uid, curso.id)
    anterior = ref.get()
    if anterior.exists:
        # Ya había estado: se reactiva y se conserva la fecha original.
        inscripcion = anterior.to_dict()
        inscripcion["activo"] = True
    else:
        inscripcion = {
            "id_usuario": estudiante.uid,  # sale del token, no del cuerpo
            "id_curso": curso.id,
            "activo": True,
            "fecha_union": datetime.now(timezone.utc),
        }
    ref.set(inscripcion)
    return _a_curso_out(curso)


@router.delete("/inscripciones/{id_curso}", status_code=204)
def salirse_del_curso(
    id_curso: str,
    estudiante: UsuarioActual = Depends(requiere_estudiante),
    db=Depends(get_db),
):
    """6. Se sale del curso: activo pasa a False y el historial se conserva."""
    ref = _inscripcion(db, estudiante.uid, id_curso)
    actual = ref.get()
    if not actual.exists or actual.to_dict().get("activo") is not True:
        raise HTTPException(404, "No estás inscrito en ese curso")
    inscripcion = actual.to_dict()
    inscripcion["activo"] = False
    ref.set(inscripcion)


@router.get("/inscripciones/mias", response_model=list[CursoOut])
def mis_cursos(
    estudiante: UsuarioActual = Depends(requiere_estudiante), db=Depends(get_db)
):
    """7. Los cursos donde el estudiante tiene la inscripción activa."""
    consulta = (
        db.collection("inscripciones")
        .where(filter=FieldFilter("id_usuario", "==", estudiante.uid))
        .where(filter=FieldFilter("activo", "==", True))
    )
    cursos = []
    for inscripcion in consulta.stream():
        curso = db.collection("cursos").document(inscripcion.to_dict()["id_curso"]).get()
        if curso.exists:
            cursos.append(_a_curso_out(curso))
    return sorted(cursos, key=lambda c: c["nombre"])


def _curso_o_404(db, docente):
    curso = _curso_del_docente(db, docente.uid)
    if curso is None:
        raise HTTPException(404, "Todavía no tienes un curso")
    return curso


def _nombre_estudiante(db, id_usuario: str):
    doc = db.collection("usuarios").document(id_usuario).get()
    return doc.to_dict().get("nombre_completo") if doc.exists else None


@router.get("/cursos/mio/sesiones", response_model=list[SesionDocenteOut])
def sesiones_de_mi_curso(
    id_usuario: str | None = None,
    docente: UsuarioActual = Depends(requiere_docente),
    db=Depends(get_db),
):
    """19. Las sesiones de su curso (nunca prácticas libres ni de otros cursos),
    de la más reciente a la más antigua. Con ?id_usuario= se filtra por estudiante."""
    curso = _curso_o_404(db, docente)
    consulta = db.collection("sesiones").where(filter=FieldFilter("id_curso", "==", curso.id))
    if id_usuario is not None:
        consulta = consulta.where(filter=FieldFilter("id_usuario", "==", id_usuario))

    nombres = {}  # cada estudiante se busca una sola vez
    salida = []
    for doc in consulta.stream():
        sesion = doc.to_dict()
        uid = sesion["id_usuario"]
        if uid not in nombres:
            nombres[uid] = _nombre_estudiante(db, uid)
        puntuacion = None
        if sesion["estado"] == "completada":
            resultado = db.collection("resultados").document(sesion["id_sesion"]).get()
            puntuacion = resultado.to_dict()["puntuacion"] if resultado.exists else None
        salida.append(
            {
                "id_sesion": sesion["id_sesion"],
                "id_usuario": uid,
                "nombre_estudiante": nombres[uid],
                "estado": sesion["estado"],
                "fecha_inicio": sesion["fecha_inicio"],
                "puntuacion": puntuacion,
            }
        )
    # Se ordena aquí para no depender de un índice compuesto.
    return sorted(salida, key=lambda s: s["fecha_inicio"], reverse=True)


@router.get("/cursos/mio/sesiones/{id_sesion}", response_model=SesionDetalleOut)
def detalle_de_sesion(
    id_sesion: str,
    docente: UsuarioActual = Depends(requiere_docente),
    db=Depends(get_db),
):
    """20. Respuestas, decisiones inicial y final, siguio_ia y nota de cada caso.
    404 si la sesión no es de su curso (así no se sabe si existe en otro)."""
    curso = _curso_o_404(db, docente)
    doc = db.collection("sesiones").document(id_sesion).get()
    if not doc.exists or doc.to_dict()["id_curso"] != curso.id:
        raise HTTPException(404, "Esa sesión no es de tu curso")
    sesion = doc.to_dict()

    respuestas = [
        d.to_dict()
        for d in db.collection("respuestas")
        .where(filter=FieldFilter("id_sesion", "==", id_sesion))
        .stream()
    ]
    casos = []
    for id_caso in sesion["casos_asignados"]:
        id_doc = f"{id_sesion}_{id_caso}"
        propias = sorted(
            (r for r in respuestas if r["id_caso"] == id_caso),
            key=lambda r: r["numero_pregunta"],
        )
        inicial = db.collection("decisiones_iniciales").document(id_doc).get()
        final = db.collection("decisiones_finales").document(id_doc).get()
        caso = db.collection("casos_clinicos").document(id_caso).get()

        nota = None
        if inicial.exists and final.exists:
            acertadas = sum(1 for r in propias if r["correcta"])
            nota = nota_caso(
                acertadas, inicial.to_dict()["acierto"], final.to_dict()["acierto"]
            )
        casos.append(
            {
                "id_caso": id_caso,
                "titulo": caso.to_dict().get("titulo") if caso.exists else None,
                "respuestas": propias,
                "decision_inicial": inicial.to_dict() if inicial.exists else None,
                "decision_final": final.to_dict() if final.exists else None,
                "nota_caso": nota,
            }
        )

    puntuacion = None
    if sesion["estado"] == "completada":
        resultado = db.collection("resultados").document(id_sesion).get()
        puntuacion = resultado.to_dict()["puntuacion"] if resultado.exists else None

    return {
        "id_sesion": id_sesion,
        "id_usuario": sesion["id_usuario"],
        "nombre_estudiante": _nombre_estudiante(db, sesion["id_usuario"]),
        "estado": sesion["estado"],
        "fecha_inicio": sesion["fecha_inicio"],
        "fecha_fin": sesion.get("fecha_fin"),
        "puntuacion": puntuacion,
        "casos": casos,
    }

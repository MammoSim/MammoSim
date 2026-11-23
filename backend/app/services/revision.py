"""Detalle de un caso ya terminado, para que el estudiante lo repase.

Solo se usa con sesiones COMPLETADAS (el router lo exige): por eso aquí sí
salen las respuestas correctas y las explicaciones. Son funciones puras.
"""
from app.services.puntuacion import nota_caso
from app.services.tutor import recomendar


def detalle_caso(caso: dict, respuestas: list[dict], inicial: dict | None, final: dict | None) -> dict:
    elegidas = {r["numero_pregunta"]: r for r in respuestas}
    preguntas = []
    for p in caso["preguntas"]:
        r = elegidas.get(p["numero"])
        preguntas.append(
            {
                "numero": p["numero"],
                "enunciado": p["enunciado"],
                "opciones": [{"id": k, "texto": v} for k, v in p["opciones"].items()],
                "opcion_elegida": r["opcion_elegida"] if r else None,
                "respuesta_correcta": p["respuesta_correcta"],
                "correcta": r["correcta"] if r else None,
                "explicacion": p["explicacion"],
            }
        )

    recomendacion = None
    if inicial is not None:
        rec = recomendar(caso, inicial["decision"])
        recomendacion = {
            "opcion_ia": rec["opcion_ia"],
            "texto": rec["texto"],
            "ia_correcta": rec["ia_correcta"],
            "siguio_ia": final["siguio_ia"] if final is not None else None,
        }

    nota = None
    if inicial is not None and final is not None:
        acertadas = sum(1 for r in respuestas if r["correcta"])
        nota = nota_caso(acertadas, inicial["acierto"], final["acierto"])

    return {
        "id_caso": caso["id_caso"],
        "titulo": caso.get("titulo"),
        "nota_caso": nota,
        "preguntas": preguntas,
        "enunciado_decision": caso["enunciado_decision"],
        "opciones_decision": [
            {"id": o["valor"], "texto": o["texto"]} for o in caso["opciones_decision"]
        ],
        "respuesta_correcta": caso["respuesta_correcta"],
        "decision_inicial": (
            {"decision": inicial["decision"], "acierto": inicial["acierto"]} if inicial else None
        ),
        "recomendacion_ia": recomendacion,
        "decision_final": (
            {"decision": final["decision"], "acierto": final["acierto"]} if final else None
        ),
        "hallazgos": caso["hallazgos"],
        "retroalimentacion": caso["retroalimentacion"],
    }

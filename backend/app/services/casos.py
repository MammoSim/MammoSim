"""Qué se le muestra al cliente de un caso y cuándo.

Se usa una LISTA BLANCA: solo sale lo que está escrito aquí. Si mañana se
agrega un campo nuevo al caso en Firestore, no se filtra por accidente.
"""

CAMPOS_RESUMEN = (
    "id_caso", "titulo", "descripcion_breve", "lado",
    "vistas", "dificultad", "modelo_3d_ref",
)
CAMPOS_CASO = CAMPOS_RESUMEN + (
    "edad_simulada", "datos_estudio", "enunciado_decision", "opciones_decision", "fuente",
)


def resumen_caso(caso: dict) -> dict:
    return {k: caso[k] for k in CAMPOS_RESUMEN if k in caso}



def caso_para_resolver(caso: dict) -> dict:
    """Prepara el caso para Unity sin revelar las respuestas.

    Convierte las 7 preguntas técnicas y agrega la
    decisión diagnóstica como pregunta número 8.
    """

    # Datos generales permitidos.
    salida = {
        k: caso[k]
        for k in CAMPOS_CASO
        if k in caso
    }

    preguntas_unity = []

    # 1. Siete preguntas técnicas.
    for p in caso["preguntas"]:
        opciones = [
            {
                "id": letra,
                "texto": texto
            }
            for letra, texto in p["opciones"].items()
        ]

        preguntas_unity.append({
            "numero": p["numero"],
            "tipo": "tecnica",
            "enunciado": p["enunciado"],
            "opciones": opciones
        })

    # 2. Pregunta diagnóstica número 8.
    opciones_decision = [
        {
            "id": opcion["valor"],
            "texto": opcion["texto"]
        }
        for opcion in caso["opciones_decision"]
    ]

    preguntas_unity.append({
        "numero": 8,
        "tipo": "diagnostica",
        "enunciado": caso["enunciado_decision"],
        "opciones": opciones_decision
    })

    # 3. Entregar las ocho etapas a Unity.
    salida["preguntas"] = preguntas_unity

    return salida


def pregunta_de(caso: dict, numero: int) -> dict:
    for p in caso["preguntas"]:
        if p["numero"] == numero:
            return p
    raise KeyError(f"El caso no tiene la pregunta {numero}")


def feedback_pregunta(pregunta: dict, opcion_elegida: str) -> dict:
    """Se muestra de inmediato al contestar cada pregunta técnica."""
    correcta_letra = pregunta["respuesta_correcta"]
    correcta = opcion_elegida == correcta_letra
    explicacion = pregunta["explicacion"]
    if correcta:
        mensaje = f"Correcto. {explicacion}"
    else:
        texto = pregunta["opciones"][correcta_letra]
        mensaje = (
            f"Incorrecto. La respuesta correcta es {correcta_letra} ({texto}). "
            f"{explicacion}"
        )
    return {
        "correcta": correcta,
        "respuesta_correcta": correcta_letra,
        "explicacion": explicacion,
        "mensaje": mensaje,
    }

# Pruebas y calidad

Esta sección documenta cómo se verificará el funcionamiento de MammoSim y dónde se registrarán los casos de prueba, los resultados y las evidencias.

## Tipos de pruebas

| Tipo | Qué se verificará |
| --- | --- |
| **Funcionales** | Que el usuario pueda seleccionar un caso, visualizar la imagen médica, registrar su decisión y consultar la retroalimentación y el historial. |
| **De integración** | Que el simulador intercambie información con el agente tutor mediante la API y que las decisiones y resultados queden registrados y puedan consultarse. |
| **De usabilidad** | Que el participante pueda completar el flujo de simulación sin asistencia técnica y comprender la retroalimentación recibida. |

## Casos de prueba

Cada caso incluirá un identificador, el requisito asociado, las precondiciones, los pasos y el resultado esperado.

## Resultados y evidencias

Por cada ejecución se registrarán la fecha, la versión probada, el resultado observado y el estado: **Pendiente**, **Aprobada**, **Fallida** o **Bloqueada**. Cuando haya evidencia o un issue asociado, se añadirá su enlace.

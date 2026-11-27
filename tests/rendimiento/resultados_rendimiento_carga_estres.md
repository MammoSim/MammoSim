# Resultados de pruebas de rendimiento, carga y estrés - MammoSim API

Fecha: 26-11-2026. Herramienta: Locust 2.47.0. Objetivo: API FastAPI (rama develop, commit d665148) en un solo proceso uvicorn, local.
Base de datos simulada (FakeDb), sin Firebase ni red. Mide el código de la API, no Firestore ni Render.
Usuario virtual: estudiante con sesión en curso; espera de 0,5 a 1,5 s entre acciones.
Mezcla: GET /casos, GET /casos/{id}, GET /usuarios/me, GET /sesiones/mias, GET /salud.

| Prueba | Usuarios | Duración | Peticiones | Fallos | Media (ms) | p95 (ms) | p99 (ms) | Máx (ms) | Peticiones/s |
|---|---|---|---|---|---|---|---|---|---|
| Rendimiento | 10 | 40 s | 400 | 0 | 3,8 | 6 | 13 | 23 | 10 |
| Carga | 50 | 60 s | 2.885 | 0 | 3,6 | 6 | 17 | 27 | 49 |
| Estrés | 400 | 60 s | 22.198 | 0 | 10,6 | 28 | 100 | 190 | 376 |

Conclusión: sin errores en ningún escenario, ni siquiera con 400 usuarios simultáneos; no se alcanzó el punto de quiebre.
Limitación: falta medir contra Firebase y Render reales, y el flujo de escritura (respuestas, cierre de sesión).

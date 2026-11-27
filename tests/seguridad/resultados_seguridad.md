# Resultados de pruebas de seguridad - MammoSim API

Fecha: 26/27-11-2026. Herramienta: pytest (FastAPI TestClient) con base simulada (FakeDb). Rama develop (commit d665148) + tests/seguridad.
Comando: python -m pytest tests/seguridad -v

Resultado: 144 pasan, 1 xfail (145 pruebas). Con la suite completa (python -m pytest backend/tests tests) dan 471 pasan y 2 xfail (327 + 144 pasan; 1 + 1 xfail).

| Qué se probó | Pruebas | Resultado |
|---|---|---|
| Hay al menos 22 rutas protegidas (el esquema tiene 23) | 1 | Aprobada |
| Las 23 rutas protegidas responden 401 sin token | 23 | Aprobadas |
| Las 23 rutas responden 401 con token manipulado y no devuelven el token | 23 | Aprobadas |
| IDs maliciosos en la URL (../, comillas, script, %00, 3000 caracteres) en 5 rutas: nunca 500 ni 200 | 35 | Aprobadas |
| Cuerpos raros (nulos, tipos erróneos, 100.000 caracteres, SQL, script, números enormes) en 4 rutas POST y 2 roles: nunca 500 | 48 | Aprobadas |
| JSON mal formado: 422 | 4 | Aprobadas |
| Métodos no permitidos: 405 | 5 | Aprobadas |
| Un 404 no filtra Traceback ni rutas internas | 1 | Aprobada |
| CORS: rechaza origen desconocido y permite el del frontend | 2 | Aprobadas |
| El perfil no expone datos de otros usuarios | 1 | Aprobada |
| Documentación automática (informativo) | 1 | Aprobada |
| Encabezado X-Content-Type-Options: nosniff | 1 | XFAIL (defecto de bajo riesgo) |

Hallazgos:
1. Bajo: la API no envía X-Content-Type-Options: nosniff (se puede agregar con un middleware).
2. Informativo: /docs y /openapi.json son públicos; conviene desactivarlos en producción.
3. No hay límite de peticiones por usuario (rate limiting); no se probó porque no existe.

Limitaciones: no es un escaneo OWASP ZAP; no verifica tokens reales de Firebase, HTTPS ni encabezados de Render; el acceso por rol ya estaba cubierto en backend/tests.

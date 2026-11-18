# MammoSim — Backend (FastAPI): guía para el equipo

Resumen de lo que se ha hecho en la base de datos y el backend, cómo trabajar sobre él y en qué se puede ayudar. Autora del backend: Marlin Pérez Huertas.

## 1. Qué hay hecho

| Pieza | Estado | Dónde |
| --- | --- | --- |
| Base de datos Firestore (esquema, 7 casos clínicos cargados, lista de docentes) | Hecho, mergeado | PR #15, `docs/ESQUEMA_BASE_DE_DATOS.md`, `scripts/` |
| Esqueleto de FastAPI con los 21 endpoints declarados, autenticación por token, roles, lógica del Agente Tutor y fórmula de puntuación | Hecho, mergeado | PR #17, carpeta `backend/` |
| Los 21 endpoints (registro, perfil, cursos, inscripciones, casos, sesiones, respuestas, decisiones, retroalimentación, resultado y vista del docente) | Hecho: 21 de 21, con 157 pruebas | Rama `backend` del repositorio `MammoSim/MammoSim` |
| Prueba con Firestore real y con la web | Pendiente (las pruebas actuales usan una base falsa) | Marlin y Zaira |

## 2. Cómo se conecta todo

Unity → React → FastAPI → Firestore

- **Unity** (Luigi) muestra el caso en 3D y avisa a React lo que el usuario elige. No calcula notas ni guarda historial.
- **React** inicia sesión con Firebase, llama a la API con el token del usuario y le devuelve a Unity lo que responda el servidor.
- **FastAPI** (este backend) verifica el token, aplica las reglas del modo examen, corrige y calcula la nota. Es lo único que lee y escribe en Firestore; las reglas de Firestore bloquean a cualquier cliente.
- Unity y React hablan con mensajes dentro del navegador; React y FastAPI, con peticiones HTTP a los endpoints.

## 3. Cómo correrlo en tu computador

1. Clonar el repositorio `MammoSim/MammoSim` y cambiar a la rama del área: `git switch backend` y `git pull origin backend`.
2. En la carpeta `backend`: `python -m venv .venv`, activar el entorno (`.venv\Scripts\Activate.ps1` en Windows) e instalar con `pip install -r requirements.txt`.
3. Pruebas: `python -m pytest`. Deben pasar todas (hoy son 157).
4. Levantar la API: `uvicorn app.main:app --reload` y abrir http://localhost:8000/docs.
5. No hace falta la llave de Firebase para las pruebas: usan una base de datos falsa (`FakeDb`). La llave real **nunca** se sube al repositorio.

## 4. Flujo de trabajo con Git (acordado por el equipo)

1. Crear un **Issue** que describa el trabajo.
2. Trabajar en la rama de su área (`backend`, `frontend`, `simulacion`, `documentacion`...) del repositorio `MammoSim/MammoSim`; ya no se crean más ramas.
3. Commits con el formato `tipo(alcance): descripción`, por ejemplo `feat(backend): agrega endpoint de registro de usuarios`.
4. Subir los commits (`git push origin <rama del área>`). Zaira revisa el trabajo de cada rama.

## 5. Cómo se programa un endpoint

Todo endpoint tiene las mismas 5 partes. El ejemplo más claro es `POST /auth/registro` en `backend/app/routers/usuarios.py`.

1. **Decorador:** el método y la dirección, por ejemplo `@router.post("/auth/registro")`.
2. **Parámetros:** lo que recibe: los datos del cliente, el usuario autenticado y la base (`db`).
3. **Validaciones:** si algo no cumple, `raise HTTPException(código, "mensaje")` (401 sin token, 403 sin permiso, 404 no existe, 409 ya existe, 422 datos inválidos).
4. **Trabajo con la base:** leer con `.get()` o `.stream()`, guardar con `.set()`.
5. **Respuesta:** lo que devuelve con `return`.

Cada endpoint nuevo lleva sus pruebas en `backend/tests/`.

| Archivo en `backend/app/routers/` | Endpoints |
| --- | --- |
| `usuarios.py` | registro, perfil |
| `casos.py` | lista y detalle de casos |
| `cursos.py` | cursos, inscripciones, vista del docente |
| `sesiones.py` | abrir, retomar, cerrar y ver resultado de una sesión |
| `caso_en_sesion.py` | respuestas, decisiones, recomendación de la IA y retroalimentación |

La lógica del Agente Tutor está en `backend/app/services/tutor.py` y la fórmula de puntuación en `services/puntuacion.py`.

## 6. Reglas que no se pueden romper

- El uid y el correo salen **siempre del token** verificado, nunca del cuerpo de la petición.
- El **rol lo decide el servidor**: docente solo si el correo está en `docentes_autorizados` y activo.
- Ningún caso puede entregar `respuesta_correcta`, explicaciones, recomendación de la IA ni retroalimentación **antes de tiempo**. Se usa una lista blanca de campos (`services/casos.py`).
- Modo examen: una sola sesión `en_curso` por estudiante; no se puede salir, repetir ni cerrar incompleta.
- Puntuación de cada caso: 7 preguntas técnicas más la decisión inicial y final (0,5 cada una), sobre 10 puntos.
- Nunca subir llaves (`*firebase-adminsdk*.json`) ni `.env`.

## 7. Los 21 endpoints y su estado

| # | Método y ruta | Estado |
| --- | --- | --- |
| 1 | `POST /auth/registro` | Hecho |
| 2 | `GET /usuarios/me` | Hecho |
| 3 | `POST /cursos` | Hecho |
| 4 | `GET /cursos/mio` | Hecho |
| 5 | `POST /inscripciones` | Hecho |
| 6 | `DELETE /inscripciones/{id_curso}` | Hecho |
| 7 | `GET /inscripciones/mias` | Hecho |
| 8 | `GET /casos` | Hecho |
| 9 | `GET /casos/{id_caso}` | Hecho |
| 10 | `POST /sesiones` | Hecho |
| 11 | `GET /sesiones/mias` | Hecho |
| 12 | `GET /sesiones/activa` | Hecho |
| 13 | `POST /sesiones/{id_sesion}/cerrar` | Hecho |
| 14 | `GET /sesiones/{id_sesion}/resultado` | Hecho |
| 15 | `POST /sesiones/{id_sesion}/casos/{id_caso}/respuestas` | Hecho |
| 16 | `POST /sesiones/{id_sesion}/casos/{id_caso}/decision-inicial` | Hecho |
| 17 | `GET /sesiones/{id_sesion}/casos/{id_caso}/recomendacion-ia` | Hecho |
| 18 | `POST /sesiones/{id_sesion}/casos/{id_caso}/decision-final` | Hecho |
| 19 | `GET /sesiones/{id_sesion}/casos/{id_caso}/retroalimentacion` | Hecho |
| 20 | `GET /cursos/mio/sesiones` | Hecho |
| 21 | `GET /cursos/mio/sesiones/{id_sesion}` | Hecho |

Los 21 están programados y probados en la rama `backend`.

## 8. En qué puede ayudar Alejandro

Por su rol de análisis y calidad, lo más útil es:

1. **Pruebas.** Revisar las pruebas de seguridad de los endpoints nuevos (inscripciones, ver caso, respuestas y decisiones, cierre de sesión y vista del docente) y agregar casos borde: errores 401/403/404/409/422 y que no se filtren respuestas correctas.
2. **Revisión de PRs.** Leer cada PR del backend contra el documento de endpoints y las reglas de la sección 6, antes de que lo revise Zaira.
3. **Consistencia de casos.** Comparar el JSON de casos de Unity (`casos_mammosim.json`) con `scripts/casos_clinicos.json`, que es el que está en Firestore. Deben coincidir en preguntas, opciones y respuestas.
4. **Prueba de punta a punta.** Con Zaira y Marlin, probar el flujo completo con Firestore real y la web conectada.
5. **Documentación.** Mantener al día esta guía y el documento de endpoints cuando algo cambie.

## 9. Prompt sugerido para trabajar con Claude

> Estoy en el proyecto MammoSim (simulador de mamografías con Unity, React, FastAPI y Firestore). Voy a ayudar con el backend en `backend/` del repositorio `MammoSim/MammoSim`, rama `backend`. Lee primero `docs/ESQUEMA_BASE_DE_DATOS.md`, `backend/GUIA_BACKEND.md` y los routers en `backend/app/routers/`. El endpoint `POST /auth/registro` en `usuarios.py` es el modelo de cómo se escribe un endpoint. Reglas: uid y correo salen del token, el rol lo decide el servidor, ningún caso revela respuestas antes de tiempo, las pruebas usan `FakeDb` sin conexión a Firebase, y los cambios se suben a la rama del área. Quiero \[escribir pruebas para el endpoint X / revisar el PR #N / programar el endpoint X\]. Explícame primero el enfoque y luego propón el código.

## 10. Documentos de referencia

- `docs/ESQUEMA_BASE_DE_DATOS.md` (en el repositorio): colecciones, campos y fórmula de puntuación.
- `backend/GUIA_BACKEND.md` (en el repositorio): cómo correr y cómo programar un endpoint.
- `docs/ENDPOINTS.md` (en el repositorio): cuerpo y respuestas de cada endpoint.

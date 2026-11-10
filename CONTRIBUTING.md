# Flujo de trabajo

MammoSim trabaja con un conjunto fijo de ramas. El equipo realiza cada cambio en la rama del componente correspondiente y lo integra mediante un pull request (PR). Las versiones aprobadas pasan de `develop` a `main` mediante un PR para despliegue.

## Ramas

| Rama | Función |
| --- | --- |
| `main` | Rama protegida que recibe las versiones aprobadas para despliegue. |
| `develop` | Rama de integración donde se prueban juntos los cambios de los componentes. |
| `frontend` | Interfaz web desarrollada en React. |
| `backend` | API, base de datos, agente tutor y gestión de casos clínicos. |
| `simulacion` | Simulador en Unity y visualización de los modelos 3D. |
| `documentacion` | Requisitos, guías y documentación técnica. |
| `test` | Pruebas funcionales, de integración y de regresión. |

## Pull requests

1. Sube el cambio a la rama correspondiente y abre un PR hacia `develop`.
2. Revisa **Files changed** antes de solicitar revisión. El PR incluye todos los cambios de la rama de origen que aún están pendientes de integrar en `develop`.
3. Integra el PR después de la revisión y valida los cambios en `develop`.
4. Cuando la versión esté lista para despliegue, abre un PR de `develop` a `main` y cumple las reglas de la rama protegida.

En los PR de trabajo, usa el prefijo que corresponda al cambio principal.

| Prefijo | Ejemplo de título |
| --- | --- |
| `feature/` | `feature/ Mostrar el modelo 3D del caso` |
| `fix/` | `fix/ Corregir la recomendación del tutor` |
| `docs/` | `docs/ Actualizar los requisitos del simulador` |
| `test/` | `test/ Verificar el registro de la decisión inicial` |
| `experiment/` | `experiment/ Evaluar una variante del agente tutor` |

Antes de fusionar un PR, verifica que:

- El PR esté vinculado a una tarea o issue.
- Las pruebas aplicables se hayan ejecutado y sus resultados estén registrados.
- La documentación afectada esté actualizada.
- El PR tenga la aprobación requerida y las observaciones estén resueltas.

## Commits

Usa `tipo(alcance): descripción breve`. El alcance es opcional. Si lo omites, usa `tipo: descripción breve`.

Ejemplos:

- `feat(simulacion): mostrar el modelo 3D del caso`
- `fix(backend): mostrar la recomendación después de guardar la decisión inicial`
- `docs: actualizar el flujo de trabajo del repositorio`

Puedes añadir un cuerpo para explicar el motivo del cambio y un pie para referenciar tareas, issues o cambios incompatibles. Separa cada bloque con una línea en blanco.

### Tipos de commits

| Tipo | Uso |
| --- | --- |
| `feat` | Añadir una funcionalidad. |
| `fix` | Corregir un error. |
| `docs` | Modificar únicamente la documentación. |
| `style` | Ajustar el formato del código sin cambiar su comportamiento. |
| `refactor` | Reestructurar el código sin añadir funcionalidades ni corregir errores. |
| `perf` | Mejorar el rendimiento. |
| `test` | Añadir o corregir pruebas. |
| `chore` | Realizar tareas de mantenimiento, dependencias o configuración. |

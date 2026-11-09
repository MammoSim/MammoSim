# Requisitos de software de MammoSim

## Índice

- [Requisitos funcionales](#requisitos-funcionales)
  - [Registro y gestión de usuarios](#registro-y-gestión-de-usuarios)
  - [Selección y visualización de casos](#selección-y-visualización-de-casos)
  - [Toma de decisiones e interacción con el agente tutor](#toma-de-decisiones-e-interacción-con-el-agente-tutor)
  - [Evaluación e historial](#evaluación-e-historial)
  - [Resumen de desempeño](#resumen-de-desempeño)
- [Requisitos no funcionales](#requisitos-no-funcionales)
  - [Usabilidad](#usabilidad)
  - [Desempeño](#desempeño)
  - [Requisitos lógicos de la base de datos](#requisitos-lógicos-de-la-base-de-datos)
  - [Restricciones de diseño](#restricciones-de-diseño)
  - [Cumplimiento de estándares y normativas](#cumplimiento-de-estándares-y-normativas)
  - [Atributos del sistema](#atributos-del-sistema)

## Requisitos funcionales

### Registro y gestión de usuarios

#### RF-001.1 — Registro de usuarios

**Prioridad:** MUST · **Complejidad:** MODERADA

El sistema deberá proporcionar un mecanismo de registro que capture y valide los datos obligatorios del usuario antes de permitirle acceder al simulador.

**Criterio de aceptación.** El usuario puede ingresar los datos solicitados y el sistema valida los campos obligatorios antes de guardar el registro.

### Selección y visualización de casos

#### RF-002.1 — Catálogo de casos clínicos

**Prioridad:** MUST · **Complejidad:** BAJA

El sistema deberá mostrar un catálogo de casos clínicos disponibles para que el usuario pueda seleccionar uno.

**Criterio de aceptación.** La interfaz muestra el catálogo y permite al usuario seleccionar el caso que desee.

#### RF-002.2 — Contexto del caso clínico

**Prioridad:** MUST · **Complejidad:** MODERADA

El sistema deberá presentar la información médica contextual del caso seleccionado antes de que el usuario tome una decisión.

**Criterio de aceptación.** El usuario puede consultar y analizar la información del caso antes de registrar su decisión.

#### RF-002.3 — Visualización del modelo 3D

**Prioridad:** MUST · **Complejidad:** ALTA

El sistema deberá renderizar el modelo 3D correspondiente al caso clínico seleccionado.

**Criterio de aceptación.** Al seleccionar un caso, el usuario puede visualizar su modelo 3D para facilitar su interpretación.

### Toma de decisiones e interacción con el agente tutor

#### RF-003.1 — Registro de la decisión inicial

**Prioridad:** MUST · **Complejidad:** MODERADA

El sistema deberá registrar la decisión inicial del usuario sobre el caso evaluado antes de mostrar cualquier sugerencia o recomendación del agente tutor.

**Criterio de aceptación.** El usuario selecciona una opción antes de recibir la recomendación del agente tutor.

#### RF-003.2 — Aviso sobre el carácter de los datos

**Prioridad:** MUST · **Complejidad:** BAJA

El sistema deberá mostrar un aviso explícito en la interfaz sobre el carácter académico y experimental de los datos presentados.

**Criterio de aceptación.** La interfaz muestra el aviso antes de que el usuario interprete los resultados.

#### RF-004.1 — Configuración de las respuestas del agente tutor

**Prioridad:** MUST · **Complejidad:** ALTA

El sistema deberá almacenar en el catálogo de casos clínicos los parámetros de las respuestas del agente tutor, incluidas las respuestas correctas e incorrectas preconfiguradas para la simulación.

**Criterio de aceptación.** Los casos configurados para la simulación presentan una recomendación definida.

#### RF-004.2 — Presentación de la recomendación

**Prioridad:** MUST · **Complejidad:** ALTA

El sistema deberá mostrar las recomendaciones generadas por el agente tutor únicamente después de que el usuario registre su decisión inicial.

**Criterio de aceptación.** La recomendación se muestra únicamente después de que el usuario selecciona y registra una opción.

#### RF-004.3 — Confirmación o modificación de la decisión

**Prioridad:** MUST · **Complejidad:** MODERADA

Después de mostrar la recomendación del agente tutor, el sistema deberá pedir al usuario que confirme o modifique su decisión.

**Criterio de aceptación.** El usuario puede mantener su opción inicial o cambiarla por la recomendación del agente tutor.

### Evaluación e historial

#### RF-005.1 — Comparación con la respuesta clínica correcta

**Prioridad:** MUST · **Complejidad:** MODERADA

El sistema deberá comparar la decisión de detección e interpretación registrada con la respuesta clínica correcta predefinida para el caso.

**Criterio de aceptación.** El sistema verifica si la decisión registrada coincide con la respuesta definida como correcta.

#### RF-005.2 — Coincidencia con la recomendación del agente tutor

**Prioridad:** MUST · **Complejidad:** MODERADA

El sistema deberá comparar la decisión final del usuario con la recomendación del agente tutor y registrar si coinciden.

**Criterio de aceptación.** El sistema identifica si la decisión final del usuario coincide con la recomendación del agente tutor.

#### RF-006.1 — Índice de coincidencia e interacción

**Prioridad:** MUST · **Complejidad:** MODERADA

El sistema deberá calcular, almacenar y mostrar el índice de coincidencia e interacción del usuario con el agente tutor en cada simulación. El índice deberá distinguir el porcentaje de decisiones modificadas para seguir la recomendación del agente tutor del porcentaje de decisiones iniciales que el usuario mantuvo.

**Criterio de aceptación.** Todas las decisiones quedan almacenadas y asociadas con el usuario y el caso clínico.

#### RF-006.2 — Consulta del historial

**Prioridad:** MUST · **Complejidad:** MODERADA

El sistema deberá proporcionar una interfaz para consultar el historial de casos clínicos evaluados, las decisiones tomadas, las recomendaciones del agente tutor y la retroalimentación correspondiente.

**Criterio de aceptación.** El usuario puede consultar y analizar las actividades que realizó en el simulador.

### Resumen de desempeño

#### RF-007.1 — Resumen visual del desempeño

**Prioridad:** COULD · **Complejidad:** MODERADA

El sistema podrá proporcionar un resumen visual del desempeño del usuario en los casos evaluados.

**Criterio de aceptación.** El resumen presenta los casos evaluados y muestra información sobre el desempeño del usuario.

## Requisitos no funcionales

### Usabilidad

#### RNF-004.1 — Uso sin asistencia técnica

**Prioridad:** MUST · **Complejidad:** MODERADA

La interfaz deberá permitir al usuario completar el flujo de simulación sin asistencia técnica.

**Criterio de aceptación.** Durante las pruebas se completa el flujo definido y la evaluación de usabilidad alcanza el umbral establecido previamente por el proyecto.

### Desempeño

#### RNF-006.1 — Disponibilidad durante las pruebas

**Prioridad:** MUST · **Complejidad:** MODERADA

El sistema deberá estar disponible durante las pruebas y el muestreo con estudiantes.

**Criterio de aceptación.** Durante las pruebas, los usuarios pueden acceder al simulador y completar los casos sin interrupciones.

### Requisitos lógicos de la base de datos

#### RNF-001.1 — Anonimización de los datos

**Prioridad:** MUST · **Complejidad:** MODERADA

El sistema deberá utilizar únicamente datos anonimizados que impidan identificar a los pacientes.

**Criterio de aceptación.** Cada caso clínico cuenta con información sobre su procedencia y está completamente anonimizado.

#### RNF-002.1 — Inmutabilidad de las decisiones

**Prioridad:** MUST · **Complejidad:** ALTA

El sistema deberá almacenar las decisiones en la base de datos de forma inmutable para garantizar su integridad y veracidad.

**Criterio de aceptación.** Las decisiones almacenadas no se pueden modificar directamente.

### Restricciones de diseño

#### RNF-007.1 — Comunicación con el agente tutor mediante una API

**Prioridad:** MUST · **Complejidad:** ALTA

El sistema y el agente tutor deberán comunicarse mediante una API que permita actualizar el agente sin afectar la interfaz web.

**Criterio de aceptación.** El simulador envía y recibe información mediante la API definida. Una actualización del agente tutor que mantenga esa API no requiere cambios en la interfaz web.

### Cumplimiento de estándares y normativas

#### RNF-003.1 — Aviso sobre el uso académico y experimental

**Prioridad:** MUST · **Complejidad:** BAJA

El sistema deberá mostrar un aviso que indique el carácter académico y experimental de los datos y resultados.

**Criterio de aceptación.** El aviso es visible antes de que el usuario interprete los resultados.

### Atributos del sistema

#### RNF-005.1 — Acceso mediante navegadores web

**Prioridad:** MUST · **Complejidad:** ALTA

El sistema deberá ser accesible desde distintos navegadores web sin necesidad de instalar software local.

**Criterio de aceptación.** El usuario puede acceder al simulador desde los navegadores definidos para las pruebas.

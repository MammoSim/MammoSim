# Arquitectura y diseño

MammoSim se organiza en una interfaz web, un backend, un agente tutor y una base de datos. Estos componentes permiten presentar casos simulados, registrar las decisiones del participante y mostrar la retroalimentación.

## Componentes del sistema

| Componente | Responsabilidad |
| --- | --- |
| Interfaz web | Presenta la información y las imágenes del caso, recoge las decisiones del participante y muestra los resultados. |
| Backend | Coordina el flujo de simulación, registra las decisiones y se comunica con el agente tutor mediante una API. |
| Agente tutor | Proporciona recomendaciones y retroalimentación durante la simulación. |
| Base de datos | Guarda la información de los casos, las decisiones y los resultados de las simulaciones. |

## Flujo de una simulación

1. El participante selecciona un caso y revisa la información y las imágenes.
2. Durante la simulación, registra sus decisiones e interactúa con las recomendaciones del agente tutor.
3. El sistema compara las decisiones con la respuesta de referencia del caso y con las recomendaciones del agente.
4. El participante consulta sus resultados y la retroalimentación recibida.

## Documentación técnica

- [Backend](backend/README.md) — Endpoints de la API, esquema de base de datos y guía del equipo.

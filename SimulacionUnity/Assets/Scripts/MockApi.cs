
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// API simulada de MammoSim.
// Imita el flujo de FastAPI sin guardar en Firestore.
// Lee los casos desde Resources/Casos/casos_mammosim.json.

public static class MockApi
{
    [Serializable]
    public class ListaCasos
    {
        public CasoCompleto[] casos;
    }

    [Serializable]
    public class CasoCompleto
    {
        public string id_caso;
        public string titulo;
        public string descripcion_breve;
        public string lado;
        public string[] vistas;
        public string dificultad;
        public int edad_simulada;
        public string modelo_3d_ref;
        public PreguntaCompleta[] preguntas;

        public string respuesta_correcta;
        public string opcion_ia_incorrecta;
        public string recomendacion_ia_correcta;
        public string recomendacion_ia_incorrecta;
        public string retroalimentacion;
        public string hallazgos;
    }

    [Serializable]
    public class PreguntaCompleta
    {
        public int numero;
        public string tipo;
        public string enunciado;
        public OpcionDto[] opciones;
        public string respuesta_correcta;
        public string explicacion;
        public bool con_ia;
    }

    // ============================================
    // ESTADO LOCAL DE LA SIMULACION
    // ============================================

    static CasoCompleto casoActual;

    static string sesionId;
    static string[] casosAsignados;
    static bool sesionActiva;

    static string decisionInicial;
    static string recomendacionIA;
    static string decisionFinal;

    static Dictionary<int, bool> respuestas =
        new Dictionary<int, bool>();

    // ============================================
    // CARGAR CASOS
    // ============================================

    static ListaCasos CargarCasos()
    {
        TextAsset archivo =
            Resources.Load<TextAsset>(
                "Casos/casos_mammosim"
            );

        if (archivo == null)
        {
            Debug.LogError(
                "[MockApi] No se encontro casos_mammosim.json"
            );
            return null;
        }

        ListaCasos datos =
            JsonUtility.FromJson<ListaCasos>(
                archivo.text
            );

        if (datos == null || datos.casos == null)
        {
            Debug.LogError(
                "[MockApi] No se pudieron leer los casos."
            );
            return null;
        }

        return datos;
    }

    static CasoDto PrepararCaso(CasoCompleto original)
    {
        CasoDto caso = new CasoDto
        {
            id_caso = original.id_caso,
            titulo = original.titulo,
            descripcion_breve = original.descripcion_breve,
            lado = original.lado,
            vistas = original.vistas,
            dificultad = original.dificultad,
            edad_simulada = original.edad_simulada,
            modelo_3d_ref = original.modelo_3d_ref,

            preguntas = new PreguntaDto[
                original.preguntas.Length
            ]
        };

        for (int i = 0; i < original.preguntas.Length; i++)
        {
            PreguntaCompleta p = original.preguntas[i];

            caso.preguntas[i] = new PreguntaDto
            {
                numero = p.numero,
                tipo = p.tipo,
                enunciado = p.enunciado,
                opciones = p.opciones
            };
        }

        return caso;
    }

    static string TextoOpcion(
        PreguntaCompleta pregunta,
        string id)
    {
        if (pregunta.opciones == null)
            return id;

        foreach (OpcionDto opcion in pregunta.opciones)
        {
            if (opcion.id == id)
                return opcion.texto;
        }

        return id;
    }

    static void ReiniciarCaso()
    {
        decisionInicial = null;
        recomendacionIA = null;
        decisionFinal = null;
        respuestas.Clear();
    }

    // ============================================
    // SIMULAR PETICIONES
    // ============================================

    public static string Responder(string path, string body)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        // ----------------------------------------
        // GET /sesiones/activa
        // ----------------------------------------

        if (path == "/sesiones/activa")
        {
            // Sin sesion activa: devolvemos null.
            // ApiClient lo convierte en "Sin datos",
            // que GameManager maneja creando una sesion.
            if (!sesionActiva)
                return null;

            string[] terminados =
                decisionFinal != null &&
                casoActual != null
                    ? new[] { casoActual.id_caso }
                    : new string[0];

            return JsonUtility.ToJson(
                new SesionActivaRes
                {
                    id_sesion = sesionId,
                    casos_asignados = casosAsignados,
                    casos_terminados = terminados,
                    caso_actual = decisionFinal == null
                        ? casoActual != null
                            ? casoActual.id_caso
                            : casosAsignados[0]
                        : null,
                    estado = "en_curso"
                }
            );
        }

        // ----------------------------------------
        // POST /sesiones
        // ----------------------------------------

        if (path == "/sesiones")
        {
            if (sesionActiva)
            {
                Debug.LogWarning(
                    "[MockApi] Ya existe una sesion activa."
                );
                return null;
            }

            CrearSesionReq solicitud =
                JsonUtility.FromJson<CrearSesionReq>(body);

            if (solicitud == null ||
                solicitud.casos == null ||
                solicitud.casos.Length == 0)
            {
                Debug.LogError(
                    "[MockApi] La sesion necesita casos."
                );
                return null;
            }

            sesionId = "ses_prueba_001";
            casosAsignados = solicitud.casos;
            sesionActiva = true;
            casoActual = null;

            ReiniciarCaso();

            Debug.Log(
                "[MockApi] Sesion creada: " + sesionId
            );

            return JsonUtility.ToJson(
                new SesionRes
                {
                    id_sesion = sesionId,
                    casos_asignados = casosAsignados,
                    estado = "en_curso"
                }
            );
        }

        // ----------------------------------------
        // GET /casos/{id_caso}
        // ----------------------------------------

        if (path.StartsWith("/casos/"))
        {
            ListaCasos datos = CargarCasos();

            if (datos == null)
                return null;

            string idCaso = path
                .Substring("/casos/".Length)
                .Split('?')[0]
                .TrimEnd('/');

            foreach (CasoCompleto caso in datos.casos)
            {
                if (caso.id_caso != idCaso)
                    continue;

                casoActual = caso;
                ReiniciarCaso();

                Debug.Log(
                    "[MockApi] Caso cargado: " + idCaso
                );

                return JsonUtility.ToJson(
                    PrepararCaso(caso)
                );
            }

            Debug.LogWarning(
                "[MockApi] Caso no encontrado: " + idCaso
            );

            return null;
        }

        // ----------------------------------------
        // POST /sesiones/{id}/cerrar
        // ----------------------------------------

        if (path.EndsWith("/cerrar"))
        {
            if (!sesionActiva ||
                decisionFinal == null ||
                casoActual == null)
            {
                Debug.LogWarning(
                    "[MockApi] El caso aun no ha terminado."
                );
                return null;
            }

            int preguntasAcertadas =
                respuestas.Values.Count(valor => valor);

            bool aciertoInicial =
                decisionInicial ==
                casoActual.respuesta_correcta;

            bool aciertoFinal =
                decisionFinal ==
                casoActual.respuesta_correcta;

            bool siguioIA =
                decisionFinal == recomendacionIA;

            bool iaCorrecta =
                recomendacionIA ==
                casoActual.respuesta_correcta;

            // Nota ilustrativa del Mock.
            // FastAPI utiliza su propio sistema de puntuacion.
            float nota = (
                preguntasAcertadas +
                (aciertoInicial ? 1 : 0) +
                (aciertoFinal ? 1 : 0)
            ) * 10f / 9f;

            ResultadoSesionRes resultado =
                new ResultadoSesionRes
                {
                    total_casos = 1,
                    preguntas_acertadas = preguntasAcertadas,
                    aciertos_iniciales =
                        aciertoInicial ? 1 : 0,
                    aciertos_finales =
                        aciertoFinal ? 1 : 0,
                    veces_siguio_ia =
                        siguioIA ? 1 : 0,
                    siguio_ia_correcta =
                        siguioIA && iaCorrecta ? 1 : 0,
                    siguio_ia_incorrecta =
                        siguioIA && !iaCorrecta ? 1 : 0,
                    puntuacion = nota
                };

            sesionActiva = false;

            Debug.Log(
                "[MockApi] Sesion completada. Nota de prueba: " +
                nota
            );

            return JsonUtility.ToJson(resultado);
        }

        // ----------------------------------------
        // VERIFICAR CASO ACTUAL
        // ----------------------------------------

        if (!sesionActiva || casoActual == null)
        {
            Debug.LogError(
                "[MockApi] No existe un caso activo."
            );
            return null;
        }

        // ----------------------------------------
        // POST /respuestas
        // ----------------------------------------

        if (path.EndsWith("/respuestas"))
        {
            RespuestaReq solicitud =
                JsonUtility.FromJson<RespuestaReq>(body);

            if (solicitud == null)
                return null;

            if (respuestas.ContainsKey(
                solicitud.numero_pregunta))
            {
                Debug.LogWarning(
                    "[MockApi] Pregunta ya respondida."
                );
                return null;
            }

            foreach (PreguntaCompleta pregunta
                     in casoActual.preguntas)
            {
                if (pregunta.numero !=
                    solicitud.numero_pregunta)
                    continue;

                if (pregunta.tipo == "diagnostica")
                    return null;

                bool correcta =
                    solicitud.opcion_elegida ==
                    pregunta.respuesta_correcta;

                respuestas.Add(
                    pregunta.numero,
                    correcta
                );

                string textoCorrecto = TextoOpcion(
                    pregunta,
                    pregunta.respuesta_correcta
                );

                RespuestaRes resultado = new RespuestaRes
                {
                    correcta = correcta,

                    respuesta_correcta =
                        pregunta.respuesta_correcta,

                    opcion_correcta =
                        pregunta.respuesta_correcta,

                    texto_correcto = textoCorrecto,

                    explicacion = pregunta.explicacion,

                    mensaje = correcta
                        ? "Correcto. " + pregunta.explicacion
                        : "Incorrecto. " + pregunta.explicacion
                };

                Debug.Log(
                    "[MockApi] Pregunta " +
                    pregunta.numero +
                    " | Correcta: " + correcta
                );

                return JsonUtility.ToJson(resultado);
            }

            return null;
        }

        // ----------------------------------------
        // POST /decision-inicial
        // ----------------------------------------

        if (path.EndsWith("/decision-inicial"))
        {
            if (respuestas.Count < 7 ||
                decisionInicial != null)
            {
                Debug.LogWarning(
                    "[MockApi] No se puede guardar " +
                    "la decision inicial."
                );
                return null;
            }

            DecisionReq solicitud =
                JsonUtility.FromJson<DecisionReq>(body);

            if (solicitud == null)
                return null;

            decisionInicial = solicitud.decision;

            // Recomendacion incorrecta intencional
            // para probar el comportamiento del tutor.
            recomendacionIA =
                casoActual.opcion_ia_incorrecta;

            Debug.Log(
                "[MockApi] Decision inicial: " +
                decisionInicial
            );

            return JsonUtility.ToJson(
                new GuardadaRes { guardada = true }
            );
        }

        // ----------------------------------------
        // GET /recomendacion-ia
        // ----------------------------------------

        if (path.EndsWith("/recomendacion-ia"))
        {
            if (decisionInicial == null)
                return null;

            RecomendacionRes resultado =
                new RecomendacionRes
                {
                    opcion_ia = recomendacionIA,

                    // El HUD original utiliza conducta.
                    // Debe contener el ID, no el texto.
                    conducta = recomendacionIA,

                    texto =
                        casoActual.recomendacion_ia_incorrecta
                };

            Debug.Log(
                "[MockApi] Recomendacion IA: " +
                recomendacionIA
            );

            return JsonUtility.ToJson(resultado);
        }

        // ----------------------------------------
        // POST /decision-final
        // ----------------------------------------

        if (path.EndsWith("/decision-final"))
        {
            if (decisionInicial == null ||
                decisionFinal != null)
            {
                Debug.LogWarning(
                    "[MockApi] No se puede guardar " +
                    "la decision final."
                );
                return null;
            }

            DecisionReq solicitud =
                JsonUtility.FromJson<DecisionReq>(body);

            if (solicitud == null)
                return null;

            decisionFinal = solicitud.decision;

            Debug.Log(
                "[MockApi] Decision final: " +
                decisionFinal
            );

            return JsonUtility.ToJson(
                new GuardadaRes { guardada = true }
            );
        }

        // ----------------------------------------
        // GET /retroalimentacion
        // ----------------------------------------

        if (path.EndsWith("/retroalimentacion"))
        {
            if (decisionFinal == null)
                return null;

            bool iaCorrecta =
                recomendacionIA ==
                casoActual.respuesta_correcta;

            bool siguioIA =
                decisionFinal == recomendacionIA;

            int acertadas =
                respuestas.Values.Count(v => v);

            bool aciertoInicial =
                decisionInicial ==
                casoActual.respuesta_correcta;

            bool aciertoFinal =
                decisionFinal ==
                casoActual.respuesta_correcta;

            var preguntas = respuestas
                .OrderBy(r => r.Key)
                .Select(r => new PreguntaResultadoRes
                {
                    numero = r.Key,
                    correcta = r.Value
                })
                .ToArray();

            RetroalimentacionRes resultado =
                new RetroalimentacionRes
                {
                    hallazgos = casoActual.hallazgos,

                    retroalimentacion =
                        casoActual.retroalimentacion,

                    respuesta_correcta =
                        casoActual.respuesta_correcta,

                    preguntas = preguntas,

                    ia_correcta = iaCorrecta,

                    siguio_ia = siguioIA,

                    // Calculo local ilustrativo.
                    nota_caso = (
                        acertadas +
                        (aciertoInicial ? 1 : 0) +
                        (aciertoFinal ? 1 : 0)
                    ) * 10f / 9f
                };

            Debug.Log(
                "[MockApi] Resultado | " +
                "Acierto inicial: " + aciertoInicial +
                " | Acierto final: " + aciertoFinal +
                " | Siguio IA: " + siguioIA
            );

            return JsonUtility.ToJson(resultado);
        }

        Debug.LogWarning(
            "[MockApi] Ruta no reconocida: " + path
        );

        return null;
    }
}

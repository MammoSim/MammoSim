
using System;
using UnityEngine;

// API simulada para probar MammoSim sin backend.
// Lee los casos desde Assets/Resources/Casos/casos_mammosim.json.
public static class MockApi
{
    [Serializable]
    public class ListaCasos
    {
        public CasoCompleto[] casos;
    }

    // Clases internas: contienen las respuestas para evaluar localmente.
    // No modifican los DTO de Models.cs.
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

    // Guardamos las decisiones de la simulación actual.
    static CasoCompleto casoActual;
    static string decisionInicial;
    static string recomendacionIA;
    static string decisionFinal;

    static ListaCasos CargarCasos()
    {
        TextAsset archivo =
            Resources.Load<TextAsset>("Casos/casos_mammosim");

        if (archivo == null)
        {
            Debug.LogError(
                "MammoSim: No se encontró casos_mammosim.json."
            );
            return null;
        }

        ListaCasos datos =
            JsonUtility.FromJson<ListaCasos>(archivo.text);

        if (datos == null || datos.casos == null)
        {
            Debug.LogError(
                "MammoSim: No se pudieron leer los casos."
            );
            return null;
        }

        return datos;
    }

    // Devuelve únicamente los datos que necesita la interfaz.
    // Las respuestas correctas no se incluyen en el caso enviado.
    static CasoDto PrepararCaso(CasoCompleto original)
    {
        CasoDto caso = new CasoDto();

        caso.id_caso = original.id_caso;
        caso.titulo = original.titulo;
        caso.descripcion_breve = original.descripcion_breve;
        caso.lado = original.lado;
        caso.vistas = original.vistas;
        caso.dificultad = original.dificultad;
        caso.edad_simulada = original.edad_simulada;
        caso.modelo_3d_ref = original.modelo_3d_ref;

        caso.preguntas = new PreguntaDto[original.preguntas.Length];

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

    static string TextoOpcion(PreguntaCompleta pregunta, string id)
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

    static string TextoConducta(string id)
    {
        if (casoActual == null || casoActual.preguntas == null)
            return id;

        foreach (PreguntaCompleta pregunta in casoActual.preguntas)
        {
            if (pregunta.tipo == "diagnostica")
                return TextoOpcion(pregunta, id);
        }

        return id;
    }

    public static string Responder(string path, string body)
    {
        // Crear sesión de prueba.
        if (path == "/sesiones")
        {
            return JsonUtility.ToJson(new SesionRes
            {
                sesion_id = "ses_prueba_001",
                casos_asignados = new string[] { "caso_001" }
            });
        }

        // Cargar el caso solicitado.
        if (path.StartsWith("/casos/"))
        {
            ListaCasos datos = CargarCasos();

            if (datos == null)
                return null;

            string idCaso = path.Substring("/casos/".Length);
            idCaso = idCaso.Split('?')[0].TrimEnd('/');

            foreach (CasoCompleto caso in datos.casos)
            {
                if (caso.id_caso == idCaso)
                {
                    casoActual = caso;

                    // Reiniciamos las decisiones al iniciar otro caso.
                    decisionInicial = null;
                    recomendacionIA = null;
                    decisionFinal = null;

                    Debug.Log(
                        "MammoSim: Caso cargado: " + idCaso +
                        " | Preguntas: " + caso.preguntas.Length
                    );

                    return JsonUtility.ToJson(PrepararCaso(caso));
                }
            }

            Debug.LogWarning(
                "MammoSim: No se encontró el caso " + idCaso
            );
            return null;
        }

        if (casoActual == null)
        {
            Debug.LogError(
                "MammoSim: Debes cargar un caso antes de responder."
            );
            return null;
        }

        // Evaluar las preguntas técnicas usando el JSON real.
        if (path.EndsWith("/respuestas"))
        {
            RespuestaReq respuesta =
                JsonUtility.FromJson<RespuestaReq>(body);

            foreach (PreguntaCompleta pregunta in casoActual.preguntas)
            {
                if (pregunta.numero == respuesta.numero_pregunta)
                {
                    RespuestaRes resultado = new RespuestaRes
                    {
                        correcta =
                            respuesta.opcion == pregunta.respuesta_correcta,

                        opcion_correcta = pregunta.respuesta_correcta,

                        texto_correcto = TextoOpcion(
                            pregunta,
                            pregunta.respuesta_correcta
                        ),

                        explicacion = pregunta.explicacion
                    };

                    return JsonUtility.ToJson(resultado);
                }
            }

            Debug.LogWarning(
                "MammoSim: Pregunta no encontrada: " +
                respuesta.numero_pregunta
            );

            return null;
        }

        // Primera decisión y recomendación de la IA.
        if (path.EndsWith("/decision-inicial"))
        {
            DecisionReq solicitud =
                JsonUtility.FromJson<DecisionReq>(body);

            decisionInicial = solicitud.decision;
            decisionFinal = null;

            // Para las pruebas usamos la recomendación
            // incorrecta intencional incluida en cada caso.
            recomendacionIA = casoActual.opcion_ia_incorrecta;

            RecomendacionRes recomendacion = new RecomendacionRes
            {
                conducta = TextoConducta(recomendacionIA),
                texto = casoActual.recomendacion_ia_incorrecta
            };

            Debug.Log(
                "MammoSim: Decisión inicial: " + decisionInicial
            );

            Debug.Log(
                "MammoSim: Recomendación IA: " + recomendacionIA
            );

            return JsonUtility.ToJson(recomendacion);
        }

        // Decisión final y evaluación.
        if (path.EndsWith("/decision-final"))
        {
            DecisionReq solicitud =
                JsonUtility.FromJson<DecisionReq>(body);

            decisionFinal = solicitud.decision;

            bool aciertoInicial =
                decisionInicial == casoActual.respuesta_correcta;

            bool aciertoFinal =
                decisionFinal == casoActual.respuesta_correcta;

            bool siguioIA =
                decisionFinal == recomendacionIA;

            ResultadoCasoRes resultado = new ResultadoCasoRes
            {
                acierto_inicial = aciertoInicial,
                acierto_final = aciertoFinal,
                siguio_ia = siguioIA,
                hallazgos = casoActual.hallazgos,
                retroalimentacion = casoActual.retroalimentacion
            };

            Debug.Log(
                "MammoSim: Decisión inicial: " + decisionInicial +
                " | Recomendación IA: " + recomendacionIA +
                " | Decisión final: " + decisionFinal
            );

            Debug.Log(
                "MammoSim: Acierto inicial: " + aciertoInicial +
                " | Acierto final: " + aciertoFinal +
                " | Siguió IA: " + siguioIA
            );

            return JsonUtility.ToJson(resultado);
        }

        Debug.LogWarning(
            "MammoSim: Ruta no reconocida: " + path
        );

        return null;
    }
}

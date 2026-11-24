
using System;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] ApiClient api;
    [SerializeField] SimulationScreen simulationScreen;

    public FaseSimulacion Fase { get; private set; }
    public string DecisionInicial { get; private set; }
    public CaseModel CasoActual { get; private set; }

    string _sesionId;
    string _idCurso;
    string[] _casosSesion;
    string _recomendacionIA;
    bool _recuperandoSesion;

    int _i;
    bool _enviando;

    PreguntaModel Actual => CasoActual.Preguntas[_i];

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ==============================================
    // ENTRADA DESDE REACT
    // ==============================================

    // React debe llamar a SetCurso antes de IniciarCaso.
    // Valor vacío = práctica individual; id válido = clase dirigida.
    public void SetCurso(string idCurso)
    {
        if (!string.IsNullOrEmpty(_sesionId) || _enviando)
        {
            Debug.LogWarning(
                "[MammoSim] La sesión ya está iniciada o se está iniciando; no se cambia su curso."
            );
            return;
        }

        _idCurso = string.IsNullOrWhiteSpace(idCurso)
            ? null
            : idCurso.Trim();
    }

    public void RecibirCasoDesdeReact(string casoId)
    {
        if (string.IsNullOrWhiteSpace(casoId))
        {
            Debug.LogWarning(
                "[MammoSim] React no envió un caso válido."
            );
            return;
        }

        if (simulationScreen == null)
        {
            Debug.LogError(
                "[MammoSim] SimulationScreen no está asignado."
            );
            return;
        }

        Debug.Log("[MammoSim] Caso seleccionado: " + casoId);

        simulationScreen.Abrir(casoId);
    }

    // ==============================================
    // INICIAR CASO
    // ==============================================

    public void IniciarCaso(string casoId)
    {
        if (_enviando)
            return;

        if (api == null)
        {
            Fallo("ApiClient no está asignado.");
            return;
        }

        DecisionInicial = null;
        _recuperandoSesion = false;
        _recomendacionIA = null;
        CasoActual = null;

        _i = 0;
        _enviando = true;

        CambiarFase(FaseSimulacion.Cargando);

        AsegurarSesion(casoId, () =>
        {
            StartCoroutine(api.Get<CasoDto>(
                $"/casos/{casoId}",
                dto =>
                {
                    if (dto == null ||
                        dto.preguntas == null ||
                        dto.preguntas.Length == 0)
                    {
                        Fallo("El caso no contiene preguntas.");
                        return;
                    }

                    try
                    {
                        CasoActual = CaseDtoAdapter.Adaptar(dto);
                    }
                    catch (Exception e)
                    {
                        Fallo("Error cargando el caso: " + e.Message);
                        return;
                    }

                    if (HistorialManager.Instance != null)
                    {
                        HistorialManager.Instance.IniciarAnalisis(
                            CasoActual.Id,
                            dto.titulo
                        );
                    }

                    SimulationEvents.OnCasoSeleccionado?.Invoke(CasoActual);

                    if (_recuperandoSesion)
                    {
                        ConsultarProgreso();
                    }
                    else
                    {
                        _enviando = false;
                        MostrarActual();
                    }
                },
                Fallo
            ));
        });
    }

    // ==============================================
    // SESIONES
    // ==============================================

    void AsegurarSesion(string casoId, Action alTenerla)
    {
        if (!string.IsNullOrEmpty(_sesionId))
        {
            if (_casosSesion != null && !_casosSesion.Contains(casoId))
            {
                Fallo("El caso seleccionado no pertenece a la sesión actual.");
                return;
            }

            alTenerla();
            return;
        }

        // El backend permite una sola sesión en_curso por estudiante.
        StartCoroutine(api.Get<SesionActivaRes>(
            "/sesiones/activa",
            activa =>
            {
                if (activa == null || string.IsNullOrEmpty(activa.id_sesion))
                {
                    CrearSesion(casoId, alTenerla);
                    return;
                }

                if (activa.casos_asignados == null ||
                    activa.casos_asignados.Length == 0)
                {
                    Fallo("La sesión activa no tiene casos asignados.");
                    return;
                }

                // Puede suceder que el último caso se haya terminado, pero
                // el usuario cerrara la página antes de pulsar Terminar.
                // En ese caso cerramos la sesión COMPLETA, nunca una parcial.
                bool todosTerminados = activa.casos_terminados != null &&
                    activa.casos_asignados.All(id =>
                        activa.casos_terminados.Contains(id));

                if (todosTerminados)
                {
                    CerrarSesionCompletada(activa.id_sesion, () =>
                        CrearSesion(casoId, alTenerla));
                    return;
                }

                // Nunca recuperar una sesión individual como sesión de clase,
                // ni una sesión perteneciente a un curso diferente.
                string cursoActivo = string.IsNullOrWhiteSpace(activa.id_curso)
                    ? null
                    : activa.id_curso.Trim();

                if (!string.Equals(cursoActivo, _idCurso,
                    StringComparison.Ordinal))
                {
                    Fallo("Hay una sesión pendiente de otra modalidad o curso. " +
                          "Debes terminar sus casos antes de iniciar esta práctica.");
                    return;
                }

                if (!activa.casos_asignados.Contains(casoId))
                {
                    Fallo("Tienes una sesión pendiente con otro caso. " +
                          "Debes continuarla primero" +
                          (string.IsNullOrEmpty(activa.caso_actual)
                              ? "."
                              : ": " + activa.caso_actual + "."));
                    return;
                }

                if (activa.casos_terminados != null &&
                    activa.casos_terminados.Contains(casoId))
                {
                    Fallo("Este caso ya fue terminado en la sesión activa. " +
                          "Continúa los demás casos pendientes.");
                    return;
                }

                _sesionId = activa.id_sesion;
                _casosSesion = activa.casos_asignados;
                _recuperandoSesion = true;

                Debug.Log("[MammoSim] Recuperando sesión: " + _sesionId);
                alTenerla();
            },
            error =>
            {
                if (error.StartsWith("HTTP 404"))
                    CrearSesion(casoId, alTenerla);
                else
                    Fallo(error);
            }
        ));
    }

    // Recuperación de una sesión cuyo último caso ya está terminado.
    // No cierra sesiones parciales; FastAPI también valida esta condición.
    void CerrarSesionCompletada(string sesionId, Action alCerrar)
    {
        StartCoroutine(api.Post<VacioReq, ResultadoSesionRes>(
            $"/sesiones/{sesionId}/cerrar",
            new VacioReq(),
            resultado =>
            {
                Debug.Log("[MammoSim] Sesión anterior completada: " + sesionId);
                alCerrar();
            },
            error => Fallo("No se pudo cerrar la sesión terminada: " + error)
        ));
    }

    void CrearSesion(string casoId, Action alTenerla)
    {
        var datos = new CrearSesionReq
        {
            id_curso = _idCurso,
            casos = new[] { casoId }
        };

        StartCoroutine(api.Post<CrearSesionReq, SesionRes>(
            "/sesiones",
            datos,
            s =>
            {
                if (s == null ||
                    string.IsNullOrEmpty(s.id_sesion))
                {
                    Fallo(
                        "El backend no devolvió id_sesion."
                    );
                    return;
                }

                _sesionId = s.id_sesion;
                _casosSesion = new[] { casoId };

                Debug.Log(
                    "[MammoSim] Sesión creada: " + _sesionId
                );

                alTenerla();
            },
            Fallo
        ));
    }

    string Ruta(string sufijo)
    {
        return
            $"/sesiones/{_sesionId}/casos/{CasoActual.Id}/{sufijo}";
    }

    // ==============================================
    // RESPUESTAS Y DECISIONES
    // ==============================================

    public void EnviarOpcion(string opcion)
    {
        if (_enviando || CasoActual == null)
            return;

        if (string.IsNullOrWhiteSpace(opcion))
            return;

        switch (Fase)
        {
            case FaseSimulacion.Pregunta:
                EnviarRespuestaTecnica(opcion);
                break;

            case FaseSimulacion.DecisionInicial:
                EnviarDecisionInicial(opcion);
                break;

            case FaseSimulacion.DecisionFinal:
                EnviarDecisionFinal(opcion);
                break;
        }
    }

    // ==============================================
    // PREGUNTAS TÉCNICAS 1-7
    // ==============================================

    void EnviarRespuestaTecnica(string opcion)
    {
        _enviando = true;

        StartCoroutine(api.Post<RespuestaReq, RespuestaRes>(
            Ruta("respuestas"),
            new RespuestaReq
            {
                numero_pregunta = Actual.Numero,
                opcion_elegida = opcion
            },
            r =>
            {
                // Adaptar respuesta para el HUD actual.
                if (string.IsNullOrEmpty(r.opcion_correcta))
                    r.opcion_correcta = r.respuesta_correcta;

                if (string.IsNullOrEmpty(r.texto_correcto))
                {
                    var correcta = Actual.Opciones
                        .FirstOrDefault(
                            o => o.Id == r.opcion_correcta
                        );

                    r.texto_correcto = correcta != null
                        ? correcta.Texto
                        : r.opcion_correcta;
                }

                if (HistorialManager.Instance != null)
                {
                    HistorialManager.Instance.RegistrarRespuesta(
                        Actual.Numero,
                        Actual.Enunciado,
                        opcion,
                        r.opcion_correcta,
                        r.correcta,
                        r.explicacion
                    );
                }

                _enviando = false;

                CambiarFase(
                    FaseSimulacion.Retroalimentacion
                );

                SimulationEvents.OnRetroalimentacion
                    ?.Invoke(r);
            },
            Fallo
        ));
    }

    // ==============================================
    // DECISIÓN INICIAL
    // ==============================================

    
void EnviarDecisionInicial(string opcion)
{
    _enviando = true;

    StartCoroutine(api.Post<DecisionReq, GuardadaRes>(
        Ruta("decision-inicial"),
        new DecisionReq { decision = opcion },
        guardado =>
        {
            if (guardado == null || !guardado.guardada)
            {
                Fallo("No se confirmó la decisión inicial.");
                return;
            }

            DecisionInicial = opcion;

            SimulationEvents.OnDecisionRegistrada
                ?.Invoke("inicial");

            // El backend entrega la recomendación
            // mediante una petición independiente.
            StartCoroutine(api.Get<RecomendacionRes>(
                Ruta("recomendacion-ia"),
                MostrarRecomendacion,
                Fallo
            ));
        },
        Fallo
    ));
}


    void MostrarRecomendacion(RecomendacionRes r)
    {
        if (r == null)
        {
            Fallo("No se recibió la recomendación de IA.");
            return;
        }

        if (string.IsNullOrEmpty(r.conducta))
            r.conducta = r.opcion_ia;

        if (string.IsNullOrEmpty(r.opcion_ia))
            r.opcion_ia = r.conducta;

        if (string.IsNullOrEmpty(r.conducta))
        {
            Fallo("La recomendación no tiene una opción válida.");
            return;
        }

        _recomendacionIA = r.conducta;
        _enviando = false;

        if (HistorialManager.Instance != null)
        {
            HistorialManager.Instance.RegistrarDecisionInicial(
                DecisionInicial,
                r.conducta
            );
        }

        CambiarFase(FaseSimulacion.DecisionFinal);

        SimulationEvents.OnPreguntaMostrada?.Invoke(
            Actual,
            _i,
            CasoActual.Preguntas.Length
        );

        SimulationEvents.OnRecomendacionRecibida
            ?.Invoke(r);
    }

    // ==============================================
    // DECISIÓN FINAL
    // ==============================================

   
void EnviarDecisionFinal(string opcion)
{
    _enviando = true;

    StartCoroutine(api.Post<DecisionReq, GuardadaRes>(
        Ruta("decision-final"),
        new DecisionReq { decision = opcion },
        guardado =>
        {
            if (guardado == null || !guardado.guardada)
            {
                Fallo("No se confirmó la decisión final.");
                return;
            }

            // La evaluación se consulta después
            // de registrar la decisión final.
            StartCoroutine(api.Get<RetroalimentacionRes>(
                Ruta("retroalimentacion"),
                retro =>
                {
                    if (retro == null ||
                        string.IsNullOrEmpty(
                            retro.respuesta_correcta))
                    {
                        Fallo(
                            "No se recibió la retroalimentación."
                        );
                        return;
                    }

                    var resultado = new ResultadoCasoRes
                    {
                        acierto_inicial =
                            DecisionInicial ==
                            retro.respuesta_correcta,

                        acierto_final =
                            opcion == retro.respuesta_correcta,

                        siguio_ia = retro.siguio_ia,

                        hallazgos = retro.hallazgos,

                        retroalimentacion =
                            retro.retroalimentacion
                    };

                    MostrarResultado(opcion, resultado);
                },
                Fallo
            ));
        },
        Fallo
    ));
}


    void MostrarResultado(
        string decisionFinal,
        ResultadoCasoRes resultado)
    {
        _enviando = false;

        if (HistorialManager.Instance != null)
        {
            HistorialManager.Instance.RegistrarResultadoFinal(
                decisionFinal,
                resultado
            );

            HistorialManager.Instance.GuardarHistorial();
        }

        SimulationEvents.OnDecisionRegistrada
            ?.Invoke("final");

        CambiarFase(FaseSimulacion.Resultado);

        SimulationEvents.OnEvaluacionDisponible
            ?.Invoke(resultado);
    }

    // ==============================================
    // SIGUIENTE PREGUNTA
    // ==============================================

    public void Siguiente()
    {
        if (_enviando ||
            Fase != FaseSimulacion.Retroalimentacion)
            return;

        _i++;

        if (_i >= CasoActual.Preguntas.Length)
        {
            Fallo("No hay más preguntas en este caso.");
            return;
        }

        MostrarActual();
    }

void ConsultarProgreso()
{
    StartCoroutine(api.Get<ProgresoCasoRes>(
        Ruta("progreso"),
        progreso =>
        {
            if (progreso == null)
            {
                Fallo("No se pudo recuperar el progreso.");
                return;
            }

            DecisionInicial = progreso.decision_inicial;

            // Si la decisión final ya se guardó,
            // recuperamos el resultado del caso.
            if (progreso.decision_final_guardada)
            {
                if (string.IsNullOrEmpty(progreso.decision_final))
                {
                    Fallo("Falta la decisión final guardada.");
                    return;
                }

                StartCoroutine(api.Get<RetroalimentacionRes>(
                    Ruta("retroalimentacion"),
                    retro =>
                    {
                        if (retro == null)
                        {
                            Fallo("No se pudo recuperar el resultado.");
                            return;
                        }

                        var resultado = new ResultadoCasoRes
                        {
                            acierto_inicial =
                                DecisionInicial == retro.respuesta_correcta,
                            acierto_final =
                                progreso.decision_final == retro.respuesta_correcta,
                            siguio_ia = retro.siguio_ia,
                            hallazgos = retro.hallazgos,
                            retroalimentacion = retro.retroalimentacion
                        };

                        _i = CasoActual.Preguntas.Length - 1;
                        _enviando = false;

                        // Pasamos por la fase diagnóstica
                        // para respetar las transiciones existentes.
                        CambiarFase(FaseSimulacion.DecisionInicial);
                        CambiarFase(FaseSimulacion.DecisionFinal);
                        MostrarResultado(progreso.decision_final, resultado);
                    },
                    Fallo
                ));

                return;
            }

            // Ya existe la decisión inicial:
            // solamente falta consultar la recomendación.
            if (progreso.decision_inicial_guardada)
            {
                if (string.IsNullOrEmpty(DecisionInicial))
                {
                    Fallo("No se recuperó la decisión inicial.");
                    return;
                }

                _i = CasoActual.Preguntas.Length - 1;

                CambiarFase(FaseSimulacion.DecisionInicial);

                StartCoroutine(api.Get<RecomendacionRes>(
                    Ruta("recomendacion-ia"),
                    MostrarRecomendacion,
                    Fallo
                ));

                return;
            }

            // Recuperar la siguiente pregunta sin
            // repetir las respuestas anteriores.
            int numero = progreso.siguiente_pregunta;

            if (numero >= 1 && numero <= 7)
            {
                int indice = Array.FindIndex(
                    CasoActual.Preguntas,
                    p => p.Numero == numero
                );

                if (indice < 0)
                {
                    Fallo("La pregunta pendiente no existe.");
                    return;
                }

                _i = indice;
                _enviando = false;
                MostrarActual();
                return;
            }

            // Las siete preguntas ya fueron respondidas.
            // Falta registrar la decisión inicial.
            if (progreso.preguntas_respondidas != null &&
                progreso.preguntas_respondidas.Length >= 7)
            {
                _i = CasoActual.Preguntas.Length - 1;
                _enviando = false;
                MostrarActual();
                return;
            }

            Fallo("El progreso recibido es inconsistente.");
        },
        Fallo
    ));
}

    void MostrarActual()
    {
        CambiarFase(
            Actual.EsDiagnostica
                ? FaseSimulacion.DecisionInicial
                : FaseSimulacion.Pregunta
        );

        SimulationEvents.OnPreguntaMostrada?.Invoke(
            Actual,
            _i,
            CasoActual.Preguntas.Length
        );
    }

    // ==============================================
    // TERMINAR SESIÓN
    // ==============================================

    public void Terminar()
    {
        if (_enviando ||
            Fase != FaseSimulacion.Resultado)
            return;

        if (string.IsNullOrEmpty(_sesionId))
        {
            Fallo("No existe una sesión para cerrar.");
            return;
        }

        _enviando = true;

        StartCoroutine(api.Post<VacioReq, ResultadoSesionRes>(
            $"/sesiones/{_sesionId}/cerrar",
            new VacioReq(),
            resultado =>
            {
                _enviando = false;

                Debug.Log(
                    "[MammoSim] Sesión completada. Nota: " +
                    resultado.puntuacion
                );

                _sesionId = null;
                _casosSesion = null;

                SimulationEvents.OnSimulacionFinalizada
                    ?.Invoke();
            },
            Fallo
        ));
    }

    // ==============================================
    // CONTROL DE FASES
    // ==============================================

    static bool Permitida(
        FaseSimulacion anterior,
        FaseSimulacion nueva)
    {
        if (nueva == FaseSimulacion.Cargando ||
            nueva == FaseSimulacion.Error)
            return true;

        return (anterior, nueva) switch
        {
            (FaseSimulacion.Cargando,
             FaseSimulacion.Pregunta) => true,

            (FaseSimulacion.Cargando,
             FaseSimulacion.DecisionInicial) => true,

            (FaseSimulacion.Pregunta,
             FaseSimulacion.Retroalimentacion) => true,

            (FaseSimulacion.Retroalimentacion,
             FaseSimulacion.Pregunta) => true,

            (FaseSimulacion.Retroalimentacion,
             FaseSimulacion.DecisionInicial) => true,

            (FaseSimulacion.DecisionInicial,
             FaseSimulacion.DecisionFinal) => true,

            (FaseSimulacion.DecisionFinal,
             FaseSimulacion.Resultado) => true,

            _ => false
        };
    }

    void CambiarFase(FaseSimulacion nueva)
    {
        if (!Permitida(Fase, nueva))
        {
            Debug.LogWarning(
                "[MammoSim] Transición no permitida: " +
                Fase + " -> " + nueva
            );
            return;
        }

        Fase = nueva;

        SimulationEvents.OnFase?.Invoke(nueva);
    }

    // ==============================================
    // ERRORES
    // ==============================================

    void Fallo(string mensaje)
    {
        _enviando = false;

        Debug.LogError("[MammoSim] " + mensaje);

        CambiarFase(FaseSimulacion.Error);

        SimulationEvents.OnError?.Invoke(mensaje);
    }
}

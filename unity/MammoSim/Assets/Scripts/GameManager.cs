using System;
using System.Linq;
using UnityEngine;

// Patrón Singleton: una única instancia por ejecución del cliente Unity.
// Coordina la sesión, el caso actual y el flujo:
// 7 técnicas (con retroalimentación) -> decisión inicial (P8) -> recomendación IA -> decisión final -> resultado.
// No conoce la UI: publica eventos en SimulationEvents (patrón Observer).
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] ApiClient api;
    [SerializeField] SimulationScreen simulationScreen;

    public FaseSimulacion Fase { get; private set; }
    public string DecisionInicial { get; private set; }
    public CaseModel CasoActual { get; private set; }

    string _sesionId;     // una sesión = todo el ejercicio (varios casos), se crea una sola vez
    int _i;               // índice de la pregunta actual
    bool _enviando;

    PreguntaModel Actual => CasoActual.Preguntas[_i];

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // ---------- Entradas públicas ----------
    // Recibe desde React el caso clínico seleccionado.
public void RecibirCasoDesdeReact(string casoId)
{
    if (string.IsNullOrWhiteSpace(casoId))
    {
        Debug.Log("[PRUEBA REACT] Valor recibido: [" + casoId + "]");
        Debug.LogWarning("[MammoSim] React no envió un caso válido.");
        return;
    }

    if (simulationScreen == null)
    {
        Debug.LogError("[MammoSim] SimulationScreen no está asignado.");
        return;
    }

    Debug.Log("[MammoSim] React seleccionó: " + casoId);

    simulationScreen.Abrir(casoId);
}
    // Lo llama el botón "Iniciar Simulación" (vía SimulationScreen.Abrir) o React con SendMessage.

public void IniciarCaso(string casoId)
{
    DecisionInicial = null;
    _i = 0;
    _enviando = false;
    CambiarFase(FaseSimulacion.Cargando);

    AsegurarSesion(() =>
        StartCoroutine(api.Get<CasoDto>($"/casos/{casoId}", dto =>
        {
            CasoActual = CaseDtoAdapter.Adaptar(dto);

            // Crear el historial del caso.
            if (HistorialManager.Instance != null)
            {
                HistorialManager.Instance.IniciarAnalisis(
                    CasoActual.Id,
                    dto.titulo
                );
            }
            else
            {
                Debug.LogWarning(
                    "[MammoSim] No se encontró HistorialManager."
                );
            }

            SimulationEvents.OnCasoSeleccionado?.Invoke(CasoActual);
            MostrarActual();

        }, Fallo)));
}

    // La vista llama aquí con la opción elegida (letra A-D en técnicas; biopsiar/seguimiento/... en decisiones).
    public void EnviarOpcion(string opcion)
    {
        if (_enviando) return;   // evita doble envío (la API además rechaza reescrituras)
        _enviando = true;

        switch (Fase)
        {
case FaseSimulacion.Pregunta:
    StartCoroutine(api.Post<RespuestaReq, RespuestaRes>(
        Ruta("respuestas"),
        new RespuestaReq
        {
            numero_pregunta = Actual.Numero,
            opcion = opcion
        },
        r =>
        {
            _enviando = false;

            // Registrar la respuesta en el historial.
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

            CambiarFase(FaseSimulacion.Retroalimentacion);
            SimulationEvents.OnRetroalimentacion?.Invoke(r);

        }, Fallo));
    break;
            case FaseSimulacion.DecisionInicial:
                StartCoroutine(api.Post<DecisionReq, RecomendacionRes>(
                    Ruta("decision-inicial"),
                    new DecisionReq { decision = opcion },
                    r =>
                    {
                        
_enviando = false;
DecisionInicial = opcion;

// Registrar la decisión inicial y la recomendación de IA.
if (HistorialManager.Instance != null)
{
    HistorialManager.Instance.RegistrarDecisionInicial(
        opcion,
        r.conducta
    );
}

SimulationEvents.OnDecisionRegistrada?.Invoke("inicial");

                        CambiarFase(FaseSimulacion.DecisionFinal);   // desde aquí la IA ya puede mostrarse
                        SimulationEvents.OnPreguntaMostrada?.Invoke(Actual, _i, CasoActual.Preguntas.Length);
                        SimulationEvents.OnRecomendacionRecibida?.Invoke(r);
                    }, Fallo));
                break;

            case FaseSimulacion.DecisionFinal:
    StartCoroutine(api.Post<DecisionReq, ResultadoCasoRes>(
        Ruta("decision-final"),
        new DecisionReq { decision = opcion },
        r =>
        {
            _enviando = false;

            // Registrar la decisión final y el resultado.
           
if (HistorialManager.Instance != null)
{
    // Registrar la decisión final y el resultado.
    HistorialManager.Instance.RegistrarResultadoFinal(
        opcion,
        r
    );

    // Guardar el análisis completo en el archivo JSON.
    HistorialManager.Instance.GuardarHistorial();
}


            SimulationEvents.OnDecisionRegistrada?.Invoke("final");
            CambiarFase(FaseSimulacion.Resultado);
            SimulationEvents.OnEvaluacionDisponible?.Invoke(r);

        }, Fallo));
    break;
        }
    }

    // "Siguiente" tras la retroalimentación de una pregunta técnica.
    public void Siguiente()
    {
        if (Fase != FaseSimulacion.Retroalimentacion) return;
        _i++;
        MostrarActual();
    }

    public void Terminar() => SimulationEvents.OnSimulacionFinalizada?.Invoke();

    // ---------- Internos ----------

    void AsegurarSesion(Action alTenerla)
    {
        if (!string.IsNullOrEmpty(_sesionId)) { alTenerla(); return; }

        StartCoroutine(api.Post<VacioReq, SesionRes>("/sesiones", new VacioReq(), s =>
        {
            _sesionId = s.sesion_id;
            alTenerla();
        }, Fallo));
    }

    string Ruta(string sufijo) => $"/sesiones/{_sesionId}/casos/{CasoActual.Id}/{sufijo}";

    void MostrarActual()
    {
        CambiarFase(Actual.EsDiagnostica ? FaseSimulacion.DecisionInicial : FaseSimulacion.Pregunta);
        SimulationEvents.OnPreguntaMostrada?.Invoke(Actual, _i, CasoActual.Preguntas.Length);
    }

    // Transiciones válidas (versión simplificada del patrón State).
    static bool Permitida(FaseSimulacion a, FaseSimulacion b) =>
        b == FaseSimulacion.Cargando || b == FaseSimulacion.Error ||
        (a, b) switch
        {
            (FaseSimulacion.Cargando, FaseSimulacion.Pregunta) => true,
            (FaseSimulacion.Cargando, FaseSimulacion.DecisionInicial) => true,
            (FaseSimulacion.Pregunta, FaseSimulacion.Retroalimentacion) => true,
            (FaseSimulacion.Retroalimentacion, FaseSimulacion.Pregunta) => true,
            (FaseSimulacion.Retroalimentacion, FaseSimulacion.DecisionInicial) => true,
            (FaseSimulacion.DecisionInicial, FaseSimulacion.DecisionFinal) => true,
            (FaseSimulacion.DecisionFinal, FaseSimulacion.Resultado) => true,
            _ => false
        };

    void CambiarFase(FaseSimulacion nueva)
    {
        if (!Permitida(Fase, nueva))
        {
            Debug.LogWarning($"[MammoSim] Transición no permitida: {Fase} -> {nueva}");
            return;
        }
        Fase = nueva;
        SimulationEvents.OnFase?.Invoke(nueva);
    }

    void Fallo(string msg)
    {
        _enviando = false;
        Debug.LogError("[MammoSim] " + msg);
        CambiarFase(FaseSimulacion.Error);
        SimulationEvents.OnError?.Invoke(msg);
    }
}

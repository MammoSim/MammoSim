using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Pruebas del flujo de GameManager:
//   sesión -> caso -> 7 técnicas -> decisión inicial -> IA -> decisión final -> resultado -> cerrar.
// Usan ApiClient en modo "datos de ejemplo" (MockApi), sin servidor. Necesitan PlayMode porque
// GameManager usa corrutinas y Awake. MockApi guarda la sesión en campos estáticos: se limpian
// antes y después de cada prueba.
public class GameManagerFlowTests
{
    GameObject _go;
    GameManager _gm;

    readonly List<FaseSimulacion> _fases = new List<FaseSimulacion>();
    int _recomendaciones;
    int _preguntasMostradas;
    PreguntaModel _ultimaPregunta;
    string _error;
    bool _finalizada;
    ResultadoCasoRes _evaluacion;

    System.Action<FaseSimulacion> _alFase;
    System.Action<RecomendacionRes> _alRecomendar;
    System.Action<PreguntaModel, int, int> _alMostrar;
    System.Action<string> _alError;
    System.Action<ResultadoCasoRes> _alEvaluar;
    System.Action _alFinalizar;

    static void ReiniciarMock()
    {
        const BindingFlags estatico = BindingFlags.NonPublic | BindingFlags.Static;
        foreach (var campo in new[] { "casoActual", "sesionId", "casosAsignados",
                                      "decisionInicial", "recomendacionIA", "decisionFinal" })
            typeof(MockApi).GetField(campo, estatico).SetValue(null, null);
        typeof(MockApi).GetField("sesionActiva", estatico).SetValue(null, false);
        ((Dictionary<int, bool>)typeof(MockApi).GetField("respuestas", estatico).GetValue(null)).Clear();
    }

    void CrearGameManager()
    {
        _go = new GameObject("GameManager_Test");
        var api = _go.AddComponent<ApiClient>();
        // Se fuerza el modo ejemplo para que ninguna prueba llame a un servidor real.
        typeof(ApiClient).GetField("usarDatosDeEjemplo", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(api, true);

        _gm = _go.AddComponent<GameManager>();
        typeof(GameManager).GetField("api", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_gm, api);
    }

    [SetUp]
    public void SetUp()
    {
        ReiniciarMock();
        _fases.Clear();
        _recomendaciones = 0;
        _preguntasMostradas = 0;
        _ultimaPregunta = null;
        _error = null;
        _finalizada = false;
        _evaluacion = null;

        CrearGameManager();

        _alFase = f => _fases.Add(f);
        _alRecomendar = r => _recomendaciones++;
        _alMostrar = (p, i, t) => { _preguntasMostradas++; _ultimaPregunta = p; };
        _alError = m => _error = m;
        _alEvaluar = e => _evaluacion = e;
        _alFinalizar = () => _finalizada = true;

        SimulationEvents.OnFase += _alFase;
        SimulationEvents.OnRecomendacionRecibida += _alRecomendar;
        SimulationEvents.OnPreguntaMostrada += _alMostrar;
        SimulationEvents.OnError += _alError;
        SimulationEvents.OnEvaluacionDisponible += _alEvaluar;
        SimulationEvents.OnSimulacionFinalizada += _alFinalizar;
    }

    [TearDown]
    public void TearDown()
    {
        SimulationEvents.OnFase -= _alFase;
        SimulationEvents.OnRecomendacionRecibida -= _alRecomendar;
        SimulationEvents.OnPreguntaMostrada -= _alMostrar;
        SimulationEvents.OnError -= _alError;
        SimulationEvents.OnEvaluacionDisponible -= _alEvaluar;
        SimulationEvents.OnSimulacionFinalizada -= _alFinalizar;
        Object.DestroyImmediate(_go);
        ReiniciarMock();
    }

    // Espera (máx. 5 s) a que el GameManager llegue a una fase.
    IEnumerator Esperar(FaseSimulacion fase)
    {
        float limite = Time.realtimeSinceStartup + 5f;
        while (_gm.Fase != fase && Time.realtimeSinceStartup < limite) yield return null;
        Assert.AreEqual(fase, _gm.Fase, "No se llegó a la fase esperada");
    }

    // Responde las preguntas técnicas hasta llegar a la decisión inicial.
    IEnumerator ResponderTecnicas()
    {
        yield return Esperar(FaseSimulacion.Pregunta);
        while (_gm.Fase == FaseSimulacion.Pregunta)
        {
            _gm.EnviarOpcion("A");
            yield return Esperar(FaseSimulacion.Retroalimentacion);
            _gm.Siguiente();
            yield return null;
        }
    }

    // Recorre un caso completo con la misma decisión inicial y final.
    IEnumerator LlegarAlResultado(string decision = "biopsiar")
    {
        _gm.IniciarCaso("caso_001");
        yield return ResponderTecnicas();

        _gm.EnviarOpcion(decision);
        yield return Esperar(FaseSimulacion.DecisionFinal);

        _gm.EnviarOpcion(decision);
        yield return Esperar(FaseSimulacion.Resultado);
    }

    [UnityTest]
    public IEnumerator Flujo_completo_recorre_las_fases_en_orden()
    {
        _gm.IniciarCaso("caso_001");
        yield return ResponderTecnicas();

        Assert.AreEqual(FaseSimulacion.DecisionInicial, _gm.Fase);
        Assert.AreEqual(0, _recomendaciones, "La IA no debe recomendar antes de la decisión inicial");

        _gm.EnviarOpcion("biopsiar");
        yield return Esperar(FaseSimulacion.DecisionFinal);
        Assert.AreEqual("biopsiar", _gm.DecisionInicial);
        Assert.AreEqual(1, _recomendaciones);

        _gm.EnviarOpcion("biopsiar");
        yield return Esperar(FaseSimulacion.Resultado);

        Assert.AreEqual(FaseSimulacion.Cargando, _fases.First());
        Assert.AreEqual(FaseSimulacion.Resultado, _fases.Last());
        Assert.AreEqual(7, _fases.Count(f => f == FaseSimulacion.Retroalimentacion));
        Assert.AreEqual(1, _fases.Count(f => f == FaseSimulacion.DecisionInicial));
        Assert.AreEqual(1, _fases.Count(f => f == FaseSimulacion.DecisionFinal));
        Assert.IsFalse(_fases.Contains(FaseSimulacion.Error));
        Assert.IsNull(_error);
    }

    [UnityTest]
    public IEnumerator El_resultado_llega_a_la_interfaz_con_hallazgos_y_retroalimentacion()
    {
        yield return LlegarAlResultado("biopsiar");

        Assert.IsNotNull(_evaluacion);
        Assert.AreEqual(_evaluacion.acierto_inicial, _evaluacion.acierto_final,
            "Misma decisión al inicio y al final: ambos aciertos deben coincidir");
        Assert.IsNotEmpty(_evaluacion.hallazgos);
        Assert.IsNotEmpty(_evaluacion.retroalimentacion);
    }

    [UnityTest]
    public IEnumerator Un_doble_envio_se_ignora()
    {
        _gm.IniciarCaso("caso_001");
        yield return Esperar(FaseSimulacion.Pregunta);

        _gm.EnviarOpcion("A");
        _gm.EnviarOpcion("B");   // llega mientras se espera la respuesta: debe descartarse
        yield return Esperar(FaseSimulacion.Retroalimentacion);
        yield return null;
        yield return null;

        Assert.AreEqual(1, _fases.Count(f => f == FaseSimulacion.Retroalimentacion));
    }

    [UnityTest]
    public IEnumerator Una_opcion_vacia_se_ignora()
    {
        _gm.IniciarCaso("caso_001");
        yield return Esperar(FaseSimulacion.Pregunta);

        _gm.EnviarOpcion("");
        _gm.EnviarOpcion("   ");
        _gm.EnviarOpcion(null);
        yield return null;
        yield return null;

        Assert.AreEqual(FaseSimulacion.Pregunta, _gm.Fase);
        Assert.AreEqual(0, _fases.Count(f => f == FaseSimulacion.Retroalimentacion));
    }

    [Test]
    public void Enviar_una_opcion_antes_de_cargar_el_caso_no_hace_nada()
    {
        Assert.DoesNotThrow(() => _gm.EnviarOpcion("A"));
        Assert.IsEmpty(_fases);
        Assert.IsNull(_error);
    }

    [UnityTest]
    public IEnumerator Siguiente_fuera_de_la_retroalimentacion_no_hace_nada()
    {
        _gm.IniciarCaso("caso_001");
        yield return Esperar(FaseSimulacion.Pregunta);
        int antes = _preguntasMostradas;

        _gm.Siguiente();

        Assert.AreEqual(FaseSimulacion.Pregunta, _gm.Fase);
        Assert.AreEqual(antes, _preguntasMostradas);
    }

    [UnityTest]
    public IEnumerator Un_caso_inexistente_lleva_a_la_fase_Error()
    {
        LogAssert.Expect(LogType.Error, "[MammoSim] Sin datos para esta ruta");

        _gm.IniciarCaso("caso_999");
        yield return Esperar(FaseSimulacion.Error);

        Assert.AreEqual("Sin datos para esta ruta", _error);
    }

    [UnityTest]
    public IEnumerator Un_caso_que_no_es_de_la_sesion_actual_lleva_a_la_fase_Error()
    {
        _gm.IniciarCaso("caso_001");
        yield return Esperar(FaseSimulacion.Pregunta);

        LogAssert.Expect(LogType.Error, "[MammoSim] El caso seleccionado no pertenece a la sesión actual.");
        _gm.IniciarCaso("caso_002");

        Assert.AreEqual(FaseSimulacion.Error, _gm.Fase);
        Assert.AreEqual("El caso seleccionado no pertenece a la sesión actual.", _error);
    }

    // Antes (versión anterior de GameManager) esto bloqueaba todos los envíos siguientes porque
    // _enviando quedaba en true. En SimulacionUnity ya no ocurre: se deja como prueba de regresión.
    [UnityTest]
    public IEnumerator Enviar_en_una_fase_incorrecta_no_bloquea_los_envios_siguientes()
    {
        _gm.IniciarCaso("caso_001");
        yield return Esperar(FaseSimulacion.Pregunta);

        _gm.EnviarOpcion("A");
        yield return Esperar(FaseSimulacion.Retroalimentacion);

        _gm.EnviarOpcion("A");   // fase incorrecta: no debe tener efecto
        _gm.Siguiente();
        yield return Esperar(FaseSimulacion.Pregunta);

        _gm.EnviarOpcion("A");   // debe seguir funcionando
        yield return Esperar(FaseSimulacion.Retroalimentacion);
    }

    [UnityTest]
    public IEnumerator Terminar_cierra_la_sesion_y_avisa_que_la_simulacion_finalizo()
    {
        yield return LlegarAlResultado();

        _gm.Terminar();
        float limite = Time.realtimeSinceStartup + 5f;
        while (!_finalizada && Time.realtimeSinceStartup < limite) yield return null;

        Assert.IsTrue(_finalizada, "No se avisó que la simulación finalizó");
        Assert.IsNull(MockApi.Responder("/sesiones/activa", null), "La sesión debe quedar cerrada");
    }

    [UnityTest]
    public IEnumerator Terminar_fuera_del_resultado_no_hace_nada()
    {
        _gm.IniciarCaso("caso_001");
        yield return Esperar(FaseSimulacion.Pregunta);

        _gm.Terminar();
        yield return null;
        yield return null;

        Assert.IsFalse(_finalizada);
        Assert.AreEqual(FaseSimulacion.Pregunta, _gm.Fase);
    }

    // ---- PENDIENTE DE INTEGRACIÓN CON EL BACKEND ----
    // Si ya hay una sesión activa (por ejemplo, el estudiante recarga la página), GameManager consulta
    // GET .../progreso y retoma en la siguiente pregunta. MockApi no implementa esa ruta (responde
    // "Ruta no reconocida" y GameManager termina en Error). Se quita el [Ignore] cuando el mock la
    // implemente o cuando las pruebas se ejecuten contra el backend.
    [UnityTest]
    [Ignore("Pendiente de integración con el backend: MockApi no implementa GET .../progreso")]
    public IEnumerator Recuperar_una_sesion_activa_continua_en_la_siguiente_pregunta()
    {
        _gm.IniciarCaso("caso_001");
        yield return Esperar(FaseSimulacion.Pregunta);
        _gm.EnviarOpcion("A");
        yield return Esperar(FaseSimulacion.Retroalimentacion);

        // GameManager nuevo (como al recargar la página); la sesión sigue activa en el servidor.
        Object.DestroyImmediate(_go);
        CrearGameManager();
        _ultimaPregunta = null;
        _gm.IniciarCaso("caso_001");
        yield return Esperar(FaseSimulacion.Pregunta);

        Assert.AreEqual(2, _ultimaPregunta.Numero, "Debe retomar en la pregunta 2");
    }
}

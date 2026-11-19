using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Pruebas del flujo de GameManager: 7 técnicas -> decisión inicial -> IA -> decisión final -> resultado.
// Usan ApiClient en modo "datos de ejemplo" (MockApi), sin servidor. Necesitan PlayMode porque
// GameManager usa corrutinas y Awake.
public class GameManagerFlowTests
{
    GameObject _go;
    GameManager _gm;

    readonly List<FaseSimulacion> _fases = new List<FaseSimulacion>();
    int _recomendaciones;
    int _preguntasMostradas;
    string _error;

    System.Action<FaseSimulacion> _alFase;
    System.Action<RecomendacionRes> _alRecomendar;
    System.Action<PreguntaModel, int, int> _alMostrar;
    System.Action<string> _alError;

    [SetUp]
    public void SetUp()
    {
        _fases.Clear();
        _recomendaciones = 0;
        _preguntasMostradas = 0;
        _error = null;

        _go = new GameObject("GameManager_Test");
        var api = _go.AddComponent<ApiClient>();
        // Se fuerza el modo ejemplo para que ninguna prueba llame a un servidor real.
        typeof(ApiClient).GetField("usarDatosDeEjemplo", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(api, true);

        _gm = _go.AddComponent<GameManager>();
        typeof(GameManager).GetField("api", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_gm, api);

        _alFase = f => _fases.Add(f);
        _alRecomendar = r => _recomendaciones++;
        _alMostrar = (p, i, t) => _preguntasMostradas++;
        _alError = m => _error = m;

        SimulationEvents.OnFase += _alFase;
        SimulationEvents.OnRecomendacionRecibida += _alRecomendar;
        SimulationEvents.OnPreguntaMostrada += _alMostrar;
        SimulationEvents.OnError += _alError;
    }

    [TearDown]
    public void TearDown()
    {
        SimulationEvents.OnFase -= _alFase;
        SimulationEvents.OnRecomendacionRecibida -= _alRecomendar;
        SimulationEvents.OnPreguntaMostrada -= _alMostrar;
        SimulationEvents.OnError -= _alError;
        Object.DestroyImmediate(_go);
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
        Assert.IsFalse(_fases.Contains(FaseSimulacion.Error));
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

    // ---- HOY FALLA ----
    // EnviarOpcion pone _enviando = true y solo lo libera dentro de los 3 casos del switch.
    // Si se llama en otra fase (p. ej. Retroalimentacion) la bandera queda en true y se ignoran
    // todos los envíos siguientes hasta empezar otro caso.
    [UnityTest]
    public IEnumerator Enviar_en_una_fase_incorrecta_no_bloquea_los_envios_siguientes()
    {
        _gm.IniciarCaso("caso_001");
        yield return Esperar(FaseSimulacion.Pregunta);

        _gm.EnviarOpcion("A");
        yield return Esperar(FaseSimulacion.Retroalimentacion);

        _gm.EnviarOpcion("A");   // fase incorrecta: no debería tener efecto
        _gm.Siguiente();
        yield return Esperar(FaseSimulacion.Pregunta);

        _gm.EnviarOpcion("A");   // debe seguir funcionando
        yield return Esperar(FaseSimulacion.Retroalimentacion);
    }
}

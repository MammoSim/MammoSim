using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Pruebas de ApiClient en modo "datos de ejemplo" (usa MockApi, sin red).
// El modo real (UnityWebRequest: encabezado Authorization, 401 -> OnTokenExpired, 404, 409, timeout)
// no se puede probar aquí sin un servidor o sin aislar UnityWebRequest detrás de una interfaz.
public class ApiClientTests
{
    GameObject _go;
    ApiClient _api;

    // MockApi guarda la sesión en campos estáticos: se limpian para que las pruebas no dependan del orden.
    static void ReiniciarMock()
    {
        const BindingFlags estatico = BindingFlags.NonPublic | BindingFlags.Static;
        foreach (var campo in new[] { "casoActual", "sesionId", "casosAsignados",
                                      "decisionInicial", "recomendacionIA", "decisionFinal" })
            typeof(MockApi).GetField(campo, estatico).SetValue(null, null);
        typeof(MockApi).GetField("sesionActiva", estatico).SetValue(null, false);
        ((Dictionary<int, bool>)typeof(MockApi).GetField("respuestas", estatico).GetValue(null)).Clear();
    }

    [SetUp]
    public void SetUp()
    {
        ReiniciarMock();
        _go = new GameObject("ApiClient_Test");
        _api = _go.AddComponent<ApiClient>();
        typeof(ApiClient).GetField("usarDatosDeEjemplo", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_api, true);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        ReiniciarMock();
    }

    [UnityTest]
    public IEnumerator Get_entrega_el_caso_pedido()
    {
        CasoDto caso = null;
        string error = null;

        yield return _api.Get<CasoDto>("/casos/caso_001", c => caso = c, e => error = e);

        Assert.IsNull(error);
        Assert.IsNotNull(caso);
        Assert.AreEqual("caso_001", caso.id_caso);
    }

    [UnityTest]
    public IEnumerator Get_de_una_ruta_sin_datos_llama_a_Fail()
    {
        CasoDto caso = null;
        string error = null;

        yield return _api.Get<CasoDto>("/casos/caso_999", c => caso = c, e => error = e);

        Assert.IsNull(caso);
        Assert.AreEqual("Sin datos para esta ruta", error);
    }

    [UnityTest]
    public IEnumerator Post_de_sesiones_entrega_la_sesion()
    {
        SesionRes sesion = null;
        string error = null;
        var pedido = new CrearSesionReq { id_curso = null, casos = new[] { "caso_001" } };

        yield return _api.Post<CrearSesionReq, SesionRes>("/sesiones", pedido, s => sesion = s, e => error = e);

        Assert.IsNull(error);
        Assert.AreEqual("ses_prueba_001", sesion.id_sesion);
    }

    [UnityTest]
    public IEnumerator Post_de_sesiones_sin_casos_llama_a_Fail()
    {
        SesionRes sesion = null;
        string error = null;
        LogAssert.Expect(LogType.Error, "[MockApi] La sesion necesita casos.");

        yield return _api.Post<CrearSesionReq, SesionRes>(
            "/sesiones", new CrearSesionReq { casos = new string[0] }, s => sesion = s, e => error = e);

        Assert.IsNull(sesion);
        Assert.AreEqual("Sin datos para esta ruta", error);
    }

    // GameManager usa este "Sin datos" para saber que no hay sesión y crear una nueva.
    [UnityTest]
    public IEnumerator Get_de_sesion_activa_sin_sesion_llama_a_Fail()
    {
        SesionActivaRes activa = null;
        string error = null;

        yield return _api.Get<SesionActivaRes>("/sesiones/activa", a => activa = a, e => error = e);

        Assert.IsNull(activa);
        Assert.AreEqual("Sin datos para esta ruta", error);
    }

    [UnityTest]
    public IEnumerator Get_de_sesion_activa_con_sesion_entrega_los_datos()
    {
        SesionActivaRes activa = null;
        string error = null;
        var pedido = new CrearSesionReq { id_curso = null, casos = new[] { "caso_001" } };

        yield return _api.Post<CrearSesionReq, SesionRes>("/sesiones", pedido, s => { }, e => error = e);
        yield return _api.Get<SesionActivaRes>("/sesiones/activa", a => activa = a, e => error = e);

        Assert.IsNull(error);
        Assert.AreEqual("ses_prueba_001", activa.id_sesion);
        Assert.AreEqual("caso_001", activa.caso_actual);
    }

    [Test]
    public void SetToken_acepta_un_token_sin_lanzar_excepcion()
    {
        Assert.DoesNotThrow(() => _api.SetToken("token-de-prueba"));
    }
}

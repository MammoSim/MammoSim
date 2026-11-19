using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Pruebas de ApiClient en modo "datos de ejemplo" (usa MockApi, sin red).
// El modo real (UnityWebRequest: encabezado Authorization, 401 -> OnTokenExpired, 409, timeout)
// no se puede probar aquí sin un servidor o sin aislar UnityWebRequest detrás de una interfaz.
public class ApiClientTests
{
    GameObject _go;
    ApiClient _api;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("ApiClient_Test");
        _api = _go.AddComponent<ApiClient>();
        typeof(ApiClient).GetField("usarDatosDeEjemplo", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_api, true);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_go);

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
    public IEnumerator Get_de_una_ruta_sin_datos_llama_a_fail()
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

        yield return _api.Post<VacioReq, SesionRes>("/sesiones", new VacioReq(), s => sesion = s, e => error = e);

        Assert.IsNull(error);
        Assert.AreEqual("ses_prueba_001", sesion.sesion_id);
    }

    [Test]
    public void SetToken_acepta_un_token_sin_lanzar_excepcion()
    {
        Assert.DoesNotThrow(() => _api.SetToken("token-de-prueba"));
    }
}

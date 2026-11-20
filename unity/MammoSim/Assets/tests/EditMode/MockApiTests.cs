using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Pruebas unitarias de MockApi (API simulada que lee Assets/Resources/Casos/casos_mammosim.json).
// OJO: en EditMode un Debug.LogError hace fallar la prueba si no se declara con LogAssert.Expect.
public class MockApiTests
{
    const string RutaCaso = "/sesiones/ses_prueba_001/casos/caso_001/";
    static readonly string[] Decisiones = { "biopsiar", "seguimiento", "estudio_adicional", "ignorar" };

    // MockApi guarda el caso actual en campos estáticos: se limpian antes de cada prueba.
    [SetUp]
    public void Reiniciar()
    {
        foreach (var campo in new[] { "casoActual", "decisionInicial", "recomendacionIA", "decisionFinal" })
            typeof(MockApi).GetField(campo, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
    }

    // ---------- Ayudas ----------
    static string Json<T>(T objeto) => JsonUtility.ToJson(objeto);

    static CasoDto CargarCaso(string id = "caso_001") =>
        JsonUtility.FromJson<CasoDto>(MockApi.Responder("/casos/" + id, null));

    static RespuestaRes Responder(int numero, string opcion) =>
        JsonUtility.FromJson<RespuestaRes>(MockApi.Responder(
            RutaCaso + "respuestas", Json(new RespuestaReq { numero_pregunta = numero, opcion = opcion })));

    static RecomendacionRes PostDecisionInicial(string decision) =>
        JsonUtility.FromJson<RecomendacionRes>(MockApi.Responder(
            RutaCaso + "decision-inicial", Json(new DecisionReq { decision = decision })));

    static ResultadoCasoRes PostDecisionFinal(string decision) =>
        JsonUtility.FromJson<ResultadoCasoRes>(MockApi.Responder(
            RutaCaso + "decision-final", Json(new DecisionReq { decision = decision })));

    // ---------- Sesión y caso ----------
    [Test]
    public void Crear_sesion_devuelve_la_sesion_de_prueba()
    {
        var s = JsonUtility.FromJson<SesionRes>(MockApi.Responder("/sesiones", "{}"));

        Assert.AreEqual("ses_prueba_001", s.sesion_id);
        CollectionAssert.Contains(s.casos_asignados, "caso_001");
    }

    [Test]
    public void Caso_existente_devuelve_el_caso_pedido()
    {
        var caso = CargarCaso("caso_001");

        Assert.AreEqual("caso_001", caso.id_caso);
        Assert.IsNotEmpty(caso.preguntas);
    }

    [Test]
    public void El_caso_enviado_a_la_interfaz_no_revela_respuestas()
    {
        string json = MockApi.Responder("/casos/caso_001", null);

        // Se busca el NOMBRE DEL CAMPO (entre comillas y con dos puntos), porque
        // palabras como "hallazgos" sí aparecen en el texto de las preguntas.
        foreach (var prohibido in new[]
                 { "respuesta_correcta", "explicacion", "hallazgos", "retroalimentacion",
                   "opcion_ia_incorrecta", "recomendacion_ia" })
        {
            string campo = (char)34 + prohibido + (char)34 + ":";
            Assert.That(json, Does.Not.Contain(campo), "No debe viajar el campo: " + prohibido);
        }
    }

    [Test]
    public void Caso_inexistente_devuelve_null_y_avisa()
    {
        LogAssert.Expect(LogType.Warning, "MammoSim: No se encontró el caso caso_999");
        Assert.IsNull(MockApi.Responder("/casos/caso_999", null));
    }

    [Test]
    public void Responder_sin_cargar_un_caso_devuelve_null_y_registra_error()
    {
        LogAssert.Expect(LogType.Error, "MammoSim: Debes cargar un caso antes de responder.");
        Assert.IsNull(MockApi.Responder(RutaCaso + "respuestas", "{}"));
    }

    [Test]
    public void Ruta_desconocida_devuelve_null_y_avisa()
    {
        CargarCaso();
        LogAssert.Expect(LogType.Warning, "MammoSim: Ruta no reconocida: /algo-raro");
        Assert.IsNull(MockApi.Responder("/algo-raro", null));
    }

    // ---------- Preguntas técnicas ----------
    [Test]
    public void Cada_pregunta_tecnica_tiene_una_sola_opcion_correcta()
    {
        CargarCaso();
        int aciertos = 0;

        foreach (var opcion in new[] { "A", "B", "C", "D" })
        {
            var r = Responder(1, opcion);
            if (r.correcta)
            {
                aciertos++;
                Assert.AreEqual(opcion, r.opcion_correcta);
            }
            else
            {
                Assert.AreNotEqual(opcion, r.opcion_correcta);
            }
            Assert.IsNotEmpty(r.explicacion);
        }

        Assert.AreEqual(1, aciertos);
    }

    [Test]
    public void Pregunta_inexistente_devuelve_null_y_avisa()
    {
        CargarCaso();
        LogAssert.Expect(LogType.Warning, "MammoSim: Pregunta no encontrada: 99");
        Assert.IsNull(MockApi.Responder(RutaCaso + "respuestas",
            Json(new RespuestaReq { numero_pregunta = 99, opcion = "A" })));
    }

    // ---------- Decisiones e IA ----------
    [Test]
    public void La_recomendacion_trae_conducta_y_texto()
    {
        CargarCaso();
        var rec = PostDecisionInicial("biopsiar");

        Assert.IsNotEmpty(rec.conducta);
        Assert.IsNotEmpty(rec.texto);
    }

    [Test]
    public void Solo_una_decision_acierta_y_el_acierto_inicial_y_final_coinciden()
    {
        int aciertos = 0;
        foreach (var d in Decisiones)
        {
            CargarCaso();
            PostDecisionInicial(d);
            var r = PostDecisionFinal(d);

            Assert.AreEqual(r.acierto_inicial, r.acierto_final);
            if (r.acierto_final) aciertos++;
        }
        Assert.AreEqual(1, aciertos);
    }

    [Test]
    public void Solo_una_decision_final_coincide_con_la_recomendacion_de_la_IA()
    {
        int siguieron = 0;
        foreach (var d in Decisiones)
        {
            CargarCaso();
            PostDecisionInicial(d);
            if (PostDecisionFinal(d).siguio_ia) siguieron++;
        }
        Assert.AreEqual(1, siguieron);
    }

    [Test]
    public void El_resultado_trae_hallazgos_y_retroalimentacion()
    {
        CargarCaso();
        PostDecisionInicial("biopsiar");
        var r = PostDecisionFinal("biopsiar");

        Assert.IsNotEmpty(r.hallazgos);
        Assert.IsNotEmpty(r.retroalimentacion);
    }

    // ---- PENDIENTE DE INTEGRACIÓN CON EL BACKEND ----
    // Regla del tutor en el backend (services/tutor.py): si la decisión inicial fue correcta, la IA
    // defiende una opción incorrecta; si fue incorrecta, recomienda la correcta (dos textos distintos).
    // MockApi siempre devuelve la recomendación incorrecta fija ("Para las pruebas..."), a propósito.
    // Se quita el [Ignore] cuando el mock deba imitar la regla real o cuando se conecte el backend.
    [Test]
    [Ignore("Pendiente de integración con el backend: el mock usa siempre la recomendación incorrecta fija")]
    public void La_IA_del_mock_sigue_la_regla_del_tutor_del_backend()
    {
        int textosDistintos = Decisiones
            .Select(d => { CargarCaso(); return PostDecisionInicial(d).texto; })
            .Distinct()
            .Count();

        Assert.AreEqual(2, textosDistintos);
    }
}

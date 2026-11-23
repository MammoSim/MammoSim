using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Pruebas unitarias de MockApi (API simulada que lee Assets/Resources/Casos/casos_mammosim.json).
// MockApi imita el flujo del backend: sesión -> caso -> 7 respuestas -> decisión inicial -> IA ->
// decisión final -> retroalimentación -> cerrar. Guarda su estado en campos estáticos, por eso se
// reinicia antes y después de cada prueba.
// OJO: en EditMode un Debug.LogError hace fallar la prueba si no se declara con LogAssert.Expect.
public class MockApiTests
{
    const string RutaCaso = "/sesiones/ses_prueba_001/casos/caso_001/";
    const string RutaCerrar = "/sesiones/ses_prueba_001/cerrar";
    const BindingFlags Estatico = BindingFlags.NonPublic | BindingFlags.Static;
    static readonly string[] Decisiones = { "biopsiar", "seguimiento", "estudio_adicional", "ignorar" };

    // ---------- Estado del mock ----------
    static void PonerCampo(string nombre, object valor) =>
        typeof(MockApi).GetField(nombre, Estatico).SetValue(null, valor);

    [SetUp]
    public static void Reiniciar()
    {
        foreach (var campo in new[] { "casoActual", "sesionId", "casosAsignados",
                                      "decisionInicial", "recomendacionIA", "decisionFinal" })
            PonerCampo(campo, null);
        PonerCampo("sesionActiva", false);
        ((Dictionary<int, bool>)typeof(MockApi).GetField("respuestas", Estatico).GetValue(null)).Clear();
    }

    [TearDown]
    public void Limpiar() => Reiniciar();

    // ---------- Ayudas ----------
    static string Json<T>(T objeto) => JsonUtility.ToJson(objeto);

    static SesionRes CrearSesion() =>
        JsonUtility.FromJson<SesionRes>(MockApi.Responder("/sesiones",
            Json(new CrearSesionReq { id_curso = null, casos = new[] { "caso_001" } })));

    static CasoDto CargarCaso(string id = "caso_001") =>
        JsonUtility.FromJson<CasoDto>(MockApi.Responder("/casos/" + id, null));

    // Deja el mock con una sesión activa y el caso_001 cargado.
    static void Preparar()
    {
        Reiniciar();
        CrearSesion();
        CargarCaso();
    }

    static RespuestaRes Responder(int numero, string opcion) =>
        JsonUtility.FromJson<RespuestaRes>(MockApi.Responder(RutaCaso + "respuestas",
            Json(new RespuestaReq { numero_pregunta = numero, opcion_elegida = opcion })));

    // Responde las 7 técnicas con la misma opción y devuelve cuántas acertó.
    static int ResponderLas7(string opcion = "A")
    {
        int aciertos = 0;
        for (int n = 1; n <= 7; n++)
            if (Responder(n, opcion).correcta) aciertos++;
        return aciertos;
    }

    static GuardadaRes Decidir(string ruta, string decision) =>
        JsonUtility.FromJson<GuardadaRes>(MockApi.Responder(RutaCaso + ruta,
            Json(new DecisionReq { decision = decision })));

    static RecomendacionRes Recomendacion() =>
        JsonUtility.FromJson<RecomendacionRes>(MockApi.Responder(RutaCaso + "recomendacion-ia", null));

    static RetroalimentacionRes Retroalimentacion() =>
        JsonUtility.FromJson<RetroalimentacionRes>(MockApi.Responder(RutaCaso + "retroalimentacion", null));

    // Caso completo con la misma decisión inicial y final.
    static void FlujoCompleto(string decision)
    {
        Preparar();
        ResponderLas7();
        Decidir("decision-inicial", decision);
        Decidir("decision-final", decision);
    }

    // ---------- Sesión ----------
    [Test]
    public void Crear_sesion_devuelve_la_sesion_de_prueba()
    {
        var s = CrearSesion();

        Assert.AreEqual("ses_prueba_001", s.id_sesion);
        CollectionAssert.Contains(s.casos_asignados, "caso_001");
        Assert.AreEqual("en_curso", s.estado);
    }

    [Test]
    public void Crear_sesion_sin_casos_devuelve_null_y_registra_error()
    {
        LogAssert.Expect(LogType.Error, "[MockApi] La sesion necesita casos.");
        Assert.IsNull(MockApi.Responder("/sesiones", Json(new CrearSesionReq { casos = new string[0] })));
    }

    [Test]
    public void Crear_una_segunda_sesion_con_una_activa_devuelve_null_y_avisa()
    {
        CrearSesion();
        LogAssert.Expect(LogType.Warning, "[MockApi] Ya existe una sesion activa.");

        Assert.IsNull(MockApi.Responder("/sesiones",
            Json(new CrearSesionReq { casos = new[] { "caso_001" } })));
    }

    [Test]
    public void Sesion_activa_sin_sesion_devuelve_null()
    {
        Assert.IsNull(MockApi.Responder("/sesiones/activa", null));
    }

    [Test]
    public void Sesion_activa_devuelve_la_sesion_y_su_caso()
    {
        CrearSesion();
        var a = JsonUtility.FromJson<SesionActivaRes>(MockApi.Responder("/sesiones/activa", null));

        Assert.AreEqual("ses_prueba_001", a.id_sesion);
        Assert.AreEqual("caso_001", a.caso_actual);
        Assert.IsEmpty(a.casos_terminados);
        Assert.AreEqual("en_curso", a.estado);
    }

    // ---------- Caso y rutas ----------
    [Test]
    public void Caso_existente_devuelve_el_caso_pedido()
    {
        var caso = CargarCaso("caso_001");

        Assert.AreEqual("caso_001", caso.id_caso);
        Assert.IsNotEmpty(caso.preguntas);
    }

    [Test]
    public void Caso_inexistente_devuelve_null_y_avisa()
    {
        LogAssert.Expect(LogType.Warning, "[MockApi] Caso no encontrado: caso_999");
        Assert.IsNull(MockApi.Responder("/casos/caso_999", null));
    }

    [Test]
    public void Responder_sin_sesion_ni_caso_devuelve_null_y_registra_error()
    {
        LogAssert.Expect(LogType.Error, "[MockApi] No existe un caso activo.");
        Assert.IsNull(MockApi.Responder(RutaCaso + "respuestas", "{}"));
    }

    [Test]
    public void Ruta_desconocida_devuelve_null_y_avisa()
    {
        Preparar();
        LogAssert.Expect(LogType.Warning, "[MockApi] Ruta no reconocida: /algo-raro");
        Assert.IsNull(MockApi.Responder("/algo-raro", null));
    }

    // ---------- Preguntas técnicas ----------
    [Test]
    public void Pregunta_inexistente_devuelve_null()
    {
        Preparar();
        Assert.IsNull(MockApi.Responder(RutaCaso + "respuestas",
            Json(new RespuestaReq { numero_pregunta = 99, opcion_elegida = "A" })));
    }

    [Test]
    public void Pregunta_ya_respondida_devuelve_null_y_avisa()
    {
        Preparar();
        Responder(1, "A");
        LogAssert.Expect(LogType.Warning, "[MockApi] Pregunta ya respondida.");

        Assert.IsNull(MockApi.Responder(RutaCaso + "respuestas",
            Json(new RespuestaReq { numero_pregunta = 1, opcion_elegida = "B" })));
    }

    [Test]
    public void Pregunta_diagnostica_no_se_responde_como_tecnica()
    {
        Preparar();
        Assert.IsNull(MockApi.Responder(RutaCaso + "respuestas",
            Json(new RespuestaReq { numero_pregunta = 8, opcion_elegida = "biopsiar" })));
    }

    [Test]
    public void Cada_pregunta_tecnica_tiene_una_sola_opcion_correcta()
    {
        int aciertos = 0;

        foreach (var opcion in new[] { "A", "B", "C", "D" })
        {
            Preparar();   // el mock no deja responder dos veces la misma pregunta
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
            Assert.AreEqual(r.respuesta_correcta, r.opcion_correcta);
            Assert.IsNotEmpty(r.explicacion);
        }

        Assert.AreEqual(1, aciertos);
    }

    [Test]
    public void La_respuesta_trae_el_texto_correcto_y_un_mensaje_segun_el_acierto()
    {
        Preparar();
        string correcta = Responder(1, "A").respuesta_correcta;

        Preparar();
        var acierto = Responder(1, correcta);
        Assert.IsTrue(acierto.correcta);
        StringAssert.StartsWith("Correcto.", acierto.mensaje);

        Preparar();
        var fallo = Responder(1, correcta == "A" ? "B" : "A");
        Assert.IsFalse(fallo.correcta);
        StringAssert.StartsWith("Incorrecto.", fallo.mensaje);
        Assert.IsNotEmpty(fallo.texto_correcto);
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

    // ---------- Decisiones e IA ----------
    [Test]
    public void La_decision_inicial_exige_las_7_respuestas()
    {
        Preparar();
        LogAssert.Expect(LogType.Warning, "[MockApi] No se puede guardar la decision inicial.");

        Assert.IsNull(MockApi.Responder(RutaCaso + "decision-inicial",
            Json(new DecisionReq { decision = "biopsiar" })));
    }

    [Test]
    public void La_decision_final_exige_la_decision_inicial()
    {
        Preparar();
        ResponderLas7();
        LogAssert.Expect(LogType.Warning, "[MockApi] No se puede guardar la decision final.");

        Assert.IsNull(MockApi.Responder(RutaCaso + "decision-final",
            Json(new DecisionReq { decision = "biopsiar" })));
    }

    [Test]
    public void La_recomendacion_trae_opcion_conducta_y_texto()
    {
        Preparar();
        ResponderLas7();
        Assert.IsNull(MockApi.Responder(RutaCaso + "recomendacion-ia", null),
            "Sin decisión inicial no hay recomendación");

        Assert.IsTrue(Decidir("decision-inicial", "biopsiar").guardada);
        var rec = Recomendacion();

        CollectionAssert.Contains(Decisiones, rec.opcion_ia);
        Assert.AreEqual(rec.opcion_ia, rec.conducta);
        Assert.IsNotEmpty(rec.texto);
    }

    [Test]
    public void Solo_una_decision_acierta()
    {
        int aciertos = 0;
        foreach (var d in Decisiones)
        {
            FlujoCompleto(d);
            var retro = Retroalimentacion();

            CollectionAssert.Contains(Decisiones, retro.respuesta_correcta);
            if (d == retro.respuesta_correcta) aciertos++;
        }
        Assert.AreEqual(1, aciertos);
    }

    [Test]
    public void Solo_una_decision_final_coincide_con_la_recomendacion_de_la_IA()
    {
        int siguieron = 0;
        foreach (var d in Decisiones)
        {
            FlujoCompleto(d);
            if (Retroalimentacion().siguio_ia) siguieron++;
        }
        Assert.AreEqual(1, siguieron);
    }

    [Test]
    public void La_recomendacion_del_mock_siempre_es_incorrecta()
    {
        foreach (var d in Decisiones)
        {
            FlujoCompleto(d);
            Assert.IsFalse(Retroalimentacion().ia_correcta, "Decisión: " + d);
        }
    }

    // ---------- Resultado y cierre ----------
    [Test]
    public void La_retroalimentacion_trae_hallazgos_y_resultado_de_las_7_preguntas()
    {
        FlujoCompleto("biopsiar");
        var r = Retroalimentacion();

        Assert.IsNotEmpty(r.hallazgos);
        Assert.IsNotEmpty(r.retroalimentacion);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7 }, r.preguntas.Select(p => p.numero).ToArray());
        Assert.GreaterOrEqual(r.nota_caso, 0f);
        Assert.LessOrEqual(r.nota_caso, 10f);
    }

    [Test]
    public void La_retroalimentacion_antes_de_la_decision_final_devuelve_null()
    {
        Preparar();
        ResponderLas7();
        Decidir("decision-inicial", "biopsiar");

        Assert.IsNull(MockApi.Responder(RutaCaso + "retroalimentacion", null));
    }

    [Test]
    public void Cerrar_antes_de_terminar_el_caso_devuelve_null_y_avisa()
    {
        Preparar();
        LogAssert.Expect(LogType.Warning, "[MockApi] El caso aun no ha terminado.");

        Assert.IsNull(MockApi.Responder(RutaCerrar, Json(new VacioReq())));
    }

    [Test]
    public void Cerrar_devuelve_la_nota_y_deja_la_sesion_inactiva()
    {
        Preparar();
        int acertadas = ResponderLas7();
        Decidir("decision-inicial", "biopsiar");
        Decidir("decision-final", "biopsiar");
        var retro = Retroalimentacion();
        int acierto = retro.respuesta_correcta == "biopsiar" ? 1 : 0;

        var res = JsonUtility.FromJson<ResultadoSesionRes>(MockApi.Responder(RutaCerrar, Json(new VacioReq())));

        Assert.AreEqual(1, res.total_casos);
        Assert.AreEqual(acertadas, res.preguntas_acertadas);
        Assert.AreEqual(acierto, res.aciertos_iniciales);
        Assert.AreEqual(acierto, res.aciertos_finales);
        Assert.AreEqual(retro.siguio_ia ? 1 : 0, res.veces_siguio_ia);
        Assert.AreEqual((acertadas + 2 * acierto) * 10f / 9f, res.puntuacion, 0.001f);
        Assert.IsNull(MockApi.Responder("/sesiones/activa", null), "La sesión debe quedar cerrada");
    }

    // ---- PENDIENTE DE INTEGRACIÓN CON EL BACKEND ----
    // Regla del tutor en el backend (services/tutor.py): si la decisión inicial fue correcta, la IA
    // defiende una opción incorrecta; si fue incorrecta, recomienda la correcta (dos textos distintos).
    // MockApi siempre devuelve la recomendación incorrecta fija, a propósito.
    // Se quita el [Ignore] cuando el mock deba imitar la regla real o cuando se conecte el backend.
    [Test]
    [Ignore("Pendiente de integración con el backend: el mock usa siempre la recomendación incorrecta fija")]
    public void La_IA_del_mock_sigue_la_regla_del_tutor_del_backend()
    {
        int textosDistintos = Decisiones
            .Select(d =>
            {
                Preparar();
                ResponderLas7();
                Decidir("decision-inicial", d);
                return Recomendacion().texto;
            })
            .Distinct()
            .Count();

        Assert.AreEqual(2, textosDistintos);
    }
}

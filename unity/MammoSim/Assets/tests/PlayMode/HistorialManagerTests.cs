using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Pruebas de HistorialManager (registro del análisis y guardado en historial_mammosim.json).
// El archivo real del usuario se respalda antes de cada prueba y se restaura al terminar.
public class HistorialManagerTests
{
    string _ruta;
    string _respaldo;
    readonly List<GameObject> _objetos = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        _ruta = Path.Combine(Application.persistentDataPath, "historial_mammosim.json");
        _respaldo = File.Exists(_ruta) ? File.ReadAllText(_ruta) : null;
        if (File.Exists(_ruta)) File.Delete(_ruta);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var o in _objetos) if (o != null) Object.DestroyImmediate(o);
        _objetos.Clear();

        if (_respaldo != null) File.WriteAllText(_ruta, _respaldo);
        else if (File.Exists(_ruta)) File.Delete(_ruta);
    }

    // Crea un manager nuevo; su Awake ya intenta cargar el historial guardado.
    HistorialManager Nuevo()
    {
        var go = new GameObject("Historial_Test");
        _objetos.Add(go);
        return go.AddComponent<HistorialManager>();
    }

    [Test]
    public void IniciarAnalisis_crea_el_analisis_con_caso_titulo_y_fecha()
    {
        var h = Nuevo();
        h.IniciarAnalisis("caso_001", "Caso 001");

        Assert.AreEqual("caso_001", h.AnalisisActual.idCaso);
        Assert.AreEqual("Caso 001", h.AnalisisActual.tituloCaso);
        Assert.IsNotEmpty(h.AnalisisActual.fecha);
        Assert.IsEmpty(h.AnalisisActual.respuestas);
    }

    [Test]
    public void RegistrarRespuesta_agrega_la_respuesta_con_todos_sus_datos()
    {
        var h = Nuevo();
        h.IniciarAnalisis("caso_001", "Caso 001");

        h.RegistrarRespuesta(1, "¿Lateralidad?", "A", "B", false, "Es la izquierda");

        Assert.AreEqual(1, h.AnalisisActual.respuestas.Count);
        var r = h.AnalisisActual.respuestas[0];
        Assert.AreEqual(1, r.numeroPregunta);
        Assert.AreEqual("A", r.respuestaUsuario);
        Assert.AreEqual("B", r.respuestaCorrecta);
        Assert.IsFalse(r.acerto);
        Assert.AreEqual("Es la izquierda", r.explicacion);
    }

    [Test]
    public void RegistrarDecisionInicial_guarda_la_decision_y_la_recomendacion_de_la_IA()
    {
        var h = Nuevo();
        h.IniciarAnalisis("caso_001", "Caso 001");

        h.RegistrarDecisionInicial("biopsiar", "Seguimiento");

        Assert.AreEqual("biopsiar", h.AnalisisActual.decisionInicial);
        Assert.AreEqual("Seguimiento", h.AnalisisActual.recomendacionIA);
    }

    [Test]
    public void RegistrarResultadoFinal_copia_los_campos_del_resultado()
    {
        var h = Nuevo();
        h.IniciarAnalisis("caso_001", "Caso 001");

        h.RegistrarResultadoFinal("seguimiento", new ResultadoCasoRes
        {
            acierto_inicial = true, acierto_final = false, siguio_ia = true,
            hallazgos = "Masa irregular", retroalimentacion = "Debía biopsiar"
        });

        var a = h.AnalisisActual;
        Assert.AreEqual("seguimiento", a.decisionFinal);
        Assert.IsTrue(a.aciertoInicial);
        Assert.IsFalse(a.aciertoFinal);
        Assert.IsTrue(a.siguioIA);
        Assert.AreEqual("Masa irregular", a.hallazgos);
        Assert.AreEqual("Debía biopsiar", a.retroalimentacion);
    }

    [Test]
    public void Sin_analisis_activo_no_registra_no_guarda_y_no_falla()
    {
        var h = Nuevo();

        h.RegistrarRespuesta(1, "p", "A", "B", false, "x");
        h.RegistrarDecisionInicial("biopsiar", "ignorar");
        h.RegistrarResultadoFinal("biopsiar", new ResultadoCasoRes());
        h.GuardarHistorial();

        Assert.IsNull(h.AnalisisActual);
        Assert.IsFalse(File.Exists(_ruta));
    }

    [Test]
    public void Guardar_y_volver_a_abrir_recupera_el_analisis()
    {
        var h = Nuevo();
        h.IniciarAnalisis("caso_001", "Caso 001");
        h.RegistrarRespuesta(1, "p", "A", "B", false, "x");
        h.GuardarHistorial();
        Assert.IsTrue(File.Exists(_ruta));

        Object.DestroyImmediate(h.gameObject);
        var h2 = Nuevo();   // su Awake ejecuta CargarHistorial()

        var lista = h2.ObtenerHistorial();
        Assert.AreEqual(1, lista.Count);
        Assert.AreEqual("caso_001", lista[0].idCaso);
        Assert.AreEqual(1, lista[0].respuestas.Count);
    }

    [Test]
    public void Guardar_dos_veces_el_mismo_analisis_no_lo_duplica()
    {
        var h = Nuevo();
        h.IniciarAnalisis("caso_001", "Caso 001");

        h.GuardarHistorial();
        h.GuardarHistorial();

        Assert.AreEqual(1, h.ObtenerHistorial().Count);
    }

    [Test]
    public void El_historial_acumula_un_analisis_por_cada_caso()
    {
        var h = Nuevo();
        foreach (var id in new[] { "caso_001", "caso_002", "caso_003" })
        {
            h.IniciarAnalisis(id, "Titulo " + id);
            h.GuardarHistorial();
        }

        Assert.AreEqual(3, h.ObtenerHistorial().Count);
    }

    [Test]
    public void ObtenerHistorial_devuelve_una_copia()
    {
        var h = Nuevo();
        h.IniciarAnalisis("caso_001", "Caso 001");
        h.GuardarHistorial();

        h.ObtenerHistorial().Clear();

        Assert.AreEqual(1, h.ObtenerHistorial().Count);
    }

    [Test]
    public void ObtenerAnalisis_fuera_de_rango_devuelve_null()
    {
        var h = Nuevo();
        Assert.IsNull(h.ObtenerAnalisis(0));
        Assert.IsNull(h.ObtenerAnalisis(-1));
    }

    [Test]
    public void Un_archivo_corrupto_se_ignora_y_se_registra_el_error()
    {
        File.WriteAllText(_ruta, "esto no es json");
        LogAssert.Expect(LogType.Error, new Regex(@"\[Historial\] Error al cargar"));

        var h = Nuevo();

        Assert.AreEqual(0, h.ObtenerHistorial().Count);
    }
}

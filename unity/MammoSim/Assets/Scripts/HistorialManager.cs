
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
// Información de una pregunta respondida por el estudiante.
[Serializable]
public class RespuestaHistorial
{
    public int numeroPregunta;
    public string pregunta;
    public string respuestaUsuario;
    public string respuestaCorrecta;
    public bool acerto;
    public string explicacion;
}

// Información completa de un análisis realizado.
[Serializable]
public class AnalisisHistorial
{
    public string idCaso;
    public string tituloCaso;
    public string fecha;

    public List<RespuestaHistorial> respuestas =
        new List<RespuestaHistorial>();

    public string decisionInicial;
    public string recomendacionIA;
    public string decisionFinal;

    public bool aciertoInicial;
    public bool aciertoFinal;
    public bool siguioIA;

    public string hallazgos;
    public string retroalimentacion;
}

[Serializable]
public class ListaHistorial
{
    public List<AnalisisHistorial> analisis =
        new List<AnalisisHistorial>();
}

// Administrador del historial de MammoSim.
public class HistorialManager : MonoBehaviour
{
    public static HistorialManager Instance { get; private set; }

    public AnalisisHistorial AnalisisActual { get; private set; }
    private ListaHistorial historial = new ListaHistorial();

private string RutaHistorial =>
    Path.Combine(Application.persistentDataPath, "historial_mammosim.json");


void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;

    // Recuperar el historial al iniciar Unity.
    CargarHistorial();
}


    // Prepara el registro de un nuevo análisis.
    public void IniciarAnalisis(string idCaso, string titulo)
    {
        AnalisisActual = new AnalisisHistorial
        {
            idCaso = idCaso,
            tituloCaso = titulo,
            fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        Debug.Log("[Historial] Nuevo análisis iniciado: " + idCaso);
    }

    // Guarda una respuesta técnica.
    public void RegistrarRespuesta(
        int numero,
        string pregunta,
        string respuestaUsuario,
        string respuestaCorrecta,
        bool acerto,
        string explicacion)
    {
        if (AnalisisActual == null)
        {
            Debug.LogWarning("[Historial] No hay análisis activo.");
            return;
        }

        AnalisisActual.respuestas.Add(new RespuestaHistorial
        {
            numeroPregunta = numero,
            pregunta = pregunta,
            respuestaUsuario = respuestaUsuario,
            respuestaCorrecta = respuestaCorrecta,
            acerto = acerto,
            explicacion = explicacion
        });

        Debug.Log("[Historial] Respuesta " + numero + " registrada.");
    }
    
    // Guarda la decisión inicial del estudiante
    // y la recomendación recibida de la IA.
    public void RegistrarDecisionInicial(
        string decision,
        string recomendacion)
    {
        if (AnalisisActual == null)
        {
            Debug.LogWarning(
                "[Historial] No hay análisis activo."
            );
            return;
        }

        AnalisisActual.decisionInicial = decision;
        AnalisisActual.recomendacionIA = recomendacion;

        Debug.Log(
            "[Historial] Decisión inicial: " + decision
        );

        Debug.Log(
            "[Historial] Recomendación IA: " + recomendacion
        );
    }
    
    // Guarda la decisión final y el resultado del caso.
    public void RegistrarResultadoFinal(
        string decisionFinal,
        ResultadoCasoRes resultado)
    {
        if (AnalisisActual == null)
        {
            Debug.LogWarning(
                "[Historial] No hay análisis activo."
            );
            return;
        }

        AnalisisActual.decisionFinal = decisionFinal;

        AnalisisActual.aciertoInicial = resultado.acierto_inicial;
        AnalisisActual.aciertoFinal = resultado.acierto_final;
        AnalisisActual.siguioIA = resultado.siguio_ia;

        AnalisisActual.hallazgos = resultado.hallazgos;
        AnalisisActual.retroalimentacion =
            resultado.retroalimentacion;

        Debug.Log("[Historial] Resultado final registrado.");

        // Muestra el registro completo para comprobarlo.
        Debug.Log(
            "[Historial] Análisis completo:\n" +
            JsonUtility.ToJson(AnalisisActual, true)
        );
    }


public void GuardarHistorial()
{
    if (AnalisisActual == null)
    {
        Debug.LogWarning("[Historial] No hay análisis para guardar.");
        return;
    }

    // Evitar guardar dos veces el mismo análisis.
    if (historial.analisis.Contains(AnalisisActual))
    {
        Debug.LogWarning(
            "[Historial] Este análisis ya fue guardado."
        );
        return;
    }

    try
    {
        historial.analisis.Add(AnalisisActual);

        string json = JsonUtility.ToJson(historial, true);

        File.WriteAllText(RutaHistorial, json);

        Debug.Log(
            "[Historial] Archivo guardado en: " + RutaHistorial
        );
    }
    catch (Exception e)
    {
        // Si falla la escritura, retirar el registro de la lista.
        historial.analisis.Remove(AnalisisActual);

        Debug.LogError(
            "[Historial] Error al guardar: " + e.Message
        );
    }
}


public void CargarHistorial()
{
    if (!File.Exists(RutaHistorial))
    {
        historial = new ListaHistorial();

        Debug.Log(
            "[Historial] No existe un historial anterior. Se iniciará uno nuevo."
        );

        return;
    }

    try
    {
        string json = File.ReadAllText(RutaHistorial);

        ListaHistorial datos =
            JsonUtility.FromJson<ListaHistorial>(json);

        if (datos != null && datos.analisis != null)
        {
            historial = datos;
        }
        else
        {
            historial = new ListaHistorial();
        }

        Debug.Log(
            "[Historial] Análisis recuperados: " +
            historial.analisis.Count
        );
    }
    catch (Exception e)
    {
        Debug.LogError(
            "[Historial] Error al cargar: " + e.Message
        );
    }
}

    // Devuelve todos los análisis guardados.
    public List<AnalisisHistorial> ObtenerHistorial()
    {
        return new List<AnalisisHistorial>(historial.analisis);
    }

    // Busca un análisis específico por su posición.
    public AnalisisHistorial ObtenerAnalisis(int indice)
    {
        if (indice < 0 || indice >= historial.analisis.Count)
        {
            Debug.LogWarning(
                "[Historial] No existe un análisis en esa posición."
            );

            return null;
        }

        return historial.analisis[indice];
    }

[ContextMenu("Mostrar historial guardado")]
public void MostrarHistorialGuardado()
{
    List<AnalisisHistorial> registros = ObtenerHistorial();

    Debug.Log(
        "[Historial] Total de análisis guardados: " +
        registros.Count
    );

    foreach (AnalisisHistorial analisis in registros)
    {
        Debug.Log(
            "[Historial] Caso: " + analisis.idCaso +
            " | Fecha: " + analisis.fecha +
            " | Respuestas: " + analisis.respuestas.Count +
            " | Acierto final: " + analisis.aciertoFinal
        );
    }
}


}

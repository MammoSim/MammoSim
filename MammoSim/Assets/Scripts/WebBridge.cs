using System.Runtime.InteropServices;
using UnityEngine;

// Va en un objeto vacío llamado exactamente "WebBridge".
//
// React -> Unity (el catálogo ahora vive en React):
//   unityInstance.SendMessage("ApiClient", "SetToken", idToken);
//   unityInstance.SendMessage("WebBridge", "IniciarCaso", "caso_001");
//
// Unity -> React (eventos del navegador, ver MammoSimBridge.jslib):
//   window.addEventListener("mammosim:listo", ...);
//   window.addEventListener("mammosim:resultado", e => ...);
//   window.addEventListener("mammosim:finalizada", ...);
public class WebBridge : MonoBehaviour
{
    [SerializeField] SimulationScreen pantalla;

    [Header("Solo en el Editor de Unity (nunca en el build)")]
    [SerializeField] bool iniciarSoloEnEditor = true;
    [SerializeField] string casoDePrueba = "caso_001";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void MammoSim_Evento(string nombre, string datos);
#endif

    static void Notificar(string nombre, string datos = "")
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        MammoSim_Evento(nombre, datos);
#else
        Debug.Log($"[MammoSim -> React] {nombre} {datos}");
#endif
    }

    void OnEnable()
    {
        SimulationEvents.OnEvaluacionDisponible += AlEvaluar;
        SimulationEvents.OnSimulacionFinalizada += AlFinalizar;
    }

    void OnDisable()
    {
        SimulationEvents.OnEvaluacionDisponible -= AlEvaluar;
        SimulationEvents.OnSimulacionFinalizada -= AlFinalizar;
    }

    void Start()
    {
        Notificar("listo");
#if UNITY_EDITOR
        if (iniciarSoloEnEditor) IniciarCaso(casoDePrueba);
#endif
    }

    // React -> Unity
    public void IniciarCaso(string casoId) => pantalla.Abrir(casoId);

    void AlEvaluar(ResultadoCasoRes r) => Notificar("resultado", JsonUtility.ToJson(r));
    
void AlFinalizar()
{
    // Enviar el historial completo a React.
    if (HistorialManager.Instance != null &&
        HistorialManager.Instance.AnalisisActual != null)
    {
        string jsonHistorial = JsonUtility.ToJson(
            HistorialManager.Instance.AnalisisActual
        );

        Notificar("historial", jsonHistorial);
    }
    else
    {
        Debug.LogWarning(
            "[WebBridge] No se encontró un análisis para enviar."
        );
    }

    // Mantener el evento original.
    Notificar("finalizada");
}

}

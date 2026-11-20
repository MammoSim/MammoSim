import { Unity, useUnityContext } from "react-unity-webgl";
import { useEffect, useRef, useState } from "react";
import "../styles/Simulation.css";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { useSimulationMode } from "../hooks/useSimulationMode";

export default function SimulationPage() {
  const [closing, setClosing] = useState(false);
  const navigate = useNavigate();
  const {caseId} = useParams<{caseId: string}>();
  const startedCase = useRef<string | null>(null);
  const location = useLocation();
  const {mode} = useSimulationMode();


  const idCurso =
    typeof location.state?.id_curso === "string"
      ? location.state.id_curso
      : null;

  const {
    unityProvider,
    isLoaded,
    loadingProgression,
    sendMessage,
    unload,
  } = useUnityContext({
    loaderUrl: "/build/src.loader.js",
    dataUrl: "/build/src.data",
    frameworkUrl: "/build/src.framework.js",
    codeUrl: "/build/src.wasm",
  });

  useEffect(() => {
    if(!isLoaded || !caseId || startedCase.current === caseId) return;  


    sendMessage("WebBridge", "IniciarCaso", caseId);
    startedCase.current = caseId;
  }, [isLoaded, caseId, sendMessage]);


  const handleClose = async () => {
    if (closing) return;

    setClosing(true);

    try {
      if (isLoaded) {
        await unload();
      }
      navigate("/cases", {replace: true, state: {id_curso: idCurso}});
    } catch (error) {
      console.error("Error al cerrar Unity:", error);
      setClosing(false);
    }
  };

  return (
    <main
      className="simulation_page_container"
      aria-label="Simulación MammoSim"
    >
      <div className="simulation_header">
        <h2>MammoSim - Simulación clínica {caseId}</h2>
        <p>Modalidad: {" "} {
          mode?.tipo === "clase" ? "Clase dirigida" : mode?.tipo === "individual" ? "Práctica individual" : "Sin seleccionar"}</p>

        <button
          type="button"
          onClick={handleClose}
          disabled={closing}
        >
          {closing ? "Cerrando..." : "Cerrar simulación"}
        </button>
      </div>

      <div className="unity_content">
        {!isLoaded && (
          <p className="unity_loading">
            Cargando simulación...
            {Math.round(loadingProgression * 100)}%
          </p>
        )}

        <Unity
          unityProvider={unityProvider}
          className="unity_canvas"
          tabIndex={0}
        />
      </div>
    </main>
  );
}
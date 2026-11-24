import { Unity, useUnityContext } from "react-unity-webgl";
import { useEffect, useRef, useState } from "react";
import "../styles/Simulation.css";
import { useNavigate, useParams } from "react-router-dom";
import { useSimulationMode } from "../hooks/useSimulationMode";
import { getAuth, onIdTokenChanged } from "firebase/auth";

export default function SimulationPage() {
  const [closing, setClosing] = useState(false);
  const [authError, setAuthError] = useState("");

  const navigate = useNavigate();
  const { caseId } = useParams<{ caseId: string }>();
  const startedCase = useRef<string | null>(null);
  const { mode, ready } = useSimulationMode();

  const idCurso =
  mode?.tipo === "clase" ? mode.id_curso : null;

  // Configuración del build WebGL.
  const { unityProvider, isLoaded, loadingProgression, sendMessage, unload } =
    useUnityContext({
      loaderUrl: "/Build/MammoSim-WebGL.loader.js",
      dataUrl: "/Build/MammoSim-WebGL.data",
      frameworkUrl: "/Build/MammoSim-WebGL.framework.js",
      codeUrl: "/Build/MammoSim-WebGL.wasm",
    });

  // Enviar el token de Firebase antes de iniciar el caso
  useEffect(() => {
    if (!isLoaded || !caseId || !ready || !mode) return;

    let cancelled = false;

    const auth = getAuth();

    const unsubscribe = onIdTokenChanged(auth, async (user) => {
      if (cancelled) return;

      if (!user) {
        setAuthError("Debes iniciar sesión para utilizar el simulador.");
        return;
      }

      try {
        const token = await user.getIdToken();

          if (cancelled) return;

          
          // Primero autenticamos Unity.
          sendMessage("ApiClient", "SetToken", token);

        setAuthError("");

          // Configuramos el curso antes de iniciar el caso.
          if (startedCase.current !== caseId) {
            sendMessage(
              "GameManager",
              "SetCurso",
              idCurso ?? ""
            );

            startedCase.current = caseId;

            sendMessage(
              "WebBridge",
              "IniciarCaso",
              caseId
            );
          }

        } catch (error) {
          if (cancelled) return;

        console.error("Error al obtener el token:", error);

        setAuthError("No se pudo verificar tu sesión.");
      }
    });

    return () => {
      cancelled = true;
      unsubscribe();
    };
  }, [isLoaded, caseId, ready, mode, sendMessage]);

  // Cerrar Unity y regresar a la lista de casos
  const handleClose = async () => {
    if (closing) return;

    setClosing(true);

    try {
      if (isLoaded) {
        await unload();
      }

      navigate("/cases", {
        replace: true,
        state: { id_curso: idCurso },
      });
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

        <p>
          Modalidad:{" "}
          {!ready
            ? "Cargando"
            : mode?.tipo === "clase"
              ? "Clase dirigida"
              : mode?.tipo === "individual"
                ? "Práctica individual"
                : "Sin seleccionar"}
        </p>

        <button type="button" onClick={handleClose} disabled={closing}>
          {closing ? "Cerrando..." : "Cerrar simulación"}
        </button>
      </div>

      <div className="unity_content">
        {authError && <p role="alert">{authError}</p>}

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

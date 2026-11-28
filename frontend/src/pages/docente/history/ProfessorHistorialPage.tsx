import { useEffect, useState } from "react";
import type { ProfessorSession } from "../../../types/ProfessorHistorial";
import { getCourseSessions } from "../../../services/coursesServices";
import SidebarProfessor from "../../../components/layout/SidebarProfessor";
import "../../../styles/Sidebar.css";
import ProfessorHistorialDetails from "../../../components/history/ProfessorHistorialDetails";
import "../../../styles/History.css";

export default function ProfessorHistorialPage() {
  const [sessions, setSessions] = useState<ProfessorSession[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [selectedSessionId, setSelectedSessionId] = useState<string | null>(
    null,
  );

  useEffect(() => {
    const controller = new AbortController();

    async function loadSessions() {
      try {
        const data = await getCourseSessions(controller.signal);
        if (!controller.signal.aborted) {
          setSessions(data);
        }
      } catch (error) {
        if (!controller.signal.aborted) {
          setError(
            error instanceof Error
              ? error.message
              : "No fue posible cargar el historial",
          );
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      }
    }

    void loadSessions();

    return () => controller.abort();
  }, []);

  if (selectedSessionId) {
    return (
      <div className="sidebar_container">
        <SidebarProfessor />
        <main className="history_container">
          <ProfessorHistorialDetails
            key={selectedSessionId}
            sessionId={selectedSessionId}
            onBack={() => setSelectedSessionId(null)}
          />
        </main>
      </div>
    );
  }

  return (
    <div className="sidebar_container">
      <SidebarProfessor />
      <main className="history_container">
        <h2>Historial de mi clase</h2>
        <p>Aquí encontrarás las sesiones de simulación de tus estudiantes</p>

        {loading ? (
          <p role="status">Cargando sesiones...</p>
        ) : error ? (
          <p role="alert">{error}</p>
        ) : sessions.length === 0 ? (
          <p>Aún no hay sesiones registradas de tu clase</p>
        ) : (
          <div className="history_list">
            {sessions.map((session) => (
              <article key={session.id_sesion} className="history_card professor_history_card">
                <div>
                  <h2>{session.nombre_estudiante ?? "Nombre no disponible"}</h2>
                  <p>
                    Inicio:{" "}
                    {new Date(session.fecha_inicio).toLocaleString("es-CO")}
                  </p>

                  <p>
                    Estado de la simulación:{" "}
                    {session.estado === "completada"
                      ? "Completada"
                      : "En curso"}
                  </p>
                </div>
                
                <div>
                  <p>
                    Puntuación:{" "}
                    {session.puntuacion != null
                      ? `${session.puntuacion}/10`
                      : "Aún no disponible"}
                  </p>
                </div>
                <div className="history_detail_case">
                  <button
                    type="button"
                    onClick={() => setSelectedSessionId(session.id_sesion)}
                  >
                    Ver detalle
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </main>
    </div>
  );
}

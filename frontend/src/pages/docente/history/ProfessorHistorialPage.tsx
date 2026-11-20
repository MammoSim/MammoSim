import { useEffect, useState } from "react";
import type { ProfessorSession } from "../../../types/ProfessorHistorial";
import { getCourseSessions } from "../../../services/coursesServices";
import SidebarProfessor from "../../../components/layout/SidebarProfessor";
import "../../../styles/Sidebar.css";
import ProfessorHistorialDetails from "../../../components/history/ProfessorHistorialDetails";

export default function ProfessorHistorialPage() {
  const [sessions, setSessions] = useState<ProfessorSession[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [selectedSessionId, setSSelectedSessionId] = useState<string | null>(
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
      <div>
        <SidebarProfessor />
        <main>
          <ProfessorHistorialDetails
            sessionId={selectedSessionId}
            onBack={() => setSSelectedSessionId(null)}
          />
        </main>
      </div>
    );
  }

  return (
    <div className="sidebar_container">
      <SidebarProfessor />
      <main>
        <h2>Historial de mi clase</h2>
        <p>Aquí encontrarás las sesiones de simulación de tus estudiantes</p>

        {loading ? (
          <p role="status">Cargando sesiones...</p>
        ) : error ? (
          <p role="alert">{error}</p>
        ) : sessions.length === 0 ? (
          <p>Aún no hay sesiones registradas de tu clase</p>
        ) : (
          <div>
            {sessions.map((session) => (
              <article key={session.id_sesion}>
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
              </article>
            ))}
          </div>
        )}
      </main>
    </div>
  );
}

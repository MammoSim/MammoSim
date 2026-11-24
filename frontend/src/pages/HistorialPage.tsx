import Sidebar from "../components/layout/Sidebar";
import "../styles/CasesCatalog.css";
import "../styles/History.css";
import { useEffect, useState } from "react";
import type { StudentSession } from "../types/StudentHistorial";
import {
  getMySessions,
} from "../services/studentHistorialService";
import HistoryDetails from "../components/history/HistoryDetails";

export default function HistorialPage() {
  const [sessions, setSessions] = useState<StudentSession[]>([]);
  const [selectedSession, setSelectedSession] = useState<StudentSession | null>(
    null,
  );
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const controller = new AbortController();

    async function loadSessions() {
      try {
        const data = await getMySessions(controller.signal);

        if (!controller.signal.aborted) {
          setSessions(data);
        }
      } catch (error) {
        if (!controller.signal.aborted) {
          setError(
            error instanceof Error
              ? error.message
              : "No fue posible cargar el historial.",
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

  return (
    <div className="sidebar_container">
      <Sidebar />
      <main className="history_container">
        {selectedSession ? (
          <HistoryDetails
            key={selectedSession.id_sesion}
            sessionId={selectedSession.id_sesion}
            onBack={() => setSelectedSession(null)}
          />
        ) : (
          <>
            <h1>Mi historial</h1>

            {loading ? (
              <p role="status">Cargando sesiones...</p>
            ) : error ? (
              <p role="alert">{error}</p>
            ) : sessions.length === 0 ? (
              <p>Aún no tienes sesiones registradas.</p>
            ) : (
              <div className="history_list">
                {sessions.map((session) => (
                  <article key={session.id_sesion} className="history_card">
                    <div>
                      <h2>Sesión {session.id_sesion}</h2>

                      <p>
                        Modalidad:{" "}
                        {session.id_curso === null
                          ? "Práctica individual"
                          : "Clase dirigida"}
                      </p>

                      <p>
                        Inicio:{" "}
                        {new Date(session.fecha_inicio).toLocaleString("es-CO")}
                      </p>

                      {session.fecha_fin && (
                        <p>
                          Finalización:{" "}
                          {new Date(session.fecha_fin).toLocaleString("es-CO")}
                        </p>
                      )}

                      <p>Casos: {session.casos_asignados.join(", ")}</p>

                      <p>
                        Estado:{" "}
                        {session.estado === "completada"
                          ? "Completada"
                          : "En curso"}
                      </p>
                    </div>

                    <div className="history_detail_case">
                      {session.estado === "completada" ? (
                        <button
                          type="button"
                          onClick={() => setSelectedSession(session)}
                        >
                          Ver resultado
                        </button>
                      ) : (
                        <p>
                          El resultado estará disponible al completar la sesión.
                        </p>
                      )}
                    </div>
                  </article>
                ))}
              </div>
            )}
          </>
        )}
      </main>
    </div>
  );
}

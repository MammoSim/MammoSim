import Sidebar from "../components/layout/Sidebar";
import "../styles/CasesCatalog.css";
import "../styles/History.css";
import { useEffect, useState } from "react";
import type { StudentResult, StudentSession } from "../types/StudentHistorial";
import { getMySessions, getSessionResult } from "../services/studentHistorialService";

type ResultDetailsProps = {
  session: StudentSession;
  onBack: () => void;
};

function ResultDetails({ session, onBack }: ResultDetailsProps) {
  const [result, setResult] = useState<StudentResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const controller = new AbortController();

    async function loadResult() {
      try {
        const data = await getSessionResult(
          session.id_sesion,
          controller.signal,
        );

        if (!controller.signal.aborted) {
          setResult(data);
        }
      } catch (error) {
        if (!controller.signal.aborted) {
          setError(
            error instanceof Error
              ? error.message
              : "No fue posible cargar el resultado.",
          );
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      }
    }

    void loadResult();

    return () => controller.abort();
  }, [session.id_sesion]);

  return (
    <section className="history_details">
      <button type="button" onClick={onBack} className="back_button">
        Volver al historial
      </button>

      <h2>Resumen de la sesión</h2>
      <p>Sesión: {session.id_sesion}</p>
      <p>Casos: {session.casos_asignados.join(", ")}</p>

      {loading ? (
        <p role="status">Cargando resultado...</p>
      ) : error ? (
        <p role="alert">{error}</p>
      ) : result ? (
        <div className="summary_container">
          <div className="table_wrap">
            <table className="summary_table">
              <tbody>
                <tr>
                  <th scope="row">Puntuación</th>
                  <td>{result.puntuacion}/10</td>
                </tr>
                <tr>
                  <th scope="row">Total de casos</th>
                  <td>{result.total_casos}</td>
                </tr>
                <tr>
                  <th scope="row">Preguntas técnicas correctas</th>
                  <td>{result.preguntas_acertadas}</td>
                </tr>
                <tr>
                  <th scope="row">Decisiones iniciales correctas</th>
                  <td>{result.aciertos_iniciales}</td>
                </tr>
                <tr>
                  <th scope="row">Decisiones finales correctas</th>
                  <td>{result.aciertos_finales}</td>
                </tr>
                <tr>
                  <th scope="row">
                    Decisiones finales que coincidieron con el tutor
                  </th>
                  <td>{result.veces_siguio_ia}</td>
                </tr>
                <tr>
                  <th scope="row">
                    Coincidencias con recomendaciones correctas
                  </th>
                  <td>{result.siguio_ia_correcta}</td>
                </tr>
                <tr>
                  <th scope="row">
                    Coincidencias con recomendaciones incorrectas
                  </th>
                  <td>{result.siguio_ia_incorrecta}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      ) : null}
    </section>
  );
}

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
          <ResultDetails
            key={selectedSession?.id_sesion}
            session={selectedSession}
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

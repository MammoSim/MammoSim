import { useEffect, useState } from "react";
import { getSessionDetails } from "../../services/studentHistorialService";
import type { StudentSessionDetail } from "../../types/StudentHistorial";
import "../../styles/History.css";
import CaseReview from "./CaseReview";
import ModelsPreview from "./ModelsPreview";

type HistoryDetailsProps = {
  sessionId: string;
  onBack: () => void;
};

export default function HistoryDetails({
  sessionId,
  onBack,
}: HistoryDetailsProps) {
  const [detail, setDetail] = useState<StudentSessionDetail | null>(null);
  const [selectedCaseId, setSelectedCaseId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const controller = new AbortController();

    async function loadDetail() {
      try {
        const data = await getSessionDetails(sessionId, controller.signal);

        if (!controller.signal.aborted) {
          setDetail(data);
        }
      } catch (error) {
        if (!controller.signal.aborted) {
          setError(
            error instanceof Error
              ? error.message
              : "No fue posible cargar el detalle.",
          );
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      }
    }

    void loadDetail();

    return () => controller.abort();
  }, [sessionId]);

  const selectedCase =
    detail?.casos.find((caso) => caso.id_caso === selectedCaseId) ??
    detail?.casos[0];

  return (
    <section className="history_details">
      {loading ? (
        <p role="status">Cargando detalle...</p>
      ) : error ? (
        <p role="alert">{error}</p>
      ) : detail ? (
        <>
          <div className="history_card history_header_with_model">
            <h2>Detalle de la simulación</h2>
            {selectedCase && (
              <ModelsPreview caseId={selectedCase.id_caso} title={selectedCase.titulo ?? selectedCase.id_caso}/>
            )}
            <div>
              <p>
                Realizada el{" "}
                {new Date(detail.fecha_inicio).toLocaleString("es-CO")}
              </p>

              <p>
                Puntuación de la sesión:{" "}
                {detail.puntuacion === null
                  ? "No disponible"
                  : `${detail.puntuacion}/10`}
              </p>
            </div>
            <button type="button" onClick={onBack} className="back_button">
              Volver al historial
            </button>
          </div>
          {detail.casos.length > 1 && (
            <>
              <label htmlFor="history-case">Caso analizado</label>
              <select
                id="history-case"
                value={selectedCase?.id_caso ?? ""}
                onChange={(event) => setSelectedCaseId(event.target.value)}
              >
                {detail.casos.map((caso) => (
                  <option key={caso.id_caso} value={caso.id_caso}>
                    {caso.titulo ?? caso.id_caso}
                  </option>
                ))}
              </select>
            </>
          )}

          {selectedCase ? (
            <CaseReview
              key={selectedCase.id_caso}
              caso={selectedCase}
              sesion={detail}
            />
          ) : (
            <p>No hay casos disponibles en esta sesión.</p>
          )}
        </>
      ) : null}
    </section>
  );
}

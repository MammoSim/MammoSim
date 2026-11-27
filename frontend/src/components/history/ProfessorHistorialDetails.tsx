import { useEffect, useState } from "react";
import type { ProfessorSessionDetail } from "../../types/ProfessorHistorial";
import type { CasoRevision } from "../../types/StudentHistorial";
import { getCourseSessionsDetail } from "../../services/coursesServices";
import CaseReview from "../history/CaseReview";
import "../../styles/History.css";
import ModelsPreview from "./ModelsPreview";

type DetailsProps = {
  sessionId: string;
  onBack: () => void;
};

export default function ProfessorHistorialDetails({
  sessionId,
  onBack,
}: DetailsProps) {
  const [detail, setDetail] = useState<ProfessorSessionDetail | null>(null);
  const [selectedCaseId, setSelectedCaseId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const controller = new AbortController();

    async function loadDetail() {
      try {
        const result = await getCourseSessionsDetail(
          sessionId,
          controller.signal,
        );

        if (!controller.signal.aborted) {
          setDetail(result);
        }
      } catch (error) {
        if (!controller.signal.aborted) {
          setError(
            error instanceof Error
              ? error.message
              : "No fue posible consultar el detalle",
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

  let caso: CasoRevision | null = null;

  if (
    selectedCase &&
    selectedCase.preguntas != null &&
    selectedCase.enunciado_decision != null &&
    selectedCase.opciones_decision != null &&
    selectedCase.respuesta_correcta != null &&
    selectedCase.hallazgos != null &&
    selectedCase.retroalimentacion != null
  ) {
    caso = {
      ...selectedCase,
      preguntas: selectedCase.preguntas,
      enunciado_decision: selectedCase.enunciado_decision,
      opciones_decision: selectedCase.opciones_decision,
      respuesta_correcta: selectedCase.respuesta_correcta,
      recomendacion_ia: selectedCase.recomendacion_ia ?? null,
      hallazgos: selectedCase.hallazgos,
      retroalimentacion: selectedCase.retroalimentacion,
    };
  }

  return (
    <section className="history_details">
      {loading ? (
        <>
          <button type="button" onClick={onBack} className="back_button">
            Volver al historial
          </button>
          <p role="status">Cargando detalle...</p>
        </>
      ) : error ? (
        <>
          <button type="button" onClick={onBack} className="back_button">
            Volver al historial
          </button>
          <p role="alert">{error}</p>
        </>
      ) : detail ? (
        <>
          <div className="history_card history_header_with_model">
            {selectedCase && (
              <ModelsPreview
                caseId={selectedCase.id_caso}
                title={selectedCase.titulo ?? selectedCase.id_caso}
              />
            )}
            <div>
              <h2>Detalle de la simulación</h2>
              <p>
                Estudiante: {detail.nombre_estudiante ?? "Nombre no disponible"}
              </p>
            </div>

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

              <p>
                Estado:{" "}
                {detail.estado === "completada" ? "Completada" : "En curso"}
              </p>
            </div>

            <button type="button" onClick={onBack} className="back_button">
              Volver al historial
            </button>
          </div>

          {detail.casos.length > 1 && (
            <>
              <label htmlFor="professor-history-case">Caso analizado</label>
              <select
                id="professor-history-case"
                value={selectedCase?.id_caso ?? ""}
                onChange={(event) => setSelectedCaseId(event.target.value)}
              >
                {detail.casos.map((item) => (
                  <option key={item.id_caso} value={item.id_caso}>
                    {item.titulo ?? item.id_caso}
                  </option>
                ))}
              </select>
            </>
          )}

          {detail.estado !== "completada" ? (
            <p>
              El detalle completo estará disponible cuando el estudiante
              finalice la sesión.
            </p>
          ) : caso ? (
            <CaseReview key={caso.id_caso} caso={caso} sesion={detail} />
          ) : (
            <p>
              No se recibió el detalle completo de las preguntas de este caso.
            </p>
          )}
        </>
      ) : null}
    </section>
  );
}

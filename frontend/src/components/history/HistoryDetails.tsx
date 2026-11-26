import { useEffect, useState } from "react";
import { getSessionDetails } from "../../services/studentHistorialService";
import type {
  CasoRevision,
  StudentSessionDetail,
} from "../../types/StudentHistorial";
import "../../styles/History.css";
import AnalysisSummary from "./AnalysisSummary";

type HistoryDetailsProps = {
  sessionId: string;
  onBack: () => void;
};

function CaseReview({
  caso,
  sesion,
}: {
  caso: CasoRevision;
  sesion: StudentSessionDetail;
}) {
  const [selectedNumber, setSelectedNumber] = useState<number | null>(null);
  const [showDecision, setShowDecision] = useState(false);

  const question = caso.preguntas.find(
    (item) => item.numero === selectedNumber,
  );

  function decisionText(value: string | undefined) {
    if (!value) return "No registrada";

    return (
      caso.opciones_decision.find((option) => option.id === value)?.texto ??
      value
    );
  }

  return (
    <div className="history_details_columns">
      <section className="questions_summary">
        {showDecision ? (
          <>
            <h3>Interacción con el agente tutor</h3>
            <p>{caso.enunciado_decision}</p>

            <h4>Decisión inicial</h4>
            <p>{decisionText(caso.decision_inicial?.decision)}</p>

            {caso.decision_inicial && (
              <p>{caso.decision_inicial.acierto ? "Correcta" : "Incorrecta"}</p>
            )}

            <h4>Recomendación del tutor</h4>
            {caso.recomendacion_ia ? (
              <>
                <p>{decisionText(caso.recomendacion_ia.opcion_ia)}</p>
                <p>{caso.recomendacion_ia.texto}</p>
                <p>
                  Recomendación{" "}
                  {caso.recomendacion_ia.ia_correcta
                    ? "correcta"
                    : "incorrecta"}
                </p>
              </>
            ) : (
              <p>No disponible.</p>
            )}

            <h4>Decisión final</h4>
            <p>{decisionText(caso.decision_final?.decision)}</p>

            {caso.decision_final && (
              <p>{caso.decision_final.acierto ? "Correcta" : "Incorrecta"}</p>
            )}

            {caso.recomendacion_ia?.siguio_ia != null && (
              <p>
                Tu decisión final{" "}
                {caso.recomendacion_ia.siguio_ia ? "coincidió" : "no coincidió"}{" "}
                con la recomendación del tutor.
              </p>
            )}

            <h4>Respuesta clínica correcta</h4>
            <p>{decisionText(caso.respuesta_correcta)}</p>

            <h4>Hallazgos</h4>
            <p>{caso.hallazgos}</p>

            <h4>Retroalimentación</h4>
            <p>{caso.retroalimentacion}</p>
          </>
        ) : question ? (
          <>
            <h3>Pregunta {question.numero}</h3>
            <p>{question.enunciado}</p>

            <ul>
              {question.opciones.map((option) => (
                <li key={option.id}>
                  {option.id}: {option.texto}
                  {option.id === question.opcion_elegida && " — Tu respuesta"}
                  {option.id === question.respuesta_correcta &&
                    " — Respuesta correcta"}
                </li>
              ))}
            </ul>

            <p>
              Resultado:{" "}
              {question.correcta === null
                ? "Sin respuesta registrada"
                : question.correcta
                  ? "Correcta"
                  : "Incorrecta"}
            </p>

            <h4>Explicación</h4>
            <p>{question.explicacion}</p>
          </>
        ) : (
          <p>Selecciona una pregunta o la decisión clínica.</p>
        )}
      </section>

      <div className="history_details_right">
        <AnalysisSummary caso={caso} sesion={sesion} />
        <section className="question_container">
          <div className="question_content">
            <h3>Preguntas del caso</h3>

            {caso.preguntas.map((item) => (
              <button
                key={item.numero}
                type="button"
                className="button_question"
                aria-pressed={!showDecision && selectedNumber === item.numero}
                onClick={() => {
                  setSelectedNumber(item.numero);
                  setShowDecision(false);
                }}
              >
                Pregunta {item.numero}
              </button>
            ))}

            <button
              type="button"
              className="button_question"
              aria-pressed={showDecision}
              onClick={() => {
                setSelectedNumber(null);
                setShowDecision(true);
              }}
            >
              Decisión clínica e interacción con el tutor
            </button>
          </div>
        </section>
      </div>
    </div>
  );
}

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
          <div className="history_card">
            <h2>Detalle de la simulación</h2>
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

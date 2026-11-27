import { useState } from "react";
import type {
  CasoRevision,
  StudentSessionDetail,
} from "../../types/StudentHistorial";
import AnalysisSummary from "./AnalysisSummary";
import "../../styles/History.css";

type CaseReviewProps = {
  caso: CasoRevision;
  sesion: Pick<
    StudentSessionDetail,
    "estado" | "fecha_inicio" | "fecha_fin" | "puntuacion"
  >;
};

export default function CaseReview({ caso, sesion }: CaseReviewProps) {
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
                La decisión final{" "}
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
              {question.opciones.map((option) => {
                const esRegistrada = option.id === question.opcion_elegida;
                const esCorrecta = option.id === question.respuesta_correcta;

                const color = esCorrecta
                  ? "#22C55E"
                  : esRegistrada
                    ? "#EF4444"
                    : undefined;

                return (
                  <li key={option.id} style={{ color }}>
                    {option.id}: {option.texto}
                    {esRegistrada && " — Respuesta registrada"}
                    {esCorrecta && " — Respuesta correcta"}
                  </li>
                );
              })}
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

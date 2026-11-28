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

  const inicial = caso.decision_inicial;
  const final = caso.decision_final;

  const cambioObservado =
    inicial && final
      ? inicial.acierto && !final.acierto
        ? "Cambió de una respuesta correcta a una incorrecta."
        : !inicial.acierto && final.acierto
          ? "Cambió de una respuesta incorrecta a una correcta."
          : inicial.decision === final.decision
            ? "Mantuvo su decisión inicial."
            : "Cambió de decisión, manteniendo una respuesta incorrecta."
      : null;

  return (
    <div className="history_details_columns">
      <section className="questions_summary">
        {showDecision ? (
          <>
            <h3>Interacción con el agente tutor</h3>
            <p>{caso.enunciado_decision}</p>

            <div className="container_decision">
              <h4>1. Decisión inicial</h4>
              <p>{decisionText(caso.decision_inicial?.decision)}</p>

              {inicial && (
                <span
                  className={`evaluation_badge ${
                    inicial.acierto
                      ? "evaluation_correct"
                      : "evaluation_incorrect"
                  }`}
                >
                  {inicial.acierto ? " Correcta" : " Incorrecta"}
                </span>
              )}
            </div>
            <div className="container_decision">
              <h4>2. Recomendación del tutor</h4>
              {caso.recomendacion_ia ? (
                <>
                  <p>{decisionText(caso.recomendacion_ia.opcion_ia)}</p>
                  <p>{caso.recomendacion_ia.texto}</p>
                  <p>
                    <span
                      className={`evaluation_badge ${
                        caso.recomendacion_ia.ia_correcta
                          ? "evaluation_correct"
                          : "evaluation_incorrect"
                      }`}
                    >
                      {caso.recomendacion_ia.ia_correcta
                        ? " Recomendación correcta"
                        : " Recomendación incorrecta"}
                    </span>
                  </p>
                </>
              ) : (
                <p>No disponible.</p>
              )}
            </div>

            <div className="container_decision">
              <h4>3. Decisión final</h4>
              <p>{decisionText(caso.decision_final?.decision)}</p>

              {final && (
                <span
                  className={`evaluation_badge ${
                    final.acierto
                      ? "evaluation_correct"
                      : "evaluation_incorrect"
                  }`}
                >
                  {final.acierto ? " Correcta" : " Incorrecta"}
                </span>
              )}
              {cambioObservado && (
                <div className="decision_change_summary">
                  <h4>Cambio observado</h4>
                  <p>{cambioObservado}</p>

                  {caso.recomendacion_ia?.siguio_ia != null && (
                    <p>
                      La decisión final{" "}
                      {caso.recomendacion_ia.siguio_ia
                        ? "coincidió"
                        : "no coincidió"}{" "}
                      con la recomendación del tutor.
                    </p>
                  )}
                </div>
              )}
            </div>
            <div className="container_decision">
              <h4>4. Respuesta clínica correcta</h4>
              <p>{decisionText(caso.respuesta_correcta)}</p>
            </div>
            <div className="container_decision">
              <h4>5. Hallazgos</h4>
              <p>{caso.hallazgos}</p>
            </div>
            <div className="container_decision">
              <h4>6. Retroalimentación</h4>
              <p>{caso.retroalimentacion}</p>
            </div>
          </>
        ) : question ? (
          <>
            <h3>Pregunta {question.numero}</h3>
            <p>{question.enunciado}</p>

            <ul>
              {[...question.opciones]
                .sort((a, b) => a.id.localeCompare(b.id))
                .map((option) => {
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
              <span
                style={{
                  color:
                    question.correcta === null
                      ? undefined
                      : question.correcta
                        ? "#22C55E"
                        : "#EF4444",
                }}
              >
                {question.correcta === null
                  ? "Sin respuesta registrada"
                  : question.correcta
                    ? "Correcta"
                    : "Incorrecta"}
              </span>
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

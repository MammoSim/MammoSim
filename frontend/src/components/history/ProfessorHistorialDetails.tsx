import { useEffect, useState } from "react";
import { type ProfessorSessionDetail } from "../../types/ProfessorHistorial";
import { getCourseSessionsDetail } from "../../services/coursesServices";

type DetailsProps = {
  sessionId: string;
  onBack: () => void;
};

function decisionLabel(value: string) {
  switch (value) {
    case "biopsiar":
      return "Biopsiar";
    case "seguimiento":
      return "Seguimiento";
    case "ignorar":
      return "Ignorar";
    case "estudio_adicional":
      return "Estudio adcional";
    default:
      return value;
  }
}

export default function ProfessorHistorialDetails({
  sessionId,
  onBack,
}: DetailsProps){
  const [detail, setDetail] = useState<ProfessorSessionDetail | null>(null);
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

  return (
    <section>
      <button type="button" onClick={onBack}>Volver al historial</button>
      {loading ? (
        <p role="status">Cargando detalle...</p>
      ) : error ? (
        <p  role="alert">{error}</p>
      ) : detail ? (
        <>
          <h2>{detail.nombre_estudiante ?? " Nombre no disponible"}</h2>
          <p>Sesión: {" "} {detail.id_sesion}</p>
          <p>
            Estado:{" "} {detail.estado === "completada" ? "Completada" : "En curso"}
          </p>
          <p>Inicio:{" "} {new Date(detail.fecha_inicio).toLocaleString("es-CO")}</p>
          
          {detail.fecha_fin && (
            <p>
              Finalización: {" "}{new Date(detail.fecha_fin).toLocaleString("es-CO")}
            </p>
          )}
          <p>
            Puntuación de la sesión:{" "}
            {detail.puntuacion === null
              ? "Aún no se encuentra disponible"
              : `${detail.puntuacion}/10`}
          </p>
          {detail.casos.length === 0 && (<p>Esta sesión no contiene casos</p>)}
          {detail.casos.map((caso) =>(
            <article key={caso.id_caso}>
                <h3>{caso.titulo ?? caso.id_caso}</h3>
                <p>
                    Nota del caso:{" "}
                    {caso.nota_caso === null ? "Aún no disponible" : `${caso.nota_caso}/10`}
                </p>
                <h4>Respuestas registradas</h4>

                {caso.respuestas.length === 0 ? (<p>Todavía no hay respuestas</p>) : (
                    <div>
                        <table>
                            <thead>
                                <tr>
                                    <th scope="col">Pregunta</th>
                                    <th scope="col">Opción elegida</th>
                                    <th scope="col">Evaluación</th>
                                </tr>
                            </thead>
                            <tbody>
                                {caso.respuestas.map((respuesta) => (
                                    <tr key={respuesta.numero_pregunta}>
                                        <th scope="row">{respuesta.numero_pregunta}</th>
                                        <td>{respuesta.opcion_elegida}</td>
                                        <td>{respuesta.correcta ? "Correcta" : "Incorrecta"}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
                <h4>Decisión inicial</h4>
                {caso.decision_inicial ? (<p>{decisionLabel(caso.decision_inicial.decision)} {" - "} {caso.decision_inicial.acierto ? "Correcta" : "Incorrecta"}</p>) : (<p>Todavía no registrada</p>)}

                <h4>Recomendación del agente tutor y decisión final</h4>
                {caso.decision_final ? (
                    <>
                        <p>
                            Opción recomendada: {" "}
                            {decisionLabel(caso.decision_final.opcion_ia)}
                        </p>
                        
                        <p>
                            Recomendación: {" "}
                            {decisionLabel(caso.decision_final.ia_correcta ? "Correcta" : "Incorrecta")}
                        </p>
                        <p>
                            Decisión final: {" "}
                            {decisionLabel(caso.decision_final.decision)} {" - "} {caso.decision_final.acierto ? "Correcto" : "Incorrecta"}    
                        </p>
                        <p>
                            Coincidió con el tutor:{" "}
                            {caso.decision_final.siguio_ia ? "Si" : "No"}
                        </p>
                    </>
                ): (
                    <p>Todavía no hay una decision final registrada</p>
                )}
            </article>
          ))}
        </>
      ) : null}
    </section>
  );
}

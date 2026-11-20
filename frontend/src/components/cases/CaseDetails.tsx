import type { clinicalCase } from "../../types/ClinicalCase";
import "../../styles/CaseDetails.css";
import { useLocation, useNavigate } from "react-router-dom";

type CaseDetailsProps = {
  clinicalCase: clinicalCase;
  canStartSimulation?: boolean;
};

export default function CaseDetails({
  clinicalCase,
  canStartSimulation = true,
}: CaseDetailsProps) {
  const navigate = useNavigate();

  const location = useLocation();

  const idCurso =
    typeof location.state?.id_curso === "string"
      ? location.state.id_curso
      : null;

  return (
    <aside className="detail_container">
      <h2>{clinicalCase.clinical_id}</h2>
      <h4>{clinicalCase.title}</h4>
      {clinicalCase.imgUrl && (
        <img
          src={clinicalCase.imgUrl}
          alt={`Vista previa de ${clinicalCase.imgUrl}`}
          width={250}
        />
      )}
      {clinicalCase.pacientAge !== undefined && (
        <p>
          Edad simulada: <br /> {clinicalCase.pacientAge} años
        </p>
      )}
      <h4>Descripción del caso</h4>
      <p>{clinicalCase.descripcion_breve}</p>
      <p>Objetivos de aprendizaje</p>
      { canStartSimulation && (
        <button
          type="button"
          className="start_sim"
          onClick={() =>
            navigate(
              `/simulation/${encodeURIComponent(clinicalCase.clinical_id)}`,
              { state: { id_curso: idCurso } },
            )
          }
        >
          <span>Iniciar Simulación</span>
        </button>
      )}
    </aside>
  );
}

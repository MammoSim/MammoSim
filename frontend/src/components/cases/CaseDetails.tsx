import type { clinicalCase } from "../../types/ClinicalCase";
import "../../styles/CaseDetails.css";
import { useNavigate } from "react-router-dom";

type CaseDetailsProps = {
  clinicalCase: clinicalCase;
};

export default function CaseDetails({ clinicalCase }: CaseDetailsProps) {
  const navigate = useNavigate();

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
      <button
        type="button"
        className="start_sim"
        onClick={() =>
          navigate(
            `/simulation/${encodeURIComponent(clinicalCase.clinical_id)}`,
          )
        }
      >
        <span>Iniciar Simulación</span>
      </button>
    </aside>
  );
}

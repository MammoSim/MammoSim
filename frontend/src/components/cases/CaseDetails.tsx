import type { clinicalCase } from "../../types/ClinicalCase";
import "../../styles/CaseDetails.css";

type CaseDetailsProps = {
  clinicalCase: clinicalCase;
};

export default function CaseDetails({ clinicalCase }: CaseDetailsProps) {
  return (
    <aside className="detail_container">
        <h2>{clinicalCase.id}</h2>
        <h4>{clinicalCase.title}</h4>
        <img src={clinicalCase.imgUrl} alt="mamografía" width={250} />
        <h4>Descripción del caso</h4>
        <p>
          {clinicalCase.descripcion_breve}
        </p>
        <p>Objetivos de aprendizaje</p>
        <button type="button" className="start_sim" disabled>
          <span>Iniciar Simulación</span>
        </button>
    </aside>
  );
}

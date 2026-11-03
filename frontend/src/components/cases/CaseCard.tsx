import type { clinicalCase } from "../../types/ClinicalCase";
import "../../styles/CasesCatalog.css";

type CaseCardProps = {
  clinicalCase: clinicalCase;
};

export default function CaseCard({ clinicalCase }: CaseCardProps) {
  const difficulty =
    clinicalCase.difficulty === "baja"
      ? "#22C55E"
      : clinicalCase.difficulty === "media"
        ? "#F5B301"
        : "#EF4444";

  
  return (
    <article className="card">
      <h2>{clinicalCase.id}</h2>
      <img src={clinicalCase.imgUrl} alt="mamografía" width={100} />
      <p style={{ color: difficulty }}>Dificultad: {clinicalCase.difficulty}</p>
      <h3>{clinicalCase.title} </h3>
      <p>
        Edad estimada del paciente: <br /> {clinicalCase.pacientAge}
      </p>
      <p>
        Tipo de estudio: <br /> {clinicalCase.imagingStudy}
      </p>
    </article>
  );
}

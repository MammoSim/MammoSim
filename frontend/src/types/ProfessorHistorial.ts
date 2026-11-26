import type { CasoRevision } from "./StudentHistorial";
export type ProfessorSession = {
  id_sesion: string;
  id_usuario: string;
  nombre_estudiante: string | null;
  estado: "en_curso" | "completada";
  fecha_inicio: string;
  puntuacion: number | null;
};

type Decision = "biopsiar" | "seguimiento" | "ignorar" | "estudio_adicional";

export type ProfessorCaseDetail = {
  id_caso: string;
  titulo: string | null;
  respuestas: {
    numero_pregunta: number;
    opcion_elegida: "A" | "B" | "C" | "D";
    correcta: boolean;
  }[];
  decision_inicial: {
    decision: Decision;
    acierto: boolean;
  } | null;
  decision_final: {
    decision: Decision;
    acierto: boolean;
    opcion_ia: Decision;
    ia_correcta: boolean;
    siguio_ia: boolean;
  } | null;
  nota_caso: number | null;
  preguntas?: CasoRevision["preguntas"] | null;
  enunciado_decision?: string | null;
  opciones_decision?: CasoRevision["opciones_decision"] | null;
  respuesta_correcta?: string | null;
  recomendacion_ia?: CasoRevision["recomendacion_ia"];
  hallazgos?: string | null;
  retroalimentacion?: string | null;
};

export type ProfessorSessionDetail = {
  id_sesion: string;
  id_usuario: string;
  nombre_estudiante: string | null;
  estado: "en_curso" | "completada";
  fecha_inicio: string;
  fecha_fin: string | null;
  puntuacion: number | null;
  casos: ProfessorCaseDetail[];
};

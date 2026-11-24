export type StudentSession = {
  id_sesion: string;
  id_usuario: string;
  id_curso: string | null;
  casos_asignados: string[];
  estado: "en_curso" | "completada";
  fecha_inicio: string;
  fecha_fin: string | null;
};

export type StudentResult = {
  id_usuario: string;
  id_curso: string | null;
  total_casos: number;
  aciertos_iniciales: number;
  aciertos_finales: number;
  preguntas_acertadas: number;
  veces_siguio_ia: number;
  siguio_ia_correcta: number;
  siguio_ia_incorrecta: number;
  puntuacion: number;
};

type OpcionRevision = {
  id: string; 
  texto: string; 
}


export type PreguntaRevision = {
  numero: number;
  enunciado: string;
  opciones: {
    id: string;
    texto: string;
  }[];
  opcion_elegida: string | null;
  respuesta_correcta: string;
  correcta: boolean | null;
  explicacion: string;
};

export type CasoRevision = {
  id_caso: string;
  titulo: string | null;
  nota_caso: number | null;
  preguntas: PreguntaRevision[];
  enunciado_decision: string;
  opciones_decision: OpcionRevision[];
  respuesta_correcta: string;
  decision_inicial: {
    decision: string;
    acierto: boolean;
  } | null;
  recomendacion_ia: {
    opcion_ia: string;
    texto: string;
    ia_correcta: boolean;
    siguio_ia: boolean | null;
  } | null;
  decision_final: {
    decision: string;
    acierto: boolean;
  } | null;
  hallazgos: string;
  retroalimentacion: string;
};

export type StudentSessionDetail = {
  id_sesion: string;
  estado: "en_curso" | "completada";
  fecha_inicio: string;
  fecha_fin: string | null;
  puntuacion: number | null;
  casos: CasoRevision[];
};
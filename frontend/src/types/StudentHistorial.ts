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
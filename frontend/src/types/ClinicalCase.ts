export type clinicalCase = {
    id: string;
    title: string; 
    imgUrl:string;
    dificultad: "baja" | "media" | "alta";
    pacientAge: number;
    descripcion_breve: string;
    imagingStudy: string;
};
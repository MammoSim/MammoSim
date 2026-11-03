export type clinicalCase = {
    id: string;
    title: string; 
    imgUrl:string;
    difficulty: "baja" | "media" | "alta";
    pacientAge: number;
    imagingStudy: string;
};
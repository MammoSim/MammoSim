import { onAuthStateChanged, type User } from "firebase/auth";
import { auth } from "../config/firebase";
import type { clinicalCase } from "../types/ClinicalCase";

type CasoResumenApi = {
  id_caso: string;
  titulo: string;
  descripcion_breve: string;
  lado: string;
  vistas: string[];
  dificultad: string;
  modelo_3d_ref: string;
};

function waitForUser(): Promise<User | null> {
  return new Promise((resolve, reject) => {
    const unsubscribe = onAuthStateChanged(
      auth,
      (user) => {
        unsubscribe();
        resolve(user);
      },
      reject,
    );
  });
}

function mapDifficulty(value: string): clinicalCase["dificultad"] {
  const dificultad = value
    .trim()
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "");

  switch (dificultad) {
    case "facil":
    case "baja":
      return "baja";
    case "medio":
    case "media":
      return "media";

    case "dificil":
    case "alta":
      return "alta";
    default:
      throw new Error(`Dificultad no reconocida ${value}`);
  }
}

export async function getCases(signal?: AbortSignal): Promise<clinicalCase[]> {
  const user = await waitForUser();

  if (!user) {
    throw new Error("Inicia sesion para consultar los casos");
  }

  const token = await user.getIdToken();

  const response = await fetch("http://localhost:8000/casos", {
    headers: {
      Authorization: `Bearer ${token}`,
    },
    signal,
  });

  if (!response.ok) {
    throw new Error(
      response.status === 403
        ? "Debes completar el registro para consultar los casos"
        : `No fue posible cargar los casos (${response.status})`,
    );
  }

  const data: CasoResumenApi[] = await response.json();

  return data.map((caso) => ({
    clinical_id: caso.id_caso,
    title: caso.titulo,
    descripcion_breve: caso.descripcion_breve,
    dificultad: mapDifficulty(caso.dificultad),
    imagingStudy: caso.vistas.join(" / "),
    modelRef: caso.modelo_3d_ref,
  }));
}

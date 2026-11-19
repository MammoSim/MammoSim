import { onAuthStateChanged, type User } from "firebase/auth";
import { auth } from "../config/firebase";

export type Course = {
  id_curso: string;
  codigo: string;
  nombre: string;
  id_docente: string;
};

const API_URL = "http://localhost:8000";

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

async function getHeaders(): Promise<Record<string, string>> {
  const user = await waitForUser();

  if (!user) {
    throw new Error("Inicia sesión para consultar tu clase");
  }

  const token = await user.getIdToken();

  return {
    "Content-Type": "application/json",
    Authorization: `Bearer ${token}`,
  };
}

async function responseError(response: Response): Promise<Error> {
  const body = await response.json().catch(() => null);

  return new Error(
    typeof body?.detail === "string"
      ? body.detail
      : `No se pudo completar la solicitu ${response.status}`,
  );
}

export async function getMyCourse(
  signal?: AbortSignal,
): Promise<Course | null> {
  const response = await fetch(`${API_URL}/cursos/mio`, {
    headers: await getHeaders(),
    signal,
  });

  if(response.status === 404) return null;
  if(!response.ok) throw await responseError(response);

  return response.json();
}

export async function createCourse(nombre: string): Promise<Course>{
    const response = await fetch(`${API_URL}/cursos`, {
        method: "POST",
        headers: await getHeaders(),
        body: JSON.stringify({nombre: nombre.trim()
        }),
    });

    if(!response.ok) throw await responseError(response);

    return response.json();
}
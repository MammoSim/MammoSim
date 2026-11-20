import { onAuthStateChanged, type User } from "firebase/auth";
import { auth } from "../config/firebase";
import type { ProfessorSession, ProfessorSessionDetail } from "../types/ProfessorHistorial";

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

{/* Aviso de que no puede visualizar la información sin iniciar sesión */}
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

{/* Error de solicitud */}

async function responseError(response: Response): Promise<Error> {
  const body = await response.json().catch(() => null);

  return new Error(
    typeof body?.detail === "string"
      ? body.detail
      : `No se pudo completar la solicitud ${response.status}`,
  );
}

{/* el docente ve el curso */}

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


{/* el docente crea el curso */}
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


{/*Unirse a la clase con el código*/}

export async function joinCourse(codigo: string): Promise<Course>{
  const response = await fetch(`${API_URL}/inscripciones`, {
    method: "POST",
    headers: await getHeaders(),
    body: JSON.stringify({
      codigo: codigo.trim().toUpperCase(),
    }),
  });

  if(!response.ok){
    throw await responseError(response);
  }
   return response.json();
}

{/*Sesiones de los cursos*/}

export async function getCourseSessions(signal?: AbortSignal,) : Promise<ProfessorSession[]>{
  const response = await fetch(`${API_URL}/cursos/mio/sesiones`, {
    headers: await getHeaders(),
    signal,
  });

  if (!response.ok){
    throw await responseError(response);
  }

  return response.json();
}


{/*Detalle de las sesiones respectivas del curso */}

export async function getCourseSessionsDetail(sessionId: string, signal?: AbortSignal,): Promise<ProfessorSessionDetail> {
  const response = await fetch(`${API_URL}/cursos/mio/sesiones/${encodeURIComponent(sessionId)}`,
{
  headers: await getHeaders(),
  signal,
},);
  if(!response.ok){
    throw await responseError(response);
  }

  return response.json();
}
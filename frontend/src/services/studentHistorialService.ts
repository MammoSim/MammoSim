import { onAuthStateChanged, type User } from "firebase/auth";
import { auth } from "../config/firebase";
import { type StudentSessionDetail, type StudentResult, type StudentSession } from "../types/StudentHistorial";

const API_URL = import.meta.env.VITE_API_URL;

async function request<T>(path: string, signal?: AbortSignal): Promise<T> {
  const user = await new Promise<User | null>((resolve, reject) => {
    const unsubscribe = onAuthStateChanged(
      auth,
      (currentUser) => {
        unsubscribe();
        resolve(currentUser);
      },
      reject,
    );
  });

  if (!user) {
    throw new Error("Debes iniciar sesión para consultar el historial.");
  }

  const token = await user.getIdToken();

  const response = await fetch(`${API_URL}${path}`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
    signal,
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);

    throw new Error(
      typeof body?.detail === "string"
        ? body.detail
        : `No fue posible consultar el historial (${response.status}).`,
    );
  }

  return response.json();
}

export function getMySessions(signal?: AbortSignal) {
  return request<StudentSession[]>("/sesiones/mias", signal);
}


/* Obtener los resultados de la interacción */
export function getSessionResult(sessionId: string, signal?: AbortSignal) {
  return request<StudentResult>(
    `/sesiones/${encodeURIComponent(sessionId)}/resultado`,
    signal,
  );
}


/* Obtener el detalle de la sesión */
export function getSessionDetails(sessionId: string, signal?: AbortSignal){
  return request<StudentSessionDetail>(
    `/sesiones/${encodeURIComponent(sessionId)}/detalle`,
    signal,
  );
}
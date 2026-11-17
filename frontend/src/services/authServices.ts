import { FirebaseError } from "firebase/app";
import {
  createUserWithEmailAndPassword,
  signInWithEmailAndPassword,
} from "firebase/auth";

import { auth } from "../config/firebase";

const API_URL = "http://localhost:8000";

type RegistrationInput = {
  email: string;
  password: string;
  nombre_completo: string;
  carrera: string;
  semestre: number | null;
  acepto_terminos: boolean;
};

type Usuario = {
  id_usuario: string;
  nombre_completo: string;
  correo: string;
  rol: "estudiante" | "docente";
  carrera: string;
  semestre: number | null;
  acepto_terminos: boolean;
};

type RegistrationResult = {
  usuario: Usuario;
  aviso: string | null;
};

export async function registerUser(
  datos: RegistrationInput,
): Promise<RegistrationResult> {
  const email = datos.email.trim();

  let user;

  try {
    const credential = await createUserWithEmailAndPassword(
      auth,
      email,
      datos.password,
    );

    user = credential.user;
  } catch (error) {
    if (
      error instanceof FirebaseError &&
      error.code === "auth/email-already-in-use"
    ) {
      const credential = await signInWithEmailAndPassword(
        auth,
        email,
        datos.password,
      );
      user = credential.user;
    } else {
      throw error;
    }
  }

  const token = await user.getIdToken();

  const response = await fetch(`${API_URL}/auth/registro`, {
    method: "POST",
    headers: {
        "Content-Type": "application/json", 
        Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({
        nombre_completo: datos.nombre_completo,
        carrera: datos.carrera,
        semestre: datos.semestre,
        acepto_terminos: datos.acepto_terminos,
    }),
  });

  if (!response.ok) {
    throw new Error(
        response.status === 409 ? "Esta cuenta ya tiene un perfil asociado. Inicie sesión" : "No fue posible guardar el perfil. Reintente con los mismo datos.",
    );
  }

  return response.json();
}

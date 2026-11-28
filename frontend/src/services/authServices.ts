import { FirebaseError } from "firebase/app";
import {
  createUserWithEmailAndPassword,
  EmailAuthProvider,
  onAuthStateChanged,
  reauthenticateWithCredential,
  signInWithEmailAndPassword,
  signOut,
  updatePassword,
  type User,
} from "firebase/auth";

import { auth } from "../config/firebase";

const API_URL = import.meta.env.VITE_API_URL;

type RegistrationInput = {
  email: string;
  password: string;
  nombre_completo: string;
  carrera: string;
  semestre: number | null;
  acepto_terminos: boolean;
};

export type Usuario = {
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
      throw new Error(
        "Este correo ya está registrado. Inicia sesión con tu cuenta existente.",
        { cause: error },
      );
    }
    throw error;
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
      response.status === 409
        ? "Esta cuenta ya tiene un perfil asociado. Inicie sesión"
        : "No fue posible guardar el perfil. Reintente con los mismo datos.",
    );
  }

  return response.json();
}

{
  /*Inicio de sesión teniendo en cuenta el rol */
}
export async function loginUser(
  email: string,
  password: string,
  userType: Usuario["rol"],
): Promise<Usuario["rol"]> {
  const credential = await signInWithEmailAndPassword(
    auth,
    email.trim(),
    password,
  );

  try {
    const token = await credential.user.getIdToken();

    const response = await fetch(`${API_URL}/usuarios/me`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });

    if (!response.ok) {
      throw new Error(
        response.status === 404
          ? "Tu cuenta aún no tiene un perfil registrado. Regístrate"
          : "No fue posible consultar tu perfil.",
      );
    }
    const usuario: Usuario = await response.json();
    if(usuario.rol !== userType){
      const opcion = usuario.rol === "docente" ? "Docente" : "Estudiante";

      throw new Error(
        `Esta cuenta es de ${usuario.rol}. Selecciona la opción ${opcion} para ingresar.`,
      );
    }

    return usuario.rol;
  } catch (error) {
    await signOut(auth);
    throw error;
  }
}

{
  /*Obtener los datos del perfil */
}

export async function getMyProfile(signal?: AbortSignal): Promise<Usuario> {
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
    throw new Error("Debe iniciar sesión");
  }
  const token = await user.getIdToken();
  const response = await fetch(`${API_URL}/usuarios/me`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
    signal,
  });

  if (!response.ok) {
    throw new Error(
      response.status === 404
        ? "Tu cuenta aún no tiene un perfil registrado"
        : `No fue posible consultar tu perfil ${response.statusText}`,
    );
  }

  return response.json();
}

{
  /*Cambio de contraseña*/
}
export async function changePassword(
  currentPassword: string,
  newPassword: string,
): Promise<void> {
  const user = auth.currentUser;

  if (!user?.email) {
    throw new Error("Debes iniciar sesión para cmabiar la contraseña");
  }

  const credential = EmailAuthProvider.credential(user.email, currentPassword);

  await reauthenticateWithCredential(user, credential);
  await updatePassword(user, newPassword);
}

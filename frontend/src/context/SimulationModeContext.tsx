import {
  type ReactNode,
  useEffect,
  useRef,
  useState,
} from "react";
import { onAuthStateChanged } from "firebase/auth";
import { auth } from "../config/firebase";
import {
  SimulationModeContext,
  type SimulationMode,
} from "../context/SimulationMode";

function storageKey(uid: string) {
  return `mammosim:modalidad:${uid}`;
}

function readMode(uid: string): SimulationMode | null {
  try {
    const stored = sessionStorage.getItem(storageKey(uid));
    if (!stored) return null;

    const value = JSON.parse(stored);

    if (!value || typeof value !== "object") return null;

    if (value.tipo === "individual" && value.id_curso === null) {
      return { tipo: "individual", id_curso: null };
    }

    if (
      value.tipo === "clase" &&
      typeof value.id_curso === "string" &&
      value.id_curso.trim()
    ) {
      return { tipo: "clase", id_curso: value.id_curso };
    }
  } catch {
    // Se solicitará elegir nuevamente la modalidad
  }

  return null;
}

export function SimulationModeProvider({
  children,
}: {
  children: ReactNode;
}) {
  const [mode, setMode] = useState<SimulationMode | null>(null);
  const [ready, setReady] = useState(false);
  const previousUid = useRef<string | null>(null);

  useEffect(() => {
    return onAuthStateChanged(auth, (user) => {
      const uid = user?.uid ?? null;

      if (previousUid.current && previousUid.current !== uid) {
        try {
          sessionStorage.removeItem(storageKey(previousUid.current));
        } catch {
          // El estado en memoria se reemplaza igualmente
        }
      }

      previousUid.current = uid;
      setMode(uid ? readMode(uid) : null);
      setReady(true);
    });
  }, []);

  function saveMode(value: SimulationMode) {
    const user = auth.currentUser;

    if (!user) {
      throw new Error("Debes iniciar sesión para elegir una modalidad.");
    }

    setMode(value);

    try {
      sessionStorage.setItem(
        storageKey(user.uid),
        JSON.stringify(value),
      );
    } catch {
      // Se conserva la selección en memoria
    }
  }

  return (
    <SimulationModeContext.Provider
      value={{
        mode,
        ready,
        selectIndividual: () =>
          saveMode({ tipo: "individual", id_curso: null }),
        selectCourse: (courseId) =>
          saveMode({ tipo: "clase", id_curso: courseId }),
      }}
    >
      {children}
    </SimulationModeContext.Provider>
  );
}
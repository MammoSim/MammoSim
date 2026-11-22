import { createContext } from "react";

export type SimulationMode =
  | { tipo: "individual"; id_curso: null }
  | { tipo: "clase"; id_curso: string };

type SimulationModeContextValue = {
  mode: SimulationMode | null;
  ready: boolean;
  selectIndividual: () => void;
  selectCourse: (courseId: string) => void;
};

export const SimulationModeContext =
  createContext<SimulationModeContextValue | null>(null);
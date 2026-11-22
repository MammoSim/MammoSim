import { useContext } from "react";
import { SimulationModeContext } from "../context/SimulationMode";

export function useSimulationMode() {
  const context = useContext(SimulationModeContext);

  if (!context) {
    throw new Error(
      "useSimulationMode debe usarse dentro de SimulationModeProvider.",
    );
  }

  return context;
}
import { useState } from "react";
import LoginForm from "../components/auth/LoginForm";
import "../styles/AuthPage.css";
import Logo from "../assets/Logo-bg.png";
import SimulationModeDialog from "../components/courses/SimulationModeDialog";

export type UserType = "estudiante" | "docente";

export default function LoginPage() {
  const [userType, setUserType] = useState<UserType>("estudiante");
  const [isModeOpen, setIsModeOpen] = useState(false);

  return (
    <main className="auth_page">
      <section className="auth_welcome">
        <img src={Logo} alt="MammoSim Logo" className="logo" width={250} />
        <h1>
          {userType === "estudiante"
            ? "¡Bienvenido estudiante!"
            : "¡Bienvenido docente!"}
        </h1>
        <h4>
          Accede a la simulación de casos clínicos e interactúa con nuestro
          agente tutor con IA
        </h4>
      </section>
      <section className="auth_page_panel">
        <LoginForm
          userType={userType}
          onUserTypeChange={setUserType}
          onAuthenticated={(role) => {
            if (role === "estudiante") setIsModeOpen(true);
          }}
        />
        {import.meta.env.DEV && (
          <button type="button" onClick={() => setIsModeOpen(true)}>
            Probar diálogo
          </button>
        )}
        <SimulationModeDialog
          isOpen={isModeOpen}
          onClose={() => setIsModeOpen(false)}
        />
      </section>
    </main>
  );
}

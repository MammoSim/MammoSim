import { useState } from "react";
import LoginForm from "../components/auth/LoginForm";
import "../styles/AuthPage.css";
import Logo from "../assets/Logo-bg.png";
import SimulationModeDialog from "../components/mode/SimulationModeDialog";
import { useLocation } from "react-router-dom";
import { loginUser } from "../services/authServices";

export type UserType = "estudiante" | "docente";

export default function LoginPage() {
  const [userType, setUserType] = useState<UserType>("estudiante");
  const [isModeOpen, setIsModeOpen] = useState(false);
  const location = useLocation();

  const registrationMessage = typeof location.state?.registrationMessage === "string" ? location.state.registrationMessage : null; 
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
        {registrationMessage && <p role="status">{registrationMessage}</p>}
        <LoginForm
          userType={userType}
          onUserTypeChange={setUserType}
          authenticate={loginUser}
          onAuthenticated={(role) => {
            if (role === "estudiante") {setIsModeOpen(true)} else {window.alert("Inicio docente correcto")};
          }}
        />
        <SimulationModeDialog
          isOpen={isModeOpen}
          onClose={() => setIsModeOpen(false)}
        />
      </section>
    </main>
  );
}

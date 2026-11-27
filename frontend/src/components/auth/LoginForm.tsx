import TypeUserButton from "../ButtonUser";
import "../../styles/AuthPage.css";
import {useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import type { UserType } from "../../pages/LoginPage";

type LoginFormProps = {
  userType: UserType;
  onUserTypeChange: (type: UserType) => void;
  authenticate?: (email: string, password: string) => Promise<UserType>;
  onAuthenticated: (role: UserType) => void;
};

export default function LoginForm({
  userType,
  onUserTypeChange,
  authenticate,
  onAuthenticated,
}: LoginFormProps) {
  const isProfessor = userType === "docente";
  const [aceptado, setAceptado] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [loginError, setLoginError] = useState("");

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoginError("");

    if (!authenticate) {
      setLoginError("El inicio de sesión todavía no está conectado.");
      return;
    }

    const fields = new FormData(event.currentTarget);
    const email = String(fields.get("email") ?? "").trim();
    const password = String(fields.get("password") ?? "");

    setIsSubmitting(true);
    try {
      const confirmedRole = await authenticate(email, password);
      onAuthenticated(confirmedRole);
    } catch (error){
      setLoginError(error instanceof Error ? error.message : "No se pudo iniciar sesión. Inténtalo de nuevo.",);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="auth_access">
      <TypeUserButton userType={userType} onUserTypeChange={onUserTypeChange} />
      <section className="auth">
        <h1>Accede a tu cuenta</h1>
        <p>Para comenzar a practicar</p>
      </section>

      <section className="auth_page_panel">
        <form className="form_auth" onSubmit={handleSubmit}>
          <label>
            {isProfessor
              ? "Correo electrónico institucional"
              : "Correo electrónico"}
          </label>
          <input
            id="email"
            name="email"
            type="email"
            autoComplete="username"
            placeholder={
              isProfessor ? "nombre@usc.edu.co" : "nombre@correo.com"
            }
            pattern={isProfessor ? "[^@\\s]+@usc\\.edu\\.co" : undefined}
            title={
              isProfessor ? "Usa un correo terminado en @usc.edu.co" : undefined
            }
            required
          />
          <label>Contraseña</label>
          <input
            id="password"
            name="password"
            type="password"
            autoComplete="current-password"
            required
          />
          <label>
            <input
              type="checkbox"
              checked={aceptado}
              onChange={(e) => setAceptado(e.target.checked)}
              required
            />
            <span>
              Acepto el tratamiento de mis datos personales y los terminos de
              uso academico de la Universidad Santiago de Cali.
            </span>
          </label>
          <button
            type="submit"
            disabled={isSubmitting}
            style={{
              width: "100%",
              borderRadius: 12,
              border: "none",
              padding: 10,
              fontSize: 18,
              fontWeight: "bold",
              backgroundColor: aceptado ? "#E6397E" : "#D6DCE3",
              cursor: aceptado ? "pointer" : "not-allowed",
            }}
          >
            {isSubmitting ? "Iniciando sesión..." : "Iniciar sesión"}
          </button>
          {loginError && <p role="alert">{loginError}</p>}
          <p className="auth_link">
            ¿Aún no tienes una cuenta?<Link to="/register"> Registrate </Link>
          </p>
        </form>
      </section>
    </div>
  );
}

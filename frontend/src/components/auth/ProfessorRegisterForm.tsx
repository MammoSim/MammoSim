import { useState, type FormEvent } from "react";
import "../../styles/AuthPage.css";
import { Link, useNavigate } from "react-router-dom";
import { registerUser } from "../../services/authServices";

const Facultades = [
  "Salud",
  "Ingeniería",
  "Ciencias Económicas y Empresariales",
  "Ciencias Básicas",
  "Humanidades y Artes",
  "Ciencias Políticas",
  "Derecho",
  "Educación",
  "No aplica",
];

export default function ProfessorForm() {
  const navigate = useNavigate();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [aceptado, setAceptado] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmitting) return;

    setError("");

    const fields = new FormData(event.currentTarget);
    setIsSubmitting(true);

    try {
      const result = await registerUser({
        email: String(fields.get("email") ?? "").trim(),
        password: String(fields.get("password") ?? ""),
        nombre_completo: String(fields.get("name") ?? "").trim(),
        carrera: String(fields.get("carrera") ?? "").trim(),
        semestre: null,
        acepto_terminos: fields.get("aceptoTerminos") === "on",
      });

      navigate("/login", {
        replace: true,
        state: {
          registrationMessage:
            result.aviso ??
            (result.usuario.rol === "docente"
              ? "Cuenta de docente registrada. Ya puedes iniciar sesión"
              : "La cuenta se registró como estudiante. No se encuentra en la lista de autorizados como docente"),
        },
      });
    } catch (error) {
      setError(
        error instanceof Error
          ? error.message
          : "No se pudo completar el registro",
      );
    } finally {
      setIsSubmitting(false);
    }
  }
  return (
    <section className="auth">
      <h1>Crea tu cuenta</h1>
      <p>Registrate para comenzar a practicar</p>

      <form className="form_auth" onSubmit={handleSubmit}>
        <label htmlFor="name">Nombre completo</label>
        <input
          id="name"
          name="name"
          type="text"
          placeholder="Nombre y Apellido"
          required
        />
        <label htmlFor="email">Correo Institucional</label>
        <input
          id="email"
          name="email"
          type="email"
          autoComplete="email"
          placeholder="nombre@usc.edu.co"
          required
        />
        <label htmlFor="professor-password">Contraseña</label>
        <input
          id="professor-password"
          type="password"
          name="password"
          autoComplete="new-password"
          minLength={6}
          required
        />

        <label htmlFor="carrera">Facultad</label>
        <select id="carrera" name="carrera" defaultValue="" required>
          <option value="" disabled>
            Seleccione su Facultad
          </option>
          {Facultades.map((carrera) => (
            <option key={carrera} value={carrera}>
              {carrera}
            </option>
          ))}
        </select>

        <label>
          <input
            type="checkbox"
            name="aceptoTerminos"
            checked={aceptado}
            onChange={(event) => setAceptado(event.target.checked)}
          />
          <span>
            Acepto el tratamiento de mis datos personales y los terminos de uso
            academico de la Universidad Santiago de Cali.
          </span>
        </label>
        <button
          type="submit"
          disabled={!aceptado || isSubmitting}
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
          {isSubmitting ? "Creando cuenta..." : "Registrarse"}
        </button>

        {error && <p role="alert">{error}</p>}
        <p className="auth_link">
          ¿Ya tienes una cuenta? <Link to="/login">Inicia Sesión</Link>
        </p>
      </form>
    </section>
  );
}

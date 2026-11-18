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

const Semestres = Array.from({ length: 14 }, (_, index) => index + 1);

export default function StudentForm() {
  const [aceptado, setAceptado] = useState(false);

  const navigate = useNavigate();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [registrationError, setRegistrationError] = useState("");

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setRegistrationError("");
    const fields = new FormData(event.currentTarget);
    const semestre = Number(fields.get("semestre"));

    if (!Number.isInteger(semestre) || semestre < 1 || semestre > 14) {
      setRegistrationError("Selecciona un semestre entre 1 y 14");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await registerUser({
        email: String(fields.get("email") ?? ""),
        password: String(fields.get("password") ?? ""),
        nombre_completo: String(fields.get("name") ?? "").trim(),
        carrera: String(fields.get("carrera") ?? ""),
        semestre,
        acepto_terminos: fields.get("aceptoTerminos") === "on",
      });

      navigate("/login", {
        replace: true,
        state: {
          registrationMessage:
            result.aviso ?? "Cuenta registrada. Ya puedes inciar sesión",
        },
      });
    } catch (error) {
      setRegistrationError(
        error instanceof Error
          ? error.message
          : "No se pudo completar el registro.",
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

        <label htmlFor="email">Correo electrónico</label>
        <input
          id="email"
          name="email"
          type="email"
          placeholder="nombre@correo.com"
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

        <label>Semestre</label>
        <select id="semestre" name="semestre" defaultValue="" required>
          <option value="" disabled>
            Seleccione su semestre
          </option>
          {Semestres.map((semestre) => (
            <option key={semestre} value={semestre}>
              {semestre}
            </option>
          ))}
        </select>

        <label htmlFor="password">Contraseña</label>
        <input
          id="password"
          type="password"
          name="password"
          autoComplete="new-password"
          required
        />

        <label>
          <input
            type="checkbox"
            name="aceptoTerminos"
            required
            checked={aceptado}
            onChange={(e) => setAceptado(e.target.checked)}
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
          {isSubmitting ? "Creando cuenta..." : "Crear cuenta"}
        </button>
        {registrationError && <p role="alert">{registrationError}</p>}

        <p className="auth_link">
          ¿Ya tienes una cuenta? <Link to="/login">Inicia Sesión</Link>
        </p>
      </form>
    </section>
  );
}

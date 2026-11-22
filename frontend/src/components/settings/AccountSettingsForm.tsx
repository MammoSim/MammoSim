import { useState, type FormEvent } from "react";
import { changePassword } from "../../services/authServices";
import { FirebaseError } from "firebase/app";

type AccountSettingsFormProps = {
  name: string;
  email: string;
};

export default function AccountSettingsForm({
  name,
  email,
}: AccountSettingsFormProps) {
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (submitting) return;

    const form = event.currentTarget;
    const fields = new FormData(form);

    const currentPassword = String(fields.get("currentPassword") ?? "");
    const newPassword = String(fields.get("newPassword") ?? "");
    const confirmation = String(fields.get("confirmPassword") ?? "");

    setError("");
    setMessage("");

    if (newPassword !== confirmation) {
      setError("La nueva contraseña y la confirmación no coinciden");
      return;
    }

    if (currentPassword === newPassword) {
      setError("La nueva contraseña debe ser diferente de la actual");
      return;
    }

    setSubmitting(true);

    try {
      await changePassword(currentPassword, newPassword);
      form.reset();
      setMessage("Contraseña actualizada correctamente");
    } catch (error) {
      if (error instanceof FirebaseError) {
        switch (error.code) {
          case "auth/invalid-credential":
          case "auth/wrong-password":
            setError("No se pudo verificar la contraseña actual");
            break;
          case "auth/weak-password":
          case "auth/password-does-not-meet-requirements":
            setError(
              "La nueva contraseña no cumple con la política de seguridad",
            );
            break;
          case "auth/too-many-requests":
            setError(
              "No fue posible cambiar la contraseña. Vuelve a iniciar sesión e inténtalo de nuevo",
            );
        }
      } else {
        setError(
          error instanceof Error
            ? error.message
            : "No fue posible cambiar la contrasseña",
        );
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="account_setting_form" onSubmit={handleSubmit}>
      <h2>Información del perfíl</h2>
      <label htmlFor="fullname">Nombre completo</label>
      <input
        id="fullname"
        name="fullname"
        type="text"
        defaultValue={name}
        readOnly
      />

      <label htmlFor="email">Correo electrónico</label>
      <input id="email" name="email" type="email" value={email} readOnly />

      <h4>Cambiar contraseña</h4>

      <label htmlFor="currentPassword">Contraseña actual</label>
      <input
        id="currentPassword"
        name="currentPassword"
        type="password"
        autoComplete="current-password"
        disabled={submitting}
        required
      />

      <label htmlFor="newPassword">Nueva contraseña</label>
      <input
        id="newPassword"
        name="newPassword"
        type="password"
        autoComplete="new-password"
        minLength={6}
        disabled={submitting}
        required
      />
      <label htmlFor="confirmPassword">Confirmar nueva contraseña</label>
      <input
        id="confirmPassword"
        name="confirmPassword"
        type="password"
        autoComplete="new-password"
        minLength={6}
        disabled={submitting}
        required
      />

      <div className="button_container">
        <button type="submit" disabled={submitting} className="submit_form_setting">
          {submitting ? "Actualizando..." : "Cambiar contraseña"}
        </button>
      </div>
      {error && <p role="alert">{error}</p>}
      {message && <p role="status">{message}</p>}
    </form>
  );
}

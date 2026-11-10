type AccountSettingsFormProps = {
  name: string;
  email: string;
};

export default function AccountSettingsForm({
  name,
  email,
}: AccountSettingsFormProps) {
  return (
    <form className="account_setting_form" onSubmit={(e) => e.preventDefault()}>
        <h2>Información del perfíl</h2>
      <label htmlFor="fullname">Nombre completo</label>
      <input
        id="fullname"
        name="fullname"
        type="text"
        autoComplete="name"
        defaultValue={name}
        required
      />

      <label htmlFor="email">Correo</label>
      <input
        id="email"
        name="email"
        type="email"
        autoComplete="email"
        value={email}
        readOnly
      />

      <label htmlFor="currentPassword">Contraseña actual</label>
      <input
        id="currentPassword"
        name="currentPassword"
        type="password"
        autoComplete="current-password"
      />

      <label htmlFor="newPassword">Nueva contraseña</label>
      <input
        id="newPassword"
        name="newPassword"
        type="password"
        autoComplete="new-password"
      />
    </form>
  );
}

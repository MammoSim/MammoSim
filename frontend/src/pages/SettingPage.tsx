import Sidebar from "../components/layout/Sidebar";
import SidebarProfessor from "../components/layout/SidebarProfessor";
import AccountSettingsForm from "../components/settings/AccountSettingsForm";
import { useProfile } from "../hooks/useProfile";
import "../styles/Setting.css";

export default function SettingPage() {
  const { usuario, loading, error } = useProfile();

  if (loading) {
    return <p role="status">Cargando ajustes...</p>;
  }

  if (error || !usuario) {
    return <p role="alert">{error || "No fue posible cargar el pefil"}</p>;
  }
  return (
    <div className="sidebar_container">
      {usuario?.rol === "docente" ? <SidebarProfessor /> : <Sidebar />}
      <main className="account_info_container">
        <h1>Ajustes de cuenta</h1>
        <AccountSettingsForm
          key={usuario.id_usuario}
          name={usuario.nombre_completo}
          email={usuario.correo}
        />
        
      </main>
    </div>
  );
}

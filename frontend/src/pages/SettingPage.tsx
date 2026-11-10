import Sidebar from "../components/layout/Sidebar";
import AccountSettingsForm from "../components/settings/AccountSettingsForm";
import "../styles/Setting.css";

export default function SettingPage() {
  return (
    <div className="sidebar_container">
      <Sidebar />
      <main className="account_info_container">
        <h1>Ajustes de cuenta</h1>
        <AccountSettingsForm name="zaira" email="zairame28@gmail.com" />
        <div className="button_container">
          <button type="submit" form="account-setting-form" className="submit_form_setting">
            Guardar cambios
          </button>
        </div>
      </main>
    </div>
  );
}

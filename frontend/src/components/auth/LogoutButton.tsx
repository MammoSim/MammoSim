import { LogOut } from "lucide-react";
import "../../styles/Sidebar.css";
import { signOut } from "firebase/auth";
import { auth } from "../../config/firebase";
import { useNavigate } from "react-router-dom";
import { useState } from "react";
export default function LogoutButton() {
  const navigate = useNavigate();
  const [error, setError] = useState("");
  const [closing, setClosing] = useState(false);

  async function handleLogout() {
    if (closing) return;

    setClosing(true);
    setError("");

    try {
      await signOut(auth);
      navigate("/login", { replace: true });
    } catch {
      setError("No fue posible cerrar la sesión");
      setClosing(false);
    }
  }
  return (
    <>
      {error && <p role="alert">{error}</p>}
      <button type="button" className="sidebar_logout" onClick={handleLogout}>
        <LogOut className="icons" aria-hidden="true" />
        {closing ? "Cerrando..." : "Cerrar sesión"}
      </button>
    </>
  );
}

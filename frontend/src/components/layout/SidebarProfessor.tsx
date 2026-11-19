import { NavLink } from "react-router-dom";
import "../../styles/Sidebar.css";
import logo from "../../assets/Logo-bg.png";
import { Folder, Settings, StickyNotes, UserCircle } from "lucide-react";
import LogoutButton from "../auth/LogoutButton";

export default function SidebarProfessor() {
  return (
    <nav className="sidebar">
      <div className="logo_container">
        <img src={logo} alt="MammoSim" />
        <h5>Simulador de casos clínicos</h5>
      </div>
      <ul>
        <li>
          <NavLink to="/docente">
            <UserCircle />
            Panel docente
          </NavLink>
        </li>
        <li>
            <NavLink to="/cases_aviable">
                <Folder/>
                Casos clínicos
            </NavLink>
        </li>
        <li>
          <NavLink to="/history">
            <StickyNotes />
            Historial
          </NavLink>
        </li>
        <li>
          <NavLink to="/settings">
            <Settings />
            Ajustes
          </NavLink>
        </li>
      </ul>
      <div className="sidebar_footer">
        <LogoutButton />
      </div>
    </nav>
  );
}

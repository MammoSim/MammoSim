import { useProfile } from "../../hooks/useProfile";
import type { Usuario } from "../../services/authServices";
import { Navigate, Outlet } from "react-router-dom";
type ProtectedRouteProps = {
  allowedRoles: Usuario["rol"][];
};

export default function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const { usuario, loading, error } = useProfile();

  if (loading) {
    return <p role="status">Verificando acceso... </p>;
  }

  if (error || !usuario) {
    return (
      <Navigate
        to="/login"
        replace
        state={{ registrationMessage: "Inicia sesión nuevamente para acceder" }}
      />
    );
  }

  if (!allowedRoles.includes(usuario.rol)) {
    return (
      <Navigate
        to={usuario.rol === "docente" ? "/docente" : "/cases"}
        replace
      />
    );
  }

  return <Outlet />;
}

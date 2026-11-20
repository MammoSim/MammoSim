import { Navigate, Route, Routes } from "react-router-dom";
import "./App.css";
import RegisterPage from "./pages/RegisterPage";
import LoginPage from "./pages/LoginPage";
import CasesPage from "./pages/CasesPage";
import HistorialPage from "./pages/HistorialPage";
import GuidePage from "./pages/GuidePage";
import SettingPage from "./pages/SettingPage";
import SimulationPage from "./pages/SimulationPage";
import ProfessorPage from "./pages/ProfessorPage";
import ProfessorHistorialPage from "./pages/docente/history/ProfessorHistorialPage";

import ProtectedRoute from "./components/auth/ProtectedRoute";
import CasesProfessorPage from "./pages/docente/cases/CasesProfessorPage";

function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/login" replace />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      {/* Páginas a las que accede el estudiante */}
      <Route element={<ProtectedRoute allowedRoles={["estudiante"]} />}>
        <Route path="/cases" element={<CasesPage />} />
        <Route path="/historial" element={<HistorialPage />} />
        <Route path="/guide" element={<GuidePage />} />
        <Route path="/simulation/:caseId" element={<SimulationPage />} />
      </Route>

      {/* Páginas a las que accede el docente */}
      <Route element={<ProtectedRoute allowedRoles={["docente"]} />}>
        <Route path="/docente" element={<ProfessorPage />} />
        <Route path="/docente/historial" element={<ProfessorHistorialPage />} />
        <Route path="/docente/cases" element={<CasesProfessorPage/>}/>
      </Route>

      {/* Páginas a la que acceden ambos roles */}
      <Route
        element={<ProtectedRoute allowedRoles={["estudiante", "docente"]} />}
      >
        <Route path="/setting" element={<SettingPage />} />
      </Route>

      <Route path="*" element={<Navigate to="/login" replace />}>
      </Route>
    </Routes>
  );
}

export default App;

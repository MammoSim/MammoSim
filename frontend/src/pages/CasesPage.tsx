import { useEffect, useState } from "react";
import CaseCatalog from "../components/cases/CaseCatalog";
import "../styles/CasesCatalog.css";
import "../styles/CaseDetails.css";
import CaseDetails from "../components/cases/CaseDetails";
import type { clinicalCase } from "../types/ClinicalCase";
import Sidebar from "../components/layout/Sidebar";
import "../styles/Sidebar.css";
import PageHeader from "../components/layout/PageHeader";
import { getCases } from "../services/casesServices";
import { useProfile } from "../hooks/useProfile";
import AlertDialog from "../components/alert/AlertDialog";

export default function CasesPage() {
  const [cases, setCases] = useState<clinicalCase[]>([]);
  const [selectedCase, setSelectedCase] = useState<clinicalCase | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const {
    usuario,
    loading: profileLoading,
    error: profileError,
  } = useProfile();

  const [isOpen, setIsOpen] = useState(true);

  useEffect(() => {
    const controller = new AbortController();

    async function loadCases() {
      try {
        const result = await getCases(controller.signal);

        if (!controller.signal.aborted) {
          setCases(result);
        }
      } catch (error) {
        if (!controller.signal.aborted) {
          setError(
            error instanceof Error
              ? error.message
              : "No fue posible cargar los casos",
          );
        }
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      }
    }

    void loadCases();

    return () => controller.abort();
  }, []);

  return (
    <div className="sidebar_container">
      <Sidebar />
      <main className="content">
        <AlertDialog isOpen={isOpen} onClose={() => setIsOpen(false)}/>

        {/* Cargar la información del perfil */}
        {profileLoading ? (
          <p role="status">Cargando perfil...</p>
        ) : profileError ? (
          <p role="alert">{profileError}</p>
        ): usuario ? (
          <PageHeader
            title="Selecciona un caso clínico"
            description="Cada caso presenta una situación clínica diferente para desarrollar tus habilidades de análisis y decisión."
            name={usuario.nombre_completo}
            carrera={usuario.carrera}
          />
        ) : null}

        {/* Cargar los casos*/}
        {isLoading ? (
          <p role="status">Cargando casos...</p>
        ) : error ? (
          <p role="alert">{error}</p>
        ) : cases.length === 0 ? (
          <p>No hay casos disponibles en el momento</p>
        ) : (
          <div className="cases_container">
            <CaseCatalog cases={cases} onSelectCase={setSelectedCase} />
            {selectedCase && <CaseDetails clinicalCase={selectedCase} />}
          </div>
        )}
      </main>
    </div>
  );
}

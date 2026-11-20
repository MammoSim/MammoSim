import { useEffect, useState } from "react";
import SidebarProfessor from "../../../components/layout/SidebarProfessor";
import type { clinicalCase } from "../../../types/ClinicalCase";
import { useProfile } from "../../../hooks/useProfile";
import { getCases } from "../../../services/casesServices";
import PageHeader from "../../../components/layout/PageHeader";
import CaseCatalog from "../../../components/cases/CaseCatalog";
import CaseDetails from "../../../components/cases/CaseDetails";

export default function CasesProfessorPage() {

    const [cases, setCases] = useState<clinicalCase[]>([]);
    const [selectedCase, setSelectedCase] = useState<clinicalCase | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    const {usuario, loading: profileLoading, error: profileError,} = useProfile();

    useEffect(() => {
        const controller = new AbortController();

        async function loadCases() {
            try {
                const data = await getCases(controller.signal);

                if(!controller.signal.aborted){
                    setCases(data);
                }
                
            } catch(error){
                if(!controller.signal.aborted) {
                    setError(error instanceof Error ? error.message : "No fue posible consultar los casos disponibles",);
                }
            } finally {
                if(!controller.signal.aborted){
                    setLoading(false);
                }
            }
        }

        void loadCases();

        return () => controller.abort();
    }, []);
  return (
    <div className="sidebar_container">
      <SidebarProfessor />
      <main className="content">
        {profileLoading ? (
            <p role="status">Cargando perfil...</p>
        ): profileError ? (
            <p role="alert">{profileError}</p>
        ) : usuario ? (
            <PageHeader title="Casos disponibles" description="Consulta los casos que se encuentran disponibles para las práticas de tu estudiantes" name={usuario.nombre_completo} carrera={usuario.carrera}/>
        ) : null}


        {loading ? (
            <p role="status">Cargando casos...</p>
        ): error ? (
            <p role="alert">{error}</p>
        ) : cases.length === 0 ? (
            <p>No hay casos disponibles en el momento</p>
        ) : (
            <div className="cases_container">
                <CaseCatalog cases={cases} onSelectCase={setSelectedCase}/>

                {selectedCase && (
                    <CaseDetails clinicalCase={selectedCase} canStartSimulation={false}
                    />
                )}
            </div>
        )}
      </main>
    </div>
  );
}

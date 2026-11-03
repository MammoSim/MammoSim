import CaseCatalog from "../components/cases/CaseCatalog";
import { mockCases } from "../components/data/mockCases";
import '../styles/CasesCatalog.css'

export default function CasesPage(){
    const cases=mockCases;
    return(
        <main className="content">
            {cases.length === 0 ? (
                <p>No hay casos disponibles en el momento</p>
            ) : (
                <CaseCatalog cases={mockCases}/>
            )}
        </main>
    );

}
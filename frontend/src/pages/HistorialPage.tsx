import Sidebar from "../components/layout/Sidebar";
import '../styles/CasesCatalog.css'
export default function HistorialPage(){
    return(
        <div className="sidebar_container">
            <Sidebar/>
            <main className="content">
                <h1>HISTORIAL</h1>
            </main>
        </div>
    );
}
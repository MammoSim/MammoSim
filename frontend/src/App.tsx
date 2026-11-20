import { Navigate, Route, Routes } from 'react-router-dom'
import './App.css'
import RegisterPage from './pages/RegisterPage'
import LoginPage from './pages/LoginPage'
import CasesPage from './pages/CasesPage'
import HistorialPage from './pages/HistorialPage'
import GuidePage from './pages/GuidePage'
import SettingPage from './pages/SettingPage'
import SimulationPage from './pages/SimulationPage'
import ProfessorPage from './pages/ProfessorPage'
import ProfessorHistorialPage from './pages/docente/history/ProfessorHistorialPage'

function App() {

  return (
    <Routes>
      <Route path='/' element={<Navigate to="/login" replace />}/>
      <Route path='/login' element={<LoginPage/>}/>
      <Route path='/register' element={<RegisterPage/>}/>
      <Route path='/cases' element={<CasesPage/>}/>
      <Route path='/historial' element={<HistorialPage/>}/>
      <Route path='/guide' element={<GuidePage/>}/>
      <Route path='/setting' element={<SettingPage/>}/>
      <Route path='/simulation/:caseId' element={<SimulationPage/>}/>
      <Route path='/docente' element={<ProfessorPage/>}/>
      <Route path='/docente/historial' element={<ProfessorHistorialPage/>}/>
    </Routes>
  )
}

export default App

import { Navigate, Route, Routes } from 'react-router-dom'
import './App.css'
import RegisterPage from './pages/RegisterPage'
import LoginPage from './pages/LoginPage'
import CasesPage from './pages/CasesPage'

function App() {

  return (
    <Routes>
      <Route path='/' element={<Navigate to="/login" replace />}/>
      <Route path='/login' element={<LoginPage/>}/>
      <Route path='/register' element={<RegisterPage/>}/>
      <Route path='/cases' element={<CasesPage/>}/>
    </Routes>
  )
}

export default App

import { Link, Route, Routes } from 'react-router'
import './components/schemes.css'
import SchemeFormPage from './features/schemes/SchemeFormPage'
import SchemeListPage from './features/schemes/SchemeListPage'

function NotFoundPage() {
  return (
    <main className="page">
      <h1>Page not found</h1>
      <Link to="/">Back to schemes</Link>
    </main>
  )
}

function App() {
  return (
    <Routes>
      <Route path="/" element={<SchemeListPage />} />
      <Route path="/schemes/new" element={<SchemeFormPage />} />
      <Route path="/schemes/:id/edit" element={<SchemeFormPage />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

export default App

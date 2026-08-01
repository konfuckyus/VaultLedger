import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '@/auth'

export function ProtectedRoute() {
  const { isAuthenticated, isBootstrapping } = useAuth()
  const location = useLocation()

  if (isBootstrapping) {
    return (
      <div className="page-center">
        <p style={{ color: '#e7efe6' }}>Oturum kontrol ediliyor…</p>
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return <Outlet />
}

export function AdminRoute() {
  const { isAdmin, isBootstrapping } = useAuth()

  if (isBootstrapping) {
    return (
      <div className="page-center">
        <p style={{ color: '#e7efe6' }}>Oturum kontrol ediliyor…</p>
      </div>
    )
  }

  if (!isAdmin) {
    return <Navigate to="/accounts" replace />
  }

  return <Outlet />
}

export function GuestRoute() {
  const { isAuthenticated, isBootstrapping } = useAuth()

  if (isBootstrapping) {
    return (
      <div className="page-center">
        <p style={{ color: '#e7efe6' }}>Oturum kontrol ediliyor…</p>
      </div>
    )
  }

  if (isAuthenticated) {
    return <Navigate to="/accounts" replace />
  }

  return <Outlet />
}

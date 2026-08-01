import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '@/auth'

export function AppShell() {
  const { user, isAdmin, logout } = useAuth()

  return (
    <div className="shell">
      <header className="topbar">
        <div className="brand-lockup">
          <span className="brand-mark" aria-hidden />
          <div>
            <p className="brand">VaultLedger</p>
            <p className="brand-sub">Kapalı devre bakiye</p>
          </div>
        </div>

        <nav className="nav" aria-label="Ana menü">
          <NavLink to="/accounts">Dashboard</NavLink>
          <NavLink to="/transactions">İşlemler</NavLink>
          <NavLink to="/cards">Kartlarım</NavLink>
          {isAdmin ? <NavLink to="/admin">Admin</NavLink> : null}
        </nav>

        <div className="session">
          <div className="session-user">
            <span className="session-greeting">
              Hoş geldin, {user?.fullName || user?.email}
            </span>
            {user?.fullName ? <span className="session-email">{user.email}</span> : null}
          </div>
          <button type="button" className="btn ghost" onClick={() => void logout()}>
            Çıkış
          </button>
        </div>
      </header>

      <main className="main">
        <Outlet />
      </main>
    </div>
  )
}

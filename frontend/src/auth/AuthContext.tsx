import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { authApi } from '@/api/auth'
import { getErrorDetail, tokenStore, type StoredUser } from '@/api/client'
import type { AuthResponse, LoginRequest, RegisterRequest } from '@/types/api'

type AuthContextValue = {
  user: StoredUser | null
  isAuthenticated: boolean
  isAdmin: boolean
  isBootstrapping: boolean
  login: (input: LoginRequest) => Promise<void>
  register: (input: RegisterRequest) => Promise<void>
  logout: () => Promise<void>
  applySession: (auth: AuthResponse) => void
  refreshProfile: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<StoredUser | null>(null)
  const [isBootstrapping, setIsBootstrapping] = useState(true)

  const applySession = useCallback((auth: AuthResponse) => {
    tokenStore.saveSession(auth)
    setUser({
      userId: auth.userId,
      email: auth.email,
      fullName: auth.fullName ?? '',
      role: auth.role,
    })
  }, [])

  useEffect(() => {
    const stored = tokenStore.getUser()
    setUser(stored)

    // Refresh profile (FullName) when a session already exists in localStorage.
    if (tokenStore.getAccess()) {
      void authApi
        .me()
        .then((me) => {
          const next = {
            userId: me.userId,
            email: me.email,
            fullName: me.fullName,
            role: me.role,
          }
          localStorage.setItem('vl.user', JSON.stringify(next))
          setUser(next)
        })
        .catch(() => {
          // Keep stored session if /me fails (offline / expired handled elsewhere).
        })
        .finally(() => setIsBootstrapping(false))
    } else {
      setIsBootstrapping(false)
    }

    const onExpired = () => setUser(null)
    window.addEventListener('vl:auth-expired', onExpired)
    return () => window.removeEventListener('vl:auth-expired', onExpired)
  }, [])

  const login = useCallback(
    async (input: LoginRequest) => {
      const auth = await authApi.login(input)
      applySession(auth)
    },
    [applySession],
  )

  const register = useCallback(
    async (input: RegisterRequest) => {
      const auth = await authApi.register(input)
      applySession(auth)
    },
    [applySession],
  )

  const logout = useCallback(async () => {
    const refreshToken = tokenStore.getRefresh()
    try {
      if (refreshToken) await authApi.logout({ refreshToken })
    } catch {
      // Best-effort logout — always clear local session.
    } finally {
      tokenStore.clear()
      setUser(null)
    }
  }, [])

  const refreshProfile = useCallback(async () => {
    const me = await authApi.me()
    const next = {
      userId: me.userId,
      email: me.email,
      fullName: me.fullName,
      role: me.role,
    }
    localStorage.setItem('vl.user', JSON.stringify(next))
    setUser(next)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: Boolean(user && tokenStore.getAccess()),
      isAdmin: user?.role === 'Admin',
      isBootstrapping,
      login,
      register,
      logout,
      applySession,
      refreshProfile,
    }),
    [user, isBootstrapping, login, register, logout, applySession, refreshProfile],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}

export { getErrorDetail }

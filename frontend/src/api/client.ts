import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { getErrorDetail as resolveErrorDetail } from '@/api/errors'
import type { AuthResponse } from '@/types/api'

const ACCESS_KEY = 'vl.accessToken'
const REFRESH_KEY = 'vl.refreshToken'
const USER_KEY = 'vl.user'

export type StoredUser = {
  userId: string
  email: string
  fullName: string
  role: string
}

export const tokenStore = {
  getAccess: () => localStorage.getItem(ACCESS_KEY),
  getRefresh: () => localStorage.getItem(REFRESH_KEY),
  getUser: (): StoredUser | null => {
    const raw = localStorage.getItem(USER_KEY)
    if (!raw) return null
    try {
      return JSON.parse(raw) as StoredUser
    } catch {
      return null
    }
  },
  saveSession: (auth: AuthResponse) => {
    localStorage.setItem(ACCESS_KEY, auth.accessToken)
    localStorage.setItem(REFRESH_KEY, auth.refreshToken)
    localStorage.setItem(
      USER_KEY,
      JSON.stringify({
        userId: auth.userId,
        email: auth.email,
        fullName: auth.fullName ?? '',
        role: auth.role,
      } satisfies StoredUser),
    )
  },
  clear: () => {
    localStorage.removeItem(ACCESS_KEY)
    localStorage.removeItem(REFRESH_KEY)
    localStorage.removeItem(USER_KEY)
  },
}

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '',
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = tokenStore.getAccess()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

let refreshPromise: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
  const refreshToken = tokenStore.getRefresh()
  if (!refreshToken) return null

  try {
    // Use bare axios (not `api`) so a 401 on refresh does not re-enter this interceptor.
    const { data } = await axios.post<AuthResponse>(
      `${import.meta.env.VITE_API_BASE_URL ?? ''}/auth/refresh-token`,
      { refreshToken },
      { headers: { 'Content-Type': 'application/json' } },
    )
    tokenStore.saveSession(data)
    return data.accessToken
  } catch {
    tokenStore.clear()
    return null
  }
}

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as InternalAxiosRequestConfig & { _retry?: boolean }
    if (error.response?.status !== 401 || !original || original._retry) {
      return Promise.reject(error)
    }

    // Avoid refresh loop on auth endpoints
    const url = original.url ?? ''
    if (
      url.includes('/auth/login') ||
      url.includes('/auth/register') ||
      url.includes('/auth/refresh-token')
    ) {
      return Promise.reject(error)
    }

    original._retry = true

    // Single-flight: concurrent 401s share one refresh call (rotation-safe).
    refreshPromise ??= refreshAccessToken().finally(() => {
      refreshPromise = null
    })

    const newToken = await refreshPromise
    if (!newToken) {
      window.dispatchEvent(new Event('vl:auth-expired'))
      return Promise.reject(error)
    }

    // Retries reuse the same Axios config object → same Idempotency-Key header.
    original.headers.Authorization = `Bearer ${newToken}`
    return api(original)
  },
)

export function getErrorDetail(error: unknown): string {
  return resolveErrorDetail(error)
}

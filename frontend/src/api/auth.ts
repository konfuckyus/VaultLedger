import { api } from '@/api/client'
import type {
  AuthResponse,
  LoginRequest,
  MeResponse,
  RefreshTokenRequest,
  RegisterRequest,
  SetTransactionPinRequest,
} from '@/types/api'

export const authApi = {
  login: (body: LoginRequest) =>
    api.post<AuthResponse>('/auth/login', body).then((r) => r.data),

  register: (body: RegisterRequest) =>
    api.post<AuthResponse>('/auth/register', body).then((r) => r.data),

  refresh: (body: RefreshTokenRequest) =>
    api.post<AuthResponse>('/auth/refresh-token', body).then((r) => r.data),

  logout: (body: RefreshTokenRequest) =>
    api.post('/auth/logout', body).then((r) => r.data),

  me: () => api.get<MeResponse>('/auth/me').then((r) => r.data),

  setTransactionPin: (body: SetTransactionPinRequest) =>
    api.post('/auth/set-transaction-pin', body).then((r) => r.data),
}

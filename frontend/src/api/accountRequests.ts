import { api } from '@/api/client'
import type { AccountRequest } from '@/types/api'

export const accountRequestsApi = {
  me: () => api.get<AccountRequest[]>('/account-requests/me').then((r) => r.data),

  submit: (categoryId: string) =>
    api
      .post<AccountRequest>('/account-requests', { categoryId })
      .then((r) => r.data),
}

export const adminAccountRequestsApi = {
  pending: () =>
    api.get<AccountRequest[]>('/admin/account-requests/pending').then((r) => r.data),

  approve: (id: string) =>
    api.post<AccountRequest>(`/admin/account-requests/${id}/approve`).then((r) => r.data),

  reject: (id: string, reason: string) =>
    api
      .post<AccountRequest>(`/admin/account-requests/${id}/reject`, { reason })
      .then((r) => r.data),
}

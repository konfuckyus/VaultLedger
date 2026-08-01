import { api } from '@/api/client'
import type { TopUpRequest } from '@/types/api'

export const topUpRequestsApi = {
  me: () => api.get<TopUpRequest[]>('/topup-requests/me').then((r) => r.data),

  submit: (body: { accountId: string; amount: number; note?: string }) =>
    api.post<TopUpRequest>('/topup-requests', body).then((r) => r.data),
}

export const adminTopUpRequestsApi = {
  pending: () =>
    api
      .get<TopUpRequest[]>('/admin/topup-requests', { params: { status: 'Pending' } })
      .then((r) => r.data),

  approve: (id: string) =>
    api.post<TopUpRequest>(`/admin/topup-requests/${id}/approve`).then((r) => r.data),

  reject: (id: string, reason: string) =>
    api
      .post<TopUpRequest>(`/admin/topup-requests/${id}/reject`, { reason })
      .then((r) => r.data),
}

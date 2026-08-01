import { api } from '@/api/client'
import type {
  ApproveCardRequestResult,
  CardRequest,
  SubmitCardRequest,
} from '@/types/api'

export const cardRequestsApi = {
  me: () => api.get<CardRequest[]>('/card-requests/me').then((r) => r.data),

  submit: (body: SubmitCardRequest) =>
    api.post<CardRequest>('/card-requests', body).then((r) => r.data),
}

export const adminCardRequestsApi = {
  pending: () =>
    api.get<CardRequest[]>('/admin/card-requests/pending').then((r) => r.data),

  approve: (id: string) =>
    api
      .post<ApproveCardRequestResult>(`/admin/card-requests/${id}/approve`)
      .then((r) => r.data),

  reject: (id: string, reason: string) =>
    api
      .post<CardRequest>(`/admin/card-requests/${id}/reject`, { reason })
      .then((r) => r.data),
}

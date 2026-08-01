import { api } from '@/api/client'
import type { Card, IssueCardRequest } from '@/types/api'

export const cardsApi = {
  listByAccount: (accountId: string) =>
    api.get<Card[]>('/cards', { params: { accountId } }).then((r) => r.data),

  issue: (body: IssueCardRequest) =>
    api.post<Card>('/cards', body).then((r) => r.data),

  block: (id: string) =>
    api.patch<Card>(`/cards/${id}/block`).then((r) => r.data),

  unblock: (id: string) =>
    api.patch<Card>(`/cards/${id}/unblock`).then((r) => r.data),
}

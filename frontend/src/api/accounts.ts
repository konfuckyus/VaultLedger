import { api } from '@/api/client'
import type {
  Account,
  AccountLookup,
  AdminAccountListItem,
  Balance,
  CreateAccountRequest,
  PagedResult,
} from '@/types/api'

export const accountsApi = {
  me: () => api.get<Account[]>('/accounts/me').then((r) => r.data),

  balance: (id: string) =>
    api.get<Balance>(`/accounts/${id}/balance`).then((r) => r.data),

  /** Limited peer lookup (no balance) — for transfers. */
  lookup: (accountNumber: string) =>
    api.get<AccountLookup>(`/accounts/lookup/${accountNumber}`).then((r) => r.data),

  /** Admin full lookup including balance. */
  byNumber: (accountNumber: string) =>
    api.get<Account>(`/accounts/by-number/${accountNumber}`).then((r) => r.data),

  create: (body: CreateAccountRequest) =>
    api.post<Account>('/accounts', body).then((r) => r.data),

  /** Admin paginated list of all user accounts. */
  adminList: (page = 1, pageSize = 20, search?: string) =>
    api
      .get<PagedResult<AdminAccountListItem>>('/admin/accounts', {
        params: { page, pageSize, search: search || undefined },
      })
      .then((r) => r.data),
}

import { api } from '@/api/client'
import type {
  MoneyOperationRequest,
  SpendRequest,
  TransactionRecord,
  TransferRequest,
} from '@/types/api'

/**
 * Money-mutating calls require an Idempotency-Key created once at user submit time.
 * Callers must reuse the same key for any retry of that logical operation.
 * Do NOT generate a new UUID inside these helpers — that would break idempotency on retry.
 */
function withIdempotencyKey(idempotencyKey: string) {
  return {
    headers: {
      'Idempotency-Key': idempotencyKey,
    },
  }
}

export const transactionsApi = {
  spend: (body: SpendRequest, idempotencyKey: string) =>
    api
      .post<TransactionRecord>(
        '/transactions/spend',
        body,
        withIdempotencyKey(idempotencyKey),
      )
      .then((r) => r.data),

  topUp: (body: MoneyOperationRequest, idempotencyKey: string) =>
    api
      .post<TransactionRecord>(
        '/transactions/topup',
        body,
        withIdempotencyKey(idempotencyKey),
      )
      .then((r) => r.data),

  refund: (body: MoneyOperationRequest, idempotencyKey: string) =>
    api
      .post<TransactionRecord>(
        '/transactions/refund',
        body,
        withIdempotencyKey(idempotencyKey),
      )
      .then((r) => r.data),

  transfer: (body: TransferRequest, idempotencyKey: string) =>
    api
      .post<TransactionRecord>(
        '/transactions/transfer',
        body,
        withIdempotencyKey(idempotencyKey),
      )
      .then((r) => r.data),

  adjustment: (
    body: {
      accountId: string
      amount: number
      direction: 'Increase' | 'Decrease' | 0 | 1
      reason: string
    },
    idempotencyKey: string,
  ) =>
    api
      .post<TransactionRecord>(
        '/admin/transactions/adjustment',
        body,
        withIdempotencyKey(idempotencyKey),
      )
      .then((r) => r.data),

  history: (accountId: string, page = 1, pageSize = 20) =>
    api
      .get<TransactionRecord[]>('/transactions/history', {
        params: { accountId, page, pageSize },
      })
      .then((r) => r.data),
}

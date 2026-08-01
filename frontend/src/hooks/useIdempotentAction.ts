import axios from 'axios'
import { useCallback, useRef, useState } from 'react'
import { getErrorDetail } from '@/api/client'

export type UseIdempotentActionOptions<T> = {
  /** Default: "İşlem tamamlandı" */
  successMessage?: string | ((result: T) => string)
  onSuccess?: (result: T) => void | Promise<void>
  idleLabel?: string
  busyLabel?: string
}

/**
 * Shared submit guard for money-moving forms.
 * - Sync ref blocks double-submit before React re-renders
 * - One idempotency key per logical attempt; reused only on network / 409 errors
 * - Definitive backend errors clear the key so the next click gets a fresh UUID
 */
export function useIdempotentAction<T>(
  actionFn: (idempotencyKey: string) => Promise<T>,
  options: UseIdempotentActionOptions<T> = {},
) {
  const busyRef = useRef(false)
  const keyRef = useRef<string | null>(null)
  const actionRef = useRef(actionFn)
  const optionsRef = useRef(options)
  actionRef.current = actionFn
  optionsRef.current = options

  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const run = useCallback(async () => {
    if (busyRef.current) return
    busyRef.current = true
    setBusy(true)
    setError(null)
    setMessage(null)

    keyRef.current ??= crypto.randomUUID()
    const key = keyRef.current
    const opts = optionsRef.current

    try {
      const result = await actionRef.current(key)
      keyRef.current = null
      const ok =
        typeof opts.successMessage === 'function'
          ? opts.successMessage(result)
          : (opts.successMessage ?? 'İşlem tamamlandı')
      setMessage(ok)
      await opts.onSuccess?.(result)
      return result
    } catch (err) {
      if (!shouldReuseIdempotencyKey(err)) {
        keyRef.current = null
      }
      setError(getErrorDetail(err))
    } finally {
      busyRef.current = false
      setBusy(false)
    }
  }, [])

  return {
    run,
    busy,
    message,
    error,
    setMessage,
    setError,
    clearFeedback: () => {
      setMessage(null)
      setError(null)
    },
    submitLabel: busy
      ? (options.busyLabel ?? 'Gönderiliyor…')
      : (options.idleLabel ?? 'Gönder'),
  }
}

/** Network / no-response → keep key. Backend rejected with a response → new key next time. */
export function shouldReuseIdempotencyKey(error: unknown): boolean {
  if (!axios.isAxiosError(error)) {
    return false
  }
  if (!error.response) {
    return true
  }
  if (error.response.status === 409) {
    return true
  }
  return false
}

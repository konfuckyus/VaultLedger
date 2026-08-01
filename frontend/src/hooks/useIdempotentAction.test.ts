import { describe, expect, it } from 'vitest'
import axios from 'axios'
import { shouldReuseIdempotencyKey } from '@/hooks/useIdempotentAction'

describe('shouldReuseIdempotencyKey', () => {
  it('reuses key when there is no HTTP response (network)', () => {
    const err = new axios.AxiosError('Network Error')
    expect(shouldReuseIdempotencyKey(err)).toBe(true)
  })

  it('reuses key on 409 conflict (in progress)', () => {
    const err = new axios.AxiosError('Conflict')
    err.response = {
      status: 409,
      data: {},
      statusText: 'Conflict',
      headers: {},
      config: {} as never,
    }
    expect(shouldReuseIdempotencyKey(err)).toBe(true)
  })

  it('does not reuse key on definitive 4xx', () => {
    const err = new axios.AxiosError('Bad Request')
    err.response = {
      status: 400,
      data: {},
      statusText: 'Bad',
      headers: {},
      config: {} as never,
    }
    expect(shouldReuseIdempotencyKey(err)).toBe(false)
  })

  it('does not reuse key for non-axios errors', () => {
    expect(shouldReuseIdempotencyKey(new Error('kart yok'))).toBe(false)
  })
})

describe('busyRef double-submit guard', () => {
  it('blocks a second concurrent run via sync ref', async () => {
    let busy = false
    let calls = 0
    const run = async (fn: () => Promise<void>) => {
      if (busy) return
      busy = true
      calls += 1
      try {
        await fn()
      } finally {
        busy = false
      }
    }

    let release!: () => void
    const gate = new Promise<void>((r) => {
      release = r
    })

    const p1 = run(() => gate)
    const p2 = run(() => Promise.resolve())
    expect(calls).toBe(1)
    release()
    await Promise.all([p1, p2])
    expect(calls).toBe(1)
  })
})

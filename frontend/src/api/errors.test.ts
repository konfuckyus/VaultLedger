import { describe, expect, it } from 'vitest'
import axios from 'axios'
import { getErrorDetail } from '@/api/errors'

function axiosErr(status: number, data: unknown) {
  const err = new axios.AxiosError('fail')
  err.response = {
    status,
    data,
    statusText: 'x',
    headers: {},
    config: {} as never,
  }
  return err
}

describe('getErrorDetail', () => {
  it('shows friendly Turkish message for 422 insufficient balance', () => {
    const msg = getErrorDetail(
      axiosErr(422, {
        title: 'Insufficient Balance',
        detail: 'Account x has insufficient balance.',
      }),
    )
    expect(msg).toMatch(/Yetersiz bakiye/i)
  })

  it('prefers Turkish backend detail for 422', () => {
    const detail = 'Yetersiz bakiye: hesabınızda bu işlem için yeterli tutar yok. (İstenen: 10.00, Mevcut: 5.00)'
    expect(
      getErrorDetail(axiosErr(422, { title: 'Insufficient Balance', detail })),
    ).toBe(detail)
  })

  it('maps 403 to friendly or detail message', () => {
    expect(getErrorDetail(axiosErr(403, { title: 'Forbidden' }))).toMatch(/yetkiniz yok/i)
    expect(
      getErrorDetail(axiosErr(403, { detail: 'Cannot request a card for an account you do not own.' })),
    ).toContain('Cannot request')
  })

  it('maps 409 conflict', () => {
    expect(getErrorDetail(axiosErr(409, { title: 'Conflict' }))).toMatch(/çakışma|tekrar/i)
  })

  it('maps 400 validation field errors', () => {
    expect(
      getErrorDetail(
        axiosErr(400, {
          title: 'Validation Failed',
          errors: { Amount: ['Amount must be greater than 0'] },
        }),
      ),
    ).toContain('Amount must be greater than 0')
  })

  it('maps network errors', () => {
    expect(getErrorDetail(new axios.AxiosError('Network Error'))).toMatch(/Bağlantı/i)
  })
})

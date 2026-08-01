import axios from 'axios'

type ProblemBody = {
  detail?: string
  Detail?: string
  title?: string
  Title?: string
  errors?: Record<string, string[]>
}

/**
 * Maps ProblemDetails / Axios errors to a user-facing Turkish (or backend detail) message.
 * Prefer `detail` when present; fall back to status-specific friendly copy.
 */
export function getErrorDetail(error: unknown): string {
  if (!axios.isAxiosError(error)) {
    return error instanceof Error ? error.message : 'Beklenmeyen bir hata oluştu.'
  }

  if (!error.response) {
    return 'Bağlantı hatası. Lütfen tekrar deneyin.'
  }

  const status = error.response.status
  const data = error.response.data as ProblemBody | undefined
  const detail = (data?.detail ?? data?.Detail)?.trim()
  const title = (data?.title ?? data?.Title)?.trim()

  if (data?.errors && typeof data.errors === 'object') {
    const fieldMessages = Object.values(data.errors).flat().filter(Boolean)
    if (fieldMessages.length > 0) {
      return fieldMessages.join(' ')
    }
  }

  switch (status) {
    case 422:
      if (
        title === 'Insufficient Balance' ||
        /insufficient balance/i.test(detail ?? '') ||
        /insufficient balance/i.test(title ?? '')
      ) {
        return (
          detail && /yetersiz bakiye/i.test(detail)
            ? detail
            : 'Yetersiz bakiye: hesabınızda bu işlem için yeterli tutar yok.'
        )
      }
      return detail || title || 'İşlem gerçekleştirilemedi.'

    case 409:
      return (
        detail ||
        'İşlem şu an tamamlanamadı (çakışma veya devam eden istek). Lütfen kısa süre sonra tekrar deneyin.'
      )

    case 403:
      return detail || 'Bu işlem için yetkiniz yok.'

    case 401:
      return detail || 'Oturum geçersiz. Lütfen tekrar giriş yapın.'

    case 404:
      return detail || 'Kayıt bulunamadı.'

    case 400:
      return detail || title || 'Geçersiz istek. Lütfen alanları kontrol edin.'

    default:
      return detail || title || error.message || 'Beklenmeyen bir hata oluştu.'
  }
}

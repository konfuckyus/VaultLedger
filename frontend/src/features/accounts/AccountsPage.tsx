import { useCallback, useEffect, useMemo, useState } from 'react'
import { accountRequestsApi } from '@/api/accountRequests'
import { accountsApi } from '@/api/accounts'
import { authApi } from '@/api/auth'
import { budgetCategoriesApi } from '@/api/budgetCategories'
import { getErrorDetail } from '@/api/client'
import { topUpRequestsApi } from '@/api/topUpRequests'
import { useAuth } from '@/auth'
import { Alert, EmptyState, Field, Form, Money, Panel, StatusBadge } from '@/components/ui'
import type { Account, AccountRequest, BudgetCategory, TopUpRequest } from '@/types/api'

export function AccountsPage() {
  const { refreshProfile } = useAuth()
  const [accounts, setAccounts] = useState<Account[]>([])
  const [requests, setRequests] = useState<AccountRequest[]>([])
  const [categories, setCategories] = useState<BudgetCategory[]>([])
  const [selectedCategoryId, setSelectedCategoryId] = useState('')
  const [topUpRequests, setTopUpRequests] = useState<TopUpRequest[]>([])
  const [topUpAccountId, setTopUpAccountId] = useState('')
  const [topUpAmount, setTopUpAmount] = useState('100')
  const [topUpNote, setTopUpNote] = useState('')
  const [pin, setPin] = useState('')
  const [oldPin, setOldPin] = useState('')
  const [hasPin, setHasPin] = useState(false)
  const [pinEditorOpen, setPinEditorOpen] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [copiedId, setCopiedId] = useState<string | null>(null)

  const reload = useCallback(async () => {
    const [accs, reqs, topUps, cats, me] = await Promise.all([
      accountsApi.me(),
      accountRequestsApi.me(),
      topUpRequestsApi.me().catch(() => [] as TopUpRequest[]),
      budgetCategoriesApi.availableToMe(),
      authApi.me().catch(() => null),
    ])
    setAccounts(accs)
    setRequests(reqs)
    setTopUpRequests(topUps)
    setCategories(Array.isArray(cats) ? cats : [])
    setSelectedCategoryId((prev) => prev || (Array.isArray(cats) ? cats[0]?.id : '') || '')
    setTopUpAccountId((prev) => prev || accs[0]?.id || '')
    if (me) setHasPin(Boolean(me.hasTransactionPin))
  }, [])

  useEffect(() => {
    let alive = true
    void reload()
      .catch((err) => {
        if (alive) setError(getErrorDetail(err))
      })
      .finally(() => {
        if (alive) setLoading(false)
      })
    return () => {
      alive = false
    }
  }, [reload])

  const ownedCategoryIds = useMemo(
    () => new Set(accounts.map((a) => a.categoryId).filter(Boolean)),
    [accounts],
  )

  const pendingCategoryIds = useMemo(
    () =>
      new Set(
        requests.filter((r) => r.status === 'Pending').map((r) => r.categoryId),
      ),
    [requests],
  )

  const requestableCategories = categories.filter(
    (c) => !ownedCategoryIds.has(c.id) && !pendingCategoryIds.has(c.id),
  )

  async function submitRequest() {
    if (!selectedCategoryId) {
      setError('Kategori seçin.')
      return
    }
    setBusy(true)
    setError(null)
    setMessage(null)
    try {
      await accountRequestsApi.submit(selectedCategoryId)
      setMessage('Hesap talebiniz alındı.')
      await reload()
    } catch (err) {
      setError(getErrorDetail(err))
    } finally {
      setBusy(false)
    }
  }

  async function submitTopUpRequest() {
    if (!topUpAccountId) return
    setBusy(true)
    setError(null)
    setMessage(null)
    try {
      await topUpRequestsApi.submit({
        accountId: topUpAccountId,
        amount: Number(topUpAmount),
        note: topUpNote.trim() || undefined,
      })
      setMessage('Bakiye yükleme talebiniz alındı.')
      setTopUpNote('')
      await reload()
    } catch (err) {
      setError(getErrorDetail(err))
    } finally {
      setBusy(false)
    }
  }

  async function savePin() {
    setBusy(true)
    setError(null)
    setMessage(null)
    try {
      await authApi.setTransactionPin({
        pin,
        oldPin: hasPin ? oldPin : undefined,
      })
      setMessage(hasPin ? 'İşlem PIN\'i güncellendi.' : 'İşlem PIN\'i belirlendi.')
      setPin('')
      setOldPin('')
      setHasPin(true)
      setPinEditorOpen(false)
      await refreshProfile()
    } catch (err) {
      setError(getErrorDetail(err))
    } finally {
      setBusy(false)
    }
  }

  async function copyNumber(id: string, value: string) {
    try {
      await navigator.clipboard.writeText(value)
      setCopiedId(id)
      window.setTimeout(() => setCopiedId(null), 1600)
    } catch {
      setError('Kopyalanamadı.')
    }
  }

  const hasPendingTopUp = topUpRequests.some((r) => r.status === 'Pending')

  return (
    <div className="stack">
      <Panel
        title="VaultLedger"
        subtitle="VaultLedger ile Kapalı devre hesaplarınızı, kartlarınızı ve işlemlerinizi yönetin."
      >
        {error ? <Alert>{error}</Alert> : null}
        {message ? <Alert tone="ok">{message}</Alert> : null}
        {loading ? <p className="muted">Yükleniyor…</p> : null}

        {!loading && !hasPin ? (
          <Alert>İşlem PIN'i belirlemeniz gerekiyor (Spend / Transfer için).</Alert>
        ) : null}
      </Panel>

      {!loading ? (
        <Panel title="İşlem PIN'i">
          {hasPin && !pinEditorOpen ? (
            <div className="copy-row">
              <p style={{ margin: 0 }}>İşlem PIN'i: Ayarlandı ✓</p>
              <button
                type="button"
                className="btn"
                onClick={() => {
                  setPin('')
                  setOldPin('')
                  setPinEditorOpen(true)
                }}
              >
                Değiştir
              </button>
            </div>
          ) : (
            <Form
              onSubmit={() => {
                void savePin()
              }}
            >
              <p className="muted">
                {hasPin
                  ? 'PIN değiştirmek için mevcut PIN gerekir.'
                  : '4 haneli rakam — harcama ve transferde kullanılır.'}
              </p>
              {hasPin ? (
                <Field label="Mevcut PIN">
                  <input
                    type="password"
                    inputMode="numeric"
                    maxLength={4}
                    value={oldPin}
                    onChange={(e) => setOldPin(e.target.value.replace(/\D/g, '').slice(0, 4))}
                    required
                  />
                </Field>
              ) : null}
              <Field label={hasPin ? 'Yeni PIN' : 'PIN'}>
                <input
                  type="password"
                  inputMode="numeric"
                  maxLength={4}
                  value={pin}
                  onChange={(e) => setPin(e.target.value.replace(/\D/g, '').slice(0, 4))}
                  required
                  pattern="\d{4}"
                />
              </Field>
              <div className="btn-row">
                <button className="btn primary" type="submit" disabled={busy || pin.length !== 4}>
                  {busy ? 'Kaydediliyor…' : hasPin ? 'PIN Güncelle' : "İşlem PIN'i Belirle"}
                </button>
                {hasPin ? (
                  <button
                    type="button"
                    className="btn"
                    onClick={() => {
                      setPinEditorOpen(false)
                      setPin('')
                      setOldPin('')
                    }}
                  >
                    İptal
                  </button>
                ) : null}
              </div>
            </Form>
          )}
        </Panel>
      ) : null}

      {!loading && accounts.length === 0 ? (
        <Panel title="Hesap Talebi" subtitle="Kategori seçerek hesap açma talebi gönderin.">
          <EmptyState>
            Henüz hesabınız yok. Kategori seçip talep gönderin; admin onayından sonra hesap
            numaranız oluşur.
          </EmptyState>
          <Form
            onSubmit={() => {
              void submitRequest()
            }}
          >
            <Field label="Kategori">
              <select
                value={selectedCategoryId}
                onChange={(e) => setSelectedCategoryId(e.target.value)}
                required
              >
                {requestableCategories.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                    {c.defaultAllocatedAmount > 0
                      ? ` (varsayılan ${c.defaultAllocatedAmount} TRY)`
                      : ''}
                  </option>
                ))}
              </select>
            </Field>
            <button
              type="submit"
              className="btn primary"
              disabled={busy || requestableCategories.length === 0}
            >
              {busy ? 'Gönderiliyor…' : 'Hesap Talebinde Bulun'}
            </button>
          </Form>
          {requests[0] ? (
            <div className="request-card" style={{ marginTop: '1rem' }}>
              <div className="copy-row">
                <strong>
                  {requests[0].categoryName || 'Kategori'} · Talep
                </strong>
                <StatusBadge status={requests[0].status} />
              </div>
              <p className="muted">
                Gönderildi: {new Date(requests[0].requestedAt).toLocaleString('tr-TR')}
              </p>
              {requests[0].status === 'Rejected' && requests[0].rejectionReason ? (
                <Alert>Red sebebi: {requests[0].rejectionReason}</Alert>
              ) : null}
            </div>
          ) : null}
        </Panel>
      ) : null}

      {!loading && accounts.length > 0 ? (
        <Panel title="Hesaplarım" subtitle="Her kategori için ayrı hesap kartı.">
          <ul className="account-list">
            {accounts.map((account) => (
              <li key={account.id}>
                <div>
                  <p className="account-id">
                    {account.categoryName || 'Kategori'} · {account.accountNumber}
                  </p>
                  <p className="muted">
                    {account.status}
                    {account.isTransferable === false ? ' · Transfer kapalı' : ''}
                  </p>
                  <button
                    type="button"
                    className="btn"
                    onClick={() => void copyNumber(account.id, account.accountNumber)}
                  >
                    {copiedId === account.id ? 'Kopyalandı' : 'Numarayı kopyala'}
                  </button>
                </div>
                <Money amount={account.balance} currency={account.currency} />
              </li>
            ))}
          </ul>

          {requestableCategories.length > 0 ? (
            <Form
              onSubmit={() => {
                void submitRequest()
              }}
              style={{ marginTop: '1.25rem' }}
            >
              <Field label="Yeni kategori hesabı talep et">
                <select
                  value={selectedCategoryId}
                  onChange={(e) => setSelectedCategoryId(e.target.value)}
                  required
                >
                  {requestableCategories.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                      {c.defaultAllocatedAmount > 0
                        ? ` (varsayılan ${c.defaultAllocatedAmount} TRY)`
                        : ''}
                    </option>
                  ))}
                </select>
              </Field>
              <button type="submit" className="btn primary" disabled={busy}>
                {busy ? 'Gönderiliyor…' : 'Hesap Talebinde Bulun'}
              </button>
            </Form>
          ) : null}
        </Panel>
      ) : null}

      {!loading && accounts.length > 0 ? (
        <Panel
          title="Bakiye Talepleri"
          subtitle="Admin onayından sonra bakiyeniz artar. Direkt yükleme yalnızca admin panelinden yapılır."
        >
          <Form
            onSubmit={() => {
              void submitTopUpRequest()
            }}
          >
            <Field label="Hesap">
              <select
                value={topUpAccountId}
                onChange={(e) => setTopUpAccountId(e.target.value)}
                required
              >
                {accounts.map((a) => (
                  <option key={a.id} value={a.id}>
                    {a.categoryName || 'Hesap'} · {a.accountNumber}
                  </option>
                ))}
              </select>
            </Field>
            <Field label="Tutar">
              <input
                type="number"
                min="0.01"
                step="0.01"
                value={topUpAmount}
                onChange={(e) => setTopUpAmount(e.target.value)}
                required
              />
            </Field>
            <Field label="Not (opsiyonel)">
              <input
                value={topUpNote}
                onChange={(e) => setTopUpNote(e.target.value)}
                placeholder="Örn. Maaş avansı"
                maxLength={500}
              />
            </Field>
            <button
              className="btn primary"
              type="submit"
              disabled={busy || hasPendingTopUp}
            >
              {busy
                ? 'Gönderiliyor…'
                : hasPendingTopUp
                  ? 'Bekleyen bakiye talebiniz var'
                  : 'Bakiye Yükleme Talep Et'}
            </button>
          </Form>

          <ul className="tx-list" style={{ marginTop: '1rem' }}>
            {topUpRequests.map((r) => (
              <li key={r.id}>
                <div>
                  <div className="copy-row">
                    <strong>{r.amount} TRY</strong>
                    <StatusBadge status={r.status} />
                  </div>
                  {r.note ? <p className="muted">{r.note}</p> : null}
                  <p className="muted">{new Date(r.requestedAt).toLocaleString('tr-TR')}</p>
                  {r.status === 'Rejected' && r.rejectionReason ? (
                    <Alert>Red sebebi: {r.rejectionReason}</Alert>
                  ) : null}
                </div>
              </li>
            ))}
          </ul>
          {topUpRequests.length === 0 ? (
            <p className="empty">Henüz bakiye talebi yok.</p>
          ) : null}
        </Panel>
      ) : null}
    </div>
  )
}

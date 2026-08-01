import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { accountsApi } from '@/api/accounts'
import { cardRequestsApi } from '@/api/cardRequests'
import { cardsApi } from '@/api/cards'
import { getErrorDetail } from '@/api/client'
import {
  Alert,
  EmptyState,
  Field,
  Form,
  Panel,
  StatusBadge,
} from '@/components/ui'
import type { Account, Card, CardRequest } from '@/types/api'

const LABEL_PRESETS = ['Yemek', 'Kurumsal', 'Genel'] as const

function labelFromAccount(account: Account | undefined): {
  label: string
  customLabel: string
} {
  const name = account?.categoryName?.trim()
  if (!name) return { label: 'Genel', customLabel: '' }
  if ((LABEL_PRESETS as readonly string[]).includes(name)) {
    return { label: name, customLabel: '' }
  }
  return { label: '__custom', customLabel: name }
}

export function CardsPage() {
  const [accounts, setAccounts] = useState<Account[]>([])
  const [cards, setCards] = useState<Card[]>([])
  const [requests, setRequests] = useState<CardRequest[]>([])
  const [accountId, setAccountId] = useState('')
  const [label, setLabel] = useState('Genel')
  const [customLabel, setCustomLabel] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)

  const reload = useCallback(async (selectedAccountId?: string) => {
    const [accs, reqs] = await Promise.all([accountsApi.me(), cardRequestsApi.me()])
    setAccounts(accs)
    setRequests(reqs)
    const nextAccountId = selectedAccountId || accountId || accs[0]?.id || ''
    setAccountId(nextAccountId)
    const nextAccount = accs.find((a) => a.id === nextAccountId)
    const nextLabel = labelFromAccount(nextAccount)
    setLabel(nextLabel.label)
    setCustomLabel(nextLabel.customLabel)
    if (nextAccountId) {
      const list = await cardsApi.listByAccount(nextAccountId)
      setCards(list)
    } else {
      setCards([])
    }
  }, [accountId])

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
    // initial load only
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    if (!accountId) return
    let cancelled = false
    setCards([])
    void cardsApi
      .listByAccount(accountId)
      .then((list) => {
        if (!cancelled) setCards(list)
      })
      .catch((err) => {
        if (!cancelled) setError(getErrorDetail(err))
      })
    return () => {
      cancelled = true
    }
  }, [accountId])

  useEffect(() => {
    const account = accounts.find((a) => a.id === accountId)
    if (!account) return
    const next = labelFromAccount(account)
    setLabel(next.label)
    setCustomLabel(next.customLabel)
  }, [accountId, accounts])

  const pendingForAccount = requests.filter(
    (r) => r.accountId === accountId && r.status === 'Pending',
  )
  const resolvedLabel = label === '__custom' ? customLabel.trim() : label
  const selectedAccount = accounts.find((a) => a.id === accountId)

  async function submitRequest() {
    if (!accountId) return
    setBusy(true)
    setError(null)
    setMessage(null)
    try {
      await cardRequestsApi.submit({
        accountId,
        label: resolvedLabel || undefined,
      })
      setMessage('Kart talebiniz alındı. Admin onayından sonra harcama yapabilirsiniz.')
      await reload(accountId)
    } catch (err) {
      setError(getErrorDetail(err))
    } finally {
      setBusy(false)
    }
  }

  async function toggleBlock(card: Card) {
    setBusy(true)
    setError(null)
    try {
      if (card.status === 'Blocked') await cardsApi.unblock(card.id)
      else await cardsApi.block(card.id)
      await reload(accountId)
    } catch (err) {
      setError(getErrorDetail(err))
    } finally {
      setBusy(false)
    }
  }

  if (loading) {
    return (
      <Panel title="Kartlarım">
        <p className="muted">Yükleniyor…</p>
      </Panel>
    )
  }

  if (accounts.length === 0) {
    return (
      <Panel title="Kartlarım" subtitle="Kart talep etmek için önce hesabınız olmalı.">
        <EmptyState>
          Hesabınız yok. <Link to="/accounts">Dashboard</Link> üzerinden hesap talebi gönderin.
        </EmptyState>
      </Panel>
    )
  }

  return (
    <div className="stack">
      <Panel
        title="Kartlarım"
        subtitle="Her hesap için ayrı kart gerekir. Etiket, hesabın kategorisiyle eşleşmeli."
      >
        {error ? <Alert>{error}</Alert> : null}
        {message ? <Alert tone="ok">{message}</Alert> : null}

        <Field label="Hesap">
          <select
            value={accountId}
            onChange={(e) => setAccountId(e.target.value)}
          >
            {accounts.map((a) => (
              <option key={a.id} value={a.id}>
                {a.categoryName || 'Hesap'} · {a.accountNumber} — {a.balance}{' '}
                {a.currency}
              </option>
            ))}
          </select>
        </Field>

        {pendingForAccount.length > 0 ? (
          <div className="stack">
            {pendingForAccount.map((r) => (
              <div key={r.id} className="request-card">
                <div className="copy-row">
                  <strong>{r.label || 'Kart talebi'}</strong>
                  <StatusBadge status={r.status} />
                </div>
                <p className="muted">
                  {new Date(r.requestedAt).toLocaleString('tr-TR')}
                </p>
              </div>
            ))}
          </div>
        ) : null}

        <ul className="card-list">
          {cards.map((card) => (
            <li key={card.id}>
              <div>
                <p style={{ margin: 0, fontWeight: 700 }}>
                  {card.label || 'Kart'} · {card.maskedNumber}
                </p>
                <p className="muted">
                  <StatusBadge status={card.status} /> · son kullanma{' '}
                  {new Date(card.expiresAt).toLocaleDateString('tr-TR')}
                </p>
              </div>
              <button
                type="button"
                className="btn"
                disabled={busy || card.status === 'Expired'}
                onClick={() => void toggleBlock(card)}
              >
                {card.status === 'Blocked' ? 'Unblock' : 'Block'}
              </button>
            </li>
          ))}
        </ul>

        {cards.length === 0 && pendingForAccount.length === 0 ? (
          <EmptyState>
            Bu {selectedAccount?.categoryName || 'hesap'} için henüz kart yok. Aşağıdan talep
            gönderin.
          </EmptyState>
        ) : null}
      </Panel>

      <Panel
        title="Yeni kart talep et"
        subtitle="Talep seçili hesaba bağlanır; etiket varsayılan olarak kategori adıdır."
      >
        <Form
          onSubmit={() => {
            void submitRequest()
          }}
        >
          <Field label="Etiket">
            <select value={label} onChange={(e) => setLabel(e.target.value)}>
              {LABEL_PRESETS.map((p) => (
                <option key={p} value={p}>
                  {p}
                </option>
              ))}
              <option value="__custom">Serbest metin…</option>
            </select>
          </Field>
          {label === '__custom' ? (
            <Field label="Özel etiket">
              <input
                value={customLabel}
                onChange={(e) => setCustomLabel(e.target.value)}
                maxLength={64}
                placeholder="Örn. Seyahat"
                required
              />
            </Field>
          ) : null}
          <button
            type="submit"
            className="btn primary"
            disabled={busy || !accountId || pendingForAccount.length > 0}
          >
            {busy
              ? 'Gönderiliyor…'
              : pendingForAccount.length > 0
                ? 'Bu hesap için bekleyen talep var'
                : `${selectedAccount?.categoryName || 'Hesap'} kartı talep et`}
          </button>
        </Form>
      </Panel>

      {requests.some((r) => r.status === 'Rejected') ? (
        <Panel title="Reddedilen talepler">
          <ul className="account-list">
            {requests
              .filter((r) => r.status === 'Rejected')
              .map((r) => (
                <li key={r.id}>
                  <div>
                    <p style={{ margin: 0 }}>{r.label || 'Kart'}</p>
                    <p className="muted">{r.rejectionReason || 'Sebep belirtilmedi'}</p>
                  </div>
                  <StatusBadge status={r.status} />
                </li>
              ))}
          </ul>
        </Panel>
      ) : null}
    </div>
  )
}

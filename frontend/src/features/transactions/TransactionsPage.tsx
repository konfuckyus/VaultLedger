import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { accountsApi } from '@/api/accounts'
import { cardsApi } from '@/api/cards'
import { getErrorDetail } from '@/api/client'
import { transactionsApi } from '@/api/transactions'
import { Alert, Field, Form, Modal, Money, Panel } from '@/components/ui'
import { useIdempotentAction } from '@/hooks/useIdempotentAction'
import type { Account, AccountLookup, Card, TransactionRecord } from '@/types/api'

type PinModalState =
  | { open: false }
  | { open: true; action: 'spend' | 'transfer' }

export function TransactionsPage() {
  const [accounts, setAccounts] = useState<Account[]>([])
  const [cards, setCards] = useState<Card[]>([])
  const [accountId, setAccountId] = useState('')
  const [cardId, setCardId] = useState('')
  const [amount, setAmount] = useState('10')
  const [destinationNumber, setDestinationNumber] = useState('')
  const [lookup, setLookup] = useState<AccountLookup | null>(null)
  const [lookupError, setLookupError] = useState<string | null>(null)
  const [description, setDescription] = useState('')
  const [history, setHistory] = useState<TransactionRecord[]>([])
  const [expandedTxId, setExpandedTxId] = useState<string | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [pinModal, setPinModal] = useState<PinModalState>({ open: false })
  const [pin, setPin] = useState('')

  const reloadAfterMoney = useCallback(async () => {
    if (!accountId) return
    const [freshAccounts, freshHistory, freshCards] = await Promise.all([
      accountsApi.me(),
      transactionsApi.history(accountId),
      cardsApi.listByAccount(accountId),
    ])
    setAccounts(freshAccounts)
    setHistory(freshHistory)
    setCards(freshCards)
  }, [accountId])

  const spend = useIdempotentAction(
    async (key) => {
      if (!cardId) throw new Error('Harcama için aktif bir kart seçin.')
      if (pin.length !== 4) throw new Error('4 haneli PIN girin.')
      return transactionsApi.spend(
        { accountId, cardId, amount: Number(amount), description, pin },
        key,
      )
    },
    {
      successMessage: 'Harcama tamamlandı.',
      idleLabel: 'Harca',
      busyLabel: 'Gönderiliyor…',
      onSuccess: async () => {
        setPinModal({ open: false })
        setPin('')
        await reloadAfterMoney()
      },
    },
  )

  const transfer = useIdempotentAction(
    async (key) => {
      if (!lookup) throw new Error('Önce alıcı hesap numarasını doğrulayın.')
      if (pin.length !== 4) throw new Error('4 haneli PIN girin.')
      return transactionsApi.transfer(
        {
          sourceAccountId: accountId,
          destinationAccountId: lookup.id,
          amount: Number(amount),
          description,
          pin,
        },
        key,
      )
    },
    {
      successMessage: 'Transfer tamamlandı.',
      idleLabel: 'Transfer',
      busyLabel: 'Gönderiliyor…',
      onSuccess: async () => {
        setPinModal({ open: false })
        setPin('')
        await reloadAfterMoney()
      },
    },
  )

  useEffect(() => {
    void accountsApi
      .me()
      .then((data) => {
        setAccounts(data)
        if (data[0]) setAccountId(data[0].id)
      })
      .catch((err) => setLoadError(getErrorDetail(err)))
  }, [])

  useEffect(() => {
    if (!accountId) return
    let cancelled = false
    setExpandedTxId(null)
    setCards([])
    setCardId('')
    void Promise.all([
      transactionsApi.history(accountId),
      cardsApi.listByAccount(accountId),
    ])
      .then(([txHistory, accountCards]) => {
        if (cancelled) return
        setHistory(txHistory)
        setCards(accountCards)
        setCardId(accountCards.find((c) => c.status === 'Active')?.id ?? '')
      })
      .catch((err) => {
        if (!cancelled) setLoadError(getErrorDetail(err))
      })
    return () => {
      cancelled = true
    }
  }, [accountId])

  async function runLookup() {
    setLookup(null)
    setLookupError(null)
    const number = destinationNumber.trim()
    if (number.length !== 10) {
      setLookupError('Hesap numarası 10 hane olmalı.')
      return
    }
    try {
      const result = await accountsApi.lookup(number)
      setLookup(result)
    } catch (err) {
      setLookupError(getErrorDetail(err))
    }
  }

  function confirmPin() {
    if (!pinModal.open) return
    if (pinModal.action === 'spend') void spend.run()
    else void transfer.run()
  }

  const selectedAccount = accounts.find((a) => a.id === accountId)
  const cardById = new Map(cards.map((c) => [c.id, c]))
  const feedbackError = spend.error ?? transfer.error ?? loadError
  const feedbackOk = spend.message ?? transfer.message

  return (
    <div className="tx-layout">
      <div className="stack">
        <Panel title="Harcama (Spend)" subtitle="Kart zorunlu. Onayda işlem PIN'i istenir.">
          <Form onSubmit={() => undefined} className="stack">
            <Field label="Hesap">
              <select value={accountId} onChange={(e) => setAccountId(e.target.value)} required>
                {accounts.map((a) => (
                  <option key={a.id} value={a.id}>
                    {a.categoryName || 'Hesap'} · {a.accountNumber} — {a.balance} {a.currency}
                  </option>
                ))}
              </select>
            </Field>
            <Field label="Kart">
              <select value={cardId} onChange={(e) => setCardId(e.target.value)}>
                <option value="">Kart seçin</option>
                {cards
                  .filter((c) => c.status === 'Active')
                  .map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.label || 'Kart'} — {c.maskedNumber}
                    </option>
                  ))}
              </select>
            </Field>
            {accountId && cards.filter((c) => c.status === 'Active').length === 0 ? (
              <Alert>
                Bu hesapta aktif kart yok. Harcama için{' '}
                <Link to="/cards">Kartlarım</Link> üzerinden talep edin.
              </Alert>
            ) : null}
            <Field label="Tutar">
              <input
                type="number"
                min="0.01"
                step="0.01"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                required
              />
            </Field>
            <Field label="Açıklama">
              <input value={description} onChange={(e) => setDescription(e.target.value)} />
            </Field>
            <button
              className="btn primary"
              type="button"
              disabled={spend.busy || !accountId || !cardId}
              onClick={() => {
                setPin('')
                setPinModal({ open: true, action: 'spend' })
              }}
            >
              {spend.submitLabel}
            </button>
          </Form>
        </Panel>

        <Panel title="Transfer" subtitle="Transfer edilemeyen kategoriler reddedilir.">
          <Form onSubmit={() => undefined} className="stack">
            {selectedAccount?.isTransferable === false ? (
              <Alert>Bu hesap kategorisinden transfer yapılamaz.</Alert>
            ) : null}
            <Field label="Alıcı hesap numarası">
              <div className="copy-row">
                <input
                  value={destinationNumber}
                  onChange={(e) => {
                    setDestinationNumber(e.target.value)
                    setLookup(null)
                  }}
                  inputMode="numeric"
                  maxLength={10}
                  placeholder="10 haneli numara"
                />
                <button type="button" className="btn" onClick={() => void runLookup()}>
                  Doğrula
                </button>
              </div>
            </Field>
            {lookupError ? <Alert>{lookupError}</Alert> : null}
            {lookup ? (
              <Alert tone="ok">
                {lookup.ownerDisplayName} · {lookup.accountNumber} · {lookup.status}
              </Alert>
            ) : null}
            <Field label="Tutar">
              <input
                type="number"
                min="0.01"
                step="0.01"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
              />
            </Field>
            <button
              className="btn primary"
              type="button"
              disabled={
                transfer.busy ||
                !accountId ||
                !lookup ||
                selectedAccount?.isTransferable === false
              }
              onClick={() => {
                setPin('')
                setPinModal({ open: true, action: 'transfer' })
              }}
            >
              {transfer.submitLabel}
            </button>
          </Form>
        </Panel>

        {feedbackError ? <Alert>{feedbackError}</Alert> : null}
        {feedbackOk ? <Alert tone="ok">{feedbackOk}</Alert> : null}
      </div>

      <Panel title="İşlem geçmişi" subtitle="Özet satıra tıklayınca detay açılır.">
        <div className="tx-history-scroll">
          <ul className="tx-list">
            {history.map((tx) => {
              const card = tx.cardId ? cardById.get(tx.cardId) : undefined
              const expanded = expandedTxId === tx.id
              return (
                <li key={tx.id} className={expanded ? 'tx-item expanded' : 'tx-item'}>
                  <button
                    type="button"
                    className="tx-item-summary"
                    onClick={() => setExpandedTxId(expanded ? null : tx.id)}
                  >
                    <div>
                      <strong>{tx.type}</strong>
                      <p className="muted">{new Date(tx.createdAt).toLocaleString('tr-TR')}</p>
                    </div>
                    <Money amount={tx.amount} />
                  </button>
                  {expanded ? (
                    <div className="tx-item-detail">
                      {tx.description ? <p>{tx.description}</p> : <p className="muted">Açıklama yok</p>}
                      {tx.type === 'Spend' ? (
                        <p className="muted">
                          {card
                            ? `${card.label || 'Kart'} · ${card.maskedNumber}`
                            : tx.cardId
                              ? `Kart ${tx.cardId.slice(0, 8)}…`
                              : 'Kart bilgisi yok'}
                        </p>
                      ) : null}
                      <p className="muted">Durum: {tx.status}</p>
                    </div>
                  ) : null}
                </li>
              )
            })}
          </ul>
          {history.length === 0 ? <p className="empty">Henüz işlem yok.</p> : null}
        </div>
      </Panel>

      {pinModal.open ? (
        <Modal
          title="PIN Girin"
          onClose={() => {
            if (spend.busy || transfer.busy) return
            setPinModal({ open: false })
            setPin('')
          }}
          footer={
            <button
              type="button"
              className="btn primary"
              disabled={pin.length !== 4 || spend.busy || transfer.busy}
              onClick={confirmPin}
            >
              {spend.busy || transfer.busy ? 'Onaylanıyor…' : 'Onayla'}
            </button>
          }
        >
          <Field label="4 haneli işlem PIN'i">
            <input
              type="password"
              inputMode="numeric"
              maxLength={4}
              autoFocus
              value={pin}
              onChange={(e) => setPin(e.target.value.replace(/\D/g, '').slice(0, 4))}
              onKeyDown={(e) => {
                if (e.key === 'Enter' && pin.length === 4) confirmPin()
              }}
            />
          </Field>
        </Modal>
      ) : null}
    </div>
  )
}

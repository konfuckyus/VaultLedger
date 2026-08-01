import { useCallback, useEffect, useRef, useState } from 'react'
import { adminAccountRequestsApi } from '@/api/accountRequests'
import { accountsApi } from '@/api/accounts'
import { adminCardRequestsApi } from '@/api/cardRequests'
import { getErrorDetail } from '@/api/client'
import {
  adminBudgetCategoriesApi,
  adminCategoryEligibilityApi,
  adminUsersApi,
} from '@/api/budgetCategories'
import { adminTopUpRequestsApi } from '@/api/topUpRequests'
import { transactionsApi } from '@/api/transactions'
import { useAuth } from '@/auth'
import {
  Alert,
  EmptyState,
  Field,
  Form,
  Modal,
  Panel,
  StatusBadge,
} from '@/components/ui'
import { useIdempotentAction } from '@/hooks/useIdempotentAction'
import type {
  AccountRequest,
  AdminAccountListItem,
  ApproveCardRequestResult,
  BudgetCategory,
  CardRequest,
  CategoryEligibility,
  TopUpRequest,
  UserLookup,
} from '@/types/api'

type Tab = 'pending' | 'accounts' | 'categories' | 'topup' | 'adjustment' | 'advanced'

function formatPan(raw: string) {
  const digits = raw.replace(/\D/g, '')
  return digits.replace(/(\d{4})(?=\d)/g, '$1 ').trim()
}

export function AdminPage() {
  const { user } = useAuth()
  const [tab, setTab] = useState<Tab>('pending')

  const [accountRequests, setAccountRequests] = useState<AccountRequest[]>([])
  const [cardRequests, setCardRequests] = useState<CardRequest[]>([])
  const [topUpRequests, setTopUpRequests] = useState<TopUpRequest[]>([])
  const [panReveal, setPanReveal] = useState<ApproveCardRequestResult | null>(null)

  const [allAccounts, setAllAccounts] = useState<AdminAccountListItem[]>([])
  const [accountsSearchInput, setAccountsSearchInput] = useState('')
  const [accountsUserHits, setAccountsUserHits] = useState<UserLookup[]>([])
  const [accountsSelectedUser, setAccountsSelectedUser] = useState<UserLookup | null>(null)

  const [categories, setCategories] = useState<BudgetCategory[]>([])
  const [newCategoryName, setNewCategoryName] = useState('')
  const [newCategoryAmount, setNewCategoryAmount] = useState('0')
  const [newCategoryTransferable, setNewCategoryTransferable] = useState(true)
  const [newCategorySelfRequestable, setNewCategorySelfRequestable] = useState(true)
  const [editingCategory, setEditingCategory] = useState<BudgetCategory | null>(null)
  const [editAmount, setEditAmount] = useState('0')
  const [editTransferable, setEditTransferable] = useState(true)
  const [editSelfRequestable, setEditSelfRequestable] = useState(true)
  const [editActive, setEditActive] = useState(true)

  const [eligibilityCategory, setEligibilityCategory] = useState<BudgetCategory | null>(null)
  const [eligibilities, setEligibilities] = useState<CategoryEligibility[]>([])
  const [userSearch, setUserSearch] = useState('')
  const [userHits, setUserHits] = useState<UserLookup[]>([])
  const [selectedUserId, setSelectedUserId] = useState('')

  const [targetUserId, setTargetUserId] = useState('')
  const [currency, setCurrency] = useState('TRY')
  const [foundAccount, setFoundAccount] = useState<AdminAccountListItem | null>(null)
  const [topUpAmount, setTopUpAmount] = useState('50')
  const [topUpDescription, setTopUpDescription] = useState('Admin top-up')
  const [topUpSearchQuery, setTopUpSearchQuery] = useState('')
  const [topUpUserHits, setTopUpUserHits] = useState<UserLookup[]>([])
  const [topUpSelectedUser, setTopUpSelectedUser] = useState<UserLookup | null>(null)
  const [topUpUserAccounts, setTopUpUserAccounts] = useState<AdminAccountListItem[]>([])

  const [adjAccount, setAdjAccount] = useState<AdminAccountListItem | null>(null)
  const [adjAmount, setAdjAmount] = useState('50')
  const [adjDirection, setAdjDirection] = useState<'Increase' | 'Decrease'>('Decrease')
  const [adjReason, setAdjReason] = useState('')
  const [adjSearchQuery, setAdjSearchQuery] = useState('')
  const [adjUserHits, setAdjUserHits] = useState<UserLookup[]>([])
  const [adjSelectedUser, setAdjSelectedUser] = useState<UserLookup | null>(null)
  const [adjUserAccounts, setAdjUserAccounts] = useState<AdminAccountListItem[]>([])

  const [advancedUserSearch, setAdvancedUserSearch] = useState('')
  const [advancedUserHits, setAdvancedUserHits] = useState<UserLookup[]>([])

  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const rejectReasonRef = useRef<Record<string, string>>({})

  const reloadPending = useCallback(async () => {
    const [accounts, cards, topUps] = await Promise.all([
      adminAccountRequestsApi.pending(),
      adminCardRequestsApi.pending(),
      adminTopUpRequestsApi.pending(),
    ])
    setAccountRequests(accounts)
    setCardRequests(cards)
    setTopUpRequests(topUps)
  }, [])

  const reloadCategories = useCallback(async () => {
    setCategories(await adminBudgetCategoriesApi.list())
  }, [])

  async function openEligibilityManager(category: BudgetCategory) {
    setEligibilityCategory(category)
    setUserSearch('')
    setUserHits([])
    setSelectedUserId('')
    setBusy(true)
    setError(null)
    try {
      setEligibilities(await adminCategoryEligibilityApi.list(category.id))
    } catch (err) {
      setError(getErrorDetail(err))
    } finally {
      setBusy(false)
    }
  }

  useEffect(() => {
    if (tab !== 'pending') return
    void reloadPending().catch((err) => setError(getErrorDetail(err)))
  }, [tab, reloadPending])

  useEffect(() => {
    if (tab !== 'categories') return
    void reloadCategories().catch((err) => setError(getErrorDetail(err)))
  }, [tab, reloadCategories])

  const topUp = useIdempotentAction(
    async (key) => {
      if (!foundAccount) throw new Error('Önce hesabı seçin.')
      return transactionsApi.topUp(
        {
          accountId: foundAccount.id,
          amount: Number(topUpAmount),
          description: topUpDescription,
        },
        key,
      )
    },
    {
      successMessage: (tx) =>
        `Yükleme tamam (${foundAccount?.categoryName || 'Hesap'}): ${tx.amount} · tx ${tx.id}`,
      idleLabel: 'Bakiye yükle',
      busyLabel: 'Gönderiliyor…',
      onSuccess: async () => {
        if (!topUpSelectedUser) return
        const accounts = await loadAccountsForUser(topUpSelectedUser)
        setTopUpUserAccounts(accounts)
        if (foundAccount) {
          const fresh = accounts.find((a) => a.id === foundAccount.id)
          if (fresh) setFoundAccount(fresh)
        }
      },
    },
  )

  const adjustment = useIdempotentAction(
    async (key) => {
      if (!adjAccount) throw new Error('Önce hesabı seçin.')
      if (!adjReason.trim()) throw new Error('Sebep zorunlu.')
      return transactionsApi.adjustment(
        {
          accountId: adjAccount.id,
          amount: Number(adjAmount),
          direction: adjDirection,
          reason: adjReason.trim(),
        },
        key,
      )
    },
    {
      successMessage: (tx) =>
        `Düzeltme tamam (${adjAccount?.categoryName || 'Hesap'}): ${tx.amount} · ${tx.description ?? ''}`,
      idleLabel: 'Düzeltme uygula',
      busyLabel: 'Gönderiliyor…',
      onSuccess: async () => {
        if (!adjSelectedUser) return
        const accounts = await loadAccountsForUser(adjSelectedUser)
        setAdjUserAccounts(accounts)
        if (adjAccount) {
          const fresh = accounts.find((a) => a.id === adjAccount.id)
          if (fresh) setAdjAccount(fresh)
        }
      },
    },
  )

  async function loadAccountsForUser(user: UserLookup) {
    const page = await accountsApi.adminList(1, 50, user.email)
    return page.items.filter((a) => a.userId === user.id)
  }

  async function selectAccountsUser(user: UserLookup) {
    setBusy(true)
    setError(null)
    setAccountsUserHits([])
    setAccountsSearchInput('')
    setAccountsSelectedUser(user)
    try {
      const accounts = await loadAccountsForUser(user)
      setAllAccounts(accounts)
      setMessage(
        accounts.length > 0
          ? `${user.fullName} · ${accounts.length} hesap`
          : `${user.fullName} için henüz hesap yok.`,
      )
    } catch (err) {
      setError(getErrorDetail(err))
    } finally {
      setBusy(false)
    }
  }

  async function selectTopUpUser(user: UserLookup, preferredAccountId?: string) {
    setBusy(true)
    setError(null)
    setTopUpUserHits([])
    setTopUpSearchQuery('')
    setTopUpSelectedUser(user)
    setFoundAccount(null)
    try {
      const accounts = await loadAccountsForUser(user)
      setTopUpUserAccounts(accounts)
      const preferred =
        (preferredAccountId
          ? accounts.find((a) => a.id === preferredAccountId)
          : undefined) ?? accounts[0] ?? null
      setFoundAccount(preferred)
      setMessage(
        preferred
          ? `${user.fullName} · ${accounts.length} hesap · ${preferred.categoryName || 'Hesap'} seçili`
          : `${user.fullName} için henüz hesap yok.`,
      )
    } catch (err) {
      setError(getErrorDetail(err))
    } finally {
      setBusy(false)
    }
  }

  async function selectAdjUser(user: UserLookup, preferredAccountId?: string) {
    setBusy(true)
    setError(null)
    setAdjUserHits([])
    setAdjSearchQuery('')
    setAdjSelectedUser(user)
    setAdjAccount(null)
    setAdjReason('')
    try {
      const accounts = await loadAccountsForUser(user)
      setAdjUserAccounts(accounts)
      const preferred =
        (preferredAccountId
          ? accounts.find((a) => a.id === preferredAccountId)
          : undefined) ?? accounts[0] ?? null
      setAdjAccount(preferred)
      setMessage(
        preferred
          ? `${user.fullName} · ${accounts.length} hesap · ${preferred.categoryName || 'Hesap'} seçili`
          : `${user.fullName} için henüz hesap yok.`,
      )
    } catch (err) {
      setError(getErrorDetail(err))
    } finally {
      setBusy(false)
    }
  }

  function openTopUpFor(account: AdminAccountListItem) {
    setTab('topup')
    setError(null)
    void selectTopUpUser(
      {
        id: account.userId,
        fullName: account.ownerFullName,
        email: account.ownerEmail,
        role: 'User',
      },
      account.id,
    )
  }

  function openAdjustmentFor(account: AdminAccountListItem) {
    setTab('adjustment')
    setError(null)
    void selectAdjUser(
      {
        id: account.userId,
        fullName: account.ownerFullName,
        email: account.ownerEmail,
        role: 'User',
      },
      account.id,
    )
  }

  function resetTopUpUser() {
    setTopUpSelectedUser(null)
    setTopUpUserAccounts([])
    setFoundAccount(null)
    setTopUpUserHits([])
    setTopUpSearchQuery('')
    setMessage(null)
  }

  function resetAdjUser() {
    setAdjSelectedUser(null)
    setAdjUserAccounts([])
    setAdjAccount(null)
    setAdjUserHits([])
    setAdjSearchQuery('')
    setAdjReason('')
    setMessage(null)
  }

  function resetAccountsUser() {
    setAccountsSelectedUser(null)
    setAllAccounts([])
    setAccountsUserHits([])
    setAccountsSearchInput('')
    setMessage(null)
  }

  async function approveTopUp(id: string) {
    setBusy(true)
    setError(null)
    try {
      await adminTopUpRequestsApi.approve(id)
      setMessage('Bakiye talebi onaylandı; TopUp uygulandı.')
      await reloadPending()
    } catch (err) {
      setError(getErrorDetail(err))
    } finally {
      setBusy(false)
    }
  }

  async function rejectTopUp(id: string) {
    const reason = rejectReasonRef.current[id]?.trim() || 'Uygun görülmedi'
    setBusy(true)
    setError(null)
    try {
      await adminTopUpRequestsApi.reject(id, reason)
      setMessage('Bakiye talebi reddedildi.')
      await reloadPending()
    } catch (err) {
      setError(getErrorDetail(err))
    } finally {
      setBusy(false)
    }
  }

  async function approveAccount(id: string) {
    setBusy(true)
    setError(null)
    try {
      await adminAccountRequestsApi.approve(id)
      setMessage('Hesap talebi onaylandı.')
      await reloadPending()
    } catch (err) {
      setError(getErrorDetail(err))
    } finally {
      setBusy(false)
    }
  }

  async function rejectAccount(id: string) {
    const reason = rejectReasonRef.current[id]?.trim() || 'Uygun görülmedi'
    setBusy(true)
    setError(null)
    try {
      await adminAccountRequestsApi.reject(id, reason)
      setMessage('Hesap talebi reddedildi.')
      await reloadPending()
    } catch (err) {
      setError(getErrorDetail(err))
    } finally {
      setBusy(false)
    }
  }

  async function approveCard(id: string) {
    setBusy(true)
    setError(null)
    try {
      const result = await adminCardRequestsApi.approve(id)
      setPanReveal(result)
      setMessage('Kart talebi onaylandı. PAN yalnızca bu modalda görünür.')
      await reloadPending()
    } catch (err) {
      setError(getErrorDetail(err))
    } finally {
      setBusy(false)
    }
  }

  async function rejectCard(id: string) {
    const reason = rejectReasonRef.current[id]?.trim() || 'Uygun görülmedi'
    setBusy(true)
    setError(null)
    try {
      await adminCardRequestsApi.reject(id, reason)
      setMessage('Kart talebi reddedildi.')
      await reloadPending()
    } catch (err) {
      setError(getErrorDetail(err))
    } finally {
      setBusy(false)
    }
  }

  async function copyPan() {
    if (!panReveal) return
    await navigator.clipboard.writeText(panReveal.rawCardNumber)
    setMessage('Kart numarası panoya kopyalandı.')
  }

  const bannerError = error ?? topUp.error ?? adjustment.error
  const bannerOk = message ?? topUp.message ?? adjustment.message

  return (
    <div className="stack">
      <Panel title="Admin" subtitle={`Oturum: ${user?.email}`}>
        <div className="tabs" role="tablist">
          {(
            [
              ['pending', 'Bekleyen Talepler'],
              ['accounts', 'Tüm Hesaplar'],
              ['categories', 'Kategoriler'],
              ['topup', 'Bakiye Yükle'],
              ['adjustment', 'Düzeltme Yap'],
              ['advanced', 'Gelişmiş'],
            ] as const
          ).map(([id, label]) => (
            <button
              key={id}
              type="button"
              className={`tab ${tab === id ? 'active' : ''}`}
              onClick={() => setTab(id)}
            >
              {label}
            </button>
          ))}
        </div>

        {bannerError ? <Alert>{bannerError}</Alert> : null}
        {bannerOk ? <Alert tone="ok">{bannerOk}</Alert> : null}

        {tab === 'pending' ? (
          <div className="stack">
            <div className="split">
            <div className="stack">
              <h3 style={{ margin: 0 }}>Hesap talepleri</h3>
              {accountRequests.length === 0 ? (
                <EmptyState>Bekleyen hesap talebi yok.</EmptyState>
              ) : null}
              {accountRequests.map((r) => (
                <div key={r.id} className="request-card">
                  <div className="copy-row">
                    <strong>{r.userFullName || r.userId.slice(0, 8) + '…'}</strong>
                    <StatusBadge status={r.status} />
                  </div>
                  <p className="muted">{r.userEmail || r.userId}</p>
                  <p className="muted">
                    Kategori: {r.categoryName || r.categoryId?.slice(0, 8) || '—'}
                  </p>
                  <p className="muted">
                    {new Date(r.requestedAt).toLocaleString('tr-TR')}
                  </p>
                  <Field label="Red sebebi (opsiyonel)">
                    <input
                      defaultValue=""
                      onChange={(e) => {
                        rejectReasonRef.current[r.id] = e.target.value
                      }}
                      placeholder="Sebep"
                    />
                  </Field>
                  <div className="btn-row">
                    <button
                      type="button"
                      className="btn primary"
                      disabled={busy}
                      onClick={() => void approveAccount(r.id)}
                    >
                      Onayla
                    </button>
                    <button
                      type="button"
                      className="btn"
                      disabled={busy}
                      onClick={() => void rejectAccount(r.id)}
                    >
                      Reddet
                    </button>
                  </div>
                </div>
              ))}
            </div>

            <div className="stack">
              <h3 style={{ margin: 0 }}>Kart talepleri</h3>
              {cardRequests.length === 0 ? (
                <EmptyState>Bekleyen kart talebi yok.</EmptyState>
              ) : null}
              {cardRequests.map((r) => (
                <div key={r.id} className="request-card">
                  <div className="copy-row">
                    <strong>{r.userFullName || r.label || 'Kart'}</strong>
                    <StatusBadge status={r.status} />
                  </div>
                  <p className="muted">
                    {r.userEmail ? `${r.userEmail} · ` : null}
                    {r.label ? `${r.label} · ` : null}
                    Hesap {r.accountId.slice(0, 8)}… ·{' '}
                    {new Date(r.requestedAt).toLocaleString('tr-TR')}
                  </p>
                  <Field label="Red sebebi (opsiyonel)">
                    <input
                      defaultValue=""
                      onChange={(e) => {
                        rejectReasonRef.current[r.id] = e.target.value
                      }}
                      placeholder="Sebep"
                    />
                  </Field>
                  <div className="btn-row">
                    <button
                      type="button"
                      className="btn primary"
                      disabled={busy}
                      onClick={() => void approveCard(r.id)}
                    >
                      Onayla
                    </button>
                    <button
                      type="button"
                      className="btn"
                      disabled={busy}
                      onClick={() => void rejectCard(r.id)}
                    >
                      Reddet
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>

            <div className="stack">
              <h3 style={{ margin: 0 }}>Bakiye talepleri</h3>
              {topUpRequests.length === 0 ? (
                <EmptyState>Bekleyen bakiye talebi yok.</EmptyState>
              ) : null}
              {topUpRequests.map((r) => (
                <div key={r.id} className="request-card">
                  <div className="copy-row">
                    <strong>
                      {r.userFullName || r.userId.slice(0, 8) + '…'} · {r.amount} TRY
                    </strong>
                    <StatusBadge status={r.status} />
                  </div>
                  <p className="muted">{r.userEmail}</p>
                  {r.note ? <p className="muted">Not: {r.note}</p> : null}
                  <p className="muted">
                    Hesap {r.accountId.slice(0, 8)}… ·{' '}
                    {new Date(r.requestedAt).toLocaleString('tr-TR')}
                  </p>
                  <Field label="Red sebebi (opsiyonel)">
                    <input
                      defaultValue=""
                      onChange={(e) => {
                        rejectReasonRef.current[r.id] = e.target.value
                      }}
                      placeholder="Sebep"
                    />
                  </Field>
                  <div className="btn-row">
                    <button
                      type="button"
                      className="btn primary"
                      disabled={busy}
                      onClick={() => void approveTopUp(r.id)}
                    >
                      Onayla
                    </button>
                    <button
                      type="button"
                      className="btn"
                      disabled={busy}
                      onClick={() => void rejectTopUp(r.id)}
                    >
                      Reddet
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ) : null}

        {tab === 'accounts' ? (
          <div className="stack">
            {!accountsSelectedUser ? (
              <>
                <Form
                  onSubmit={() => {
                    const q = accountsSearchInput.trim()
                    if (q.length < 2) {
                      setError('En az 2 karakter girin.')
                      return
                    }
                    setBusy(true)
                    setError(null)
                    setMessage(null)
                    void adminUsersApi
                      .search(q)
                      .then((hits) => {
                        setAccountsUserHits(hits)
                        if (hits.length === 0) {
                          setMessage('Kullanıcı bulunamadı.')
                        }
                      })
                      .catch((err) => setError(getErrorDetail(err)))
                      .finally(() => setBusy(false))
                  }}
                >
                  <Field label="Önce kullanıcıyı ara (ad / e-posta)">
                    <div className="copy-row">
                      <input
                        value={accountsSearchInput}
                        onChange={(e) => setAccountsSearchInput(e.target.value)}
                        placeholder="Örn. test1@gmail.com"
                      />
                      <button className="btn" type="submit" disabled={busy}>
                        Ara
                      </button>
                    </div>
                  </Field>
                </Form>

                {accountsUserHits.length > 0 ? (
                  <div className="stack">
                    <p className="muted">Kullanıcı seçin; hesapları sonra listelenir.</p>
                    {accountsUserHits.map((u) => (
                      <div key={u.id} className="request-card">
                        <div className="copy-row">
                          <strong>{u.fullName}</strong>
                          <span className="muted">{u.role}</span>
                        </div>
                        <p className="muted">{u.email}</p>
                        <button
                          type="button"
                          className="btn primary"
                          disabled={busy}
                          onClick={() => void selectAccountsUser(u)}
                        >
                          Bu kullanıcıya gir
                        </button>
                      </div>
                    ))}
                  </div>
                ) : null}
              </>
            ) : (
              <>
                <div className="request-card">
                  <div className="copy-row">
                    <div>
                      <p style={{ margin: 0 }}>
                        <strong>{accountsSelectedUser.fullName}</strong>
                      </p>
                      <p className="muted">{accountsSelectedUser.email}</p>
                    </div>
                    <button type="button" className="btn" onClick={resetAccountsUser}>
                      Kullanıcı değiştir
                    </button>
                  </div>
                </div>

                {allAccounts.length === 0 ? (
                  <EmptyState>Bu kullanıcının hesabı yok.</EmptyState>
                ) : (
                  <>
                    <p className="muted">{allAccounts.length} hesap</p>
                    <div className="table-wrap">
                      <table className="data-table">
                        <thead>
                          <tr>
                            <th>Hesap No</th>
                            <th>Kategori</th>
                            <th>Bakiye</th>
                            <th>Durum</th>
                            <th />
                          </tr>
                        </thead>
                        <tbody>
                          {allAccounts.map((a) => (
                            <tr key={a.id}>
                              <td>
                                <strong>{a.accountNumber}</strong>
                              </td>
                              <td>{a.categoryName || '—'}</td>
                              <td>
                                {a.balance} {a.currency}
                              </td>
                              <td>{a.status}</td>
                              <td>
                                <div className="btn-row">
                                  <button
                                    type="button"
                                    className="btn"
                                    onClick={() => openTopUpFor(a)}
                                  >
                                    {a.categoryName || 'Hesap'} yükle
                                  </button>
                                  <button
                                    type="button"
                                    className="btn"
                                    onClick={() => openAdjustmentFor(a)}
                                  >
                                    {a.categoryName || 'Hesap'} düzelt
                                  </button>
                                </div>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </>
                )}
              </>
            )}
          </div>
        ) : null}

        {tab === 'categories' ? (
          <div className="stack">
            <Form
              onSubmit={() => {
                setBusy(true)
                setError(null)
                setMessage(null)
                void adminBudgetCategoriesApi
                  .create({
                    name: newCategoryName.trim(),
                    defaultAllocatedAmount: Number(newCategoryAmount),
                    isTransferable: newCategoryTransferable,
                    isSelfRequestable: newCategorySelfRequestable,
                  })
                  .then(async () => {
                    setMessage('Kategori eklendi.')
                    setNewCategoryName('')
                    setNewCategoryAmount('0')
                    setNewCategoryTransferable(true)
                    setNewCategorySelfRequestable(true)
                    await reloadCategories()
                  })
                  .catch((err) => setError(getErrorDetail(err)))
                  .finally(() => setBusy(false))
              }}
            >
              <h3 style={{ margin: 0 }}>Yeni Kategori Ekle</h3>
              <Field label="Ad">
                <input
                  value={newCategoryName}
                  onChange={(e) => setNewCategoryName(e.target.value)}
                  required
                  maxLength={64}
                />
              </Field>
              <Field label="Varsayılan Tutar">
                <input
                  type="number"
                  min="0"
                  step="0.01"
                  value={newCategoryAmount}
                  onChange={(e) => setNewCategoryAmount(e.target.value)}
                  required
                />
              </Field>
              <label className="checkbox-row">
                <input
                  type="checkbox"
                  checked={newCategoryTransferable}
                  onChange={(e) => setNewCategoryTransferable(e.target.checked)}
                />
                <span>Transfer Edilebilir</span>
              </label>
              <label className="checkbox-row">
                <input
                  type="checkbox"
                  checked={newCategorySelfRequestable}
                  onChange={(e) => setNewCategorySelfRequestable(e.target.checked)}
                />
                <span>Herkese Açık (Self-Requestable)</span>
              </label>
              <button className="btn primary" type="submit" disabled={busy}>
                Ekle
              </button>
            </Form>

            <div className="stack">
              <h3 style={{ margin: 0 }}>Katalog</h3>
              {categories.length === 0 ? <EmptyState>Kategori yok.</EmptyState> : null}
              <table className="data-table" style={{ width: '100%', borderCollapse: 'collapse' }}>
                <thead>
                  <tr>
                    <th align="left">Ad</th>
                    <th align="right">Varsayılan Tutar</th>
                    <th align="center">Transfer</th>
                    <th align="center">Herkese Açık</th>
                    <th align="center">Aktif</th>
                    <th align="right">İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  {categories.map((c) => (
                    <tr key={c.id}>
                      <td>
                        {c.name}
                        {c.isSystemDefault ? ' · sistem' : ''}
                      </td>
                      <td align="right">{c.defaultAllocatedAmount}</td>
                      <td align="center">{c.isTransferable ? 'Evet' : 'Hayır'}</td>
                      <td align="center">{c.isSelfRequestable ? 'Evet' : 'Hayır'}</td>
                      <td align="center">{c.isActive ? 'Evet' : 'Hayır'}</td>
                      <td align="right">
                        <div className="btn-row" style={{ justifyContent: 'flex-end' }}>
                          <button
                            type="button"
                            className="btn"
                            onClick={() => void openEligibilityManager(c)}
                          >
                            İzin Yönet
                          </button>
                          <button
                            type="button"
                            className="btn"
                            onClick={() => {
                              setEditingCategory(c)
                              setEditAmount(String(c.defaultAllocatedAmount))
                              setEditTransferable(c.isTransferable)
                              setEditSelfRequestable(c.isSelfRequestable)
                              setEditActive(c.isActive)
                            }}
                          >
                            Düzenle
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {editingCategory ? (
              <Modal
                title={`${editingCategory.name} düzenle`}
                onClose={() => setEditingCategory(null)}
                footer={
                  <button
                    type="button"
                    className="btn primary"
                    disabled={busy}
                    onClick={() => {
                      setBusy(true)
                      setError(null)
                      void adminBudgetCategoriesApi
                        .update(editingCategory.id, {
                          defaultAllocatedAmount: Number(editAmount),
                          isTransferable: editTransferable,
                          isSelfRequestable: editSelfRequestable,
                          isActive: editingCategory.isSystemDefault ? true : editActive,
                        })
                        .then(async () => {
                          setMessage('Kategori güncellendi.')
                          setEditingCategory(null)
                          await reloadCategories()
                        })
                        .catch((err) => setError(getErrorDetail(err)))
                        .finally(() => setBusy(false))
                    }}
                  >
                    Kaydet
                  </button>
                }
              >
                <Field label="Varsayılan Tutar">
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={editAmount}
                    onChange={(e) => setEditAmount(e.target.value)}
                  />
                </Field>
                <label className="checkbox-row">
                  <input
                    type="checkbox"
                    checked={editTransferable}
                    onChange={(e) => setEditTransferable(e.target.checked)}
                  />
                  <span>Transfer Edilebilir</span>
                </label>
                <label className="checkbox-row">
                  <input
                    type="checkbox"
                    checked={editSelfRequestable}
                    onChange={(e) => setEditSelfRequestable(e.target.checked)}
                  />
                  <span>Herkese Açık (Self-Requestable)</span>
                </label>
                <label className="checkbox-row">
                  <input
                    type="checkbox"
                    checked={editActive}
                    disabled={editingCategory.isSystemDefault}
                    onChange={(e) => setEditActive(e.target.checked)}
                  />
                  <span>
                    Aktif
                    {editingCategory.isSystemDefault ? ' (Genel pasifleştirilemez)' : ''}
                  </span>
                </label>
              </Modal>
            ) : null}

            {eligibilityCategory ? (
              <Modal
                title={`${eligibilityCategory.name} — izin yönetimi`}
                onClose={() => setEligibilityCategory(null)}
              >
                <p className="muted">
                  Herkese açık: {eligibilityCategory.isSelfRequestable ? 'Evet' : 'Hayır'}. Kapalı
                  kategorilerde aşağıdan kullanıcıya özel izin verin.
                </p>
                <Field label="Kullanıcı ara (ad / e-posta)">
                  <div className="copy-row">
                    <input
                      value={userSearch}
                      onChange={(e) => setUserSearch(e.target.value)}
                      placeholder="En az 2 karakter"
                    />
                    <button
                      type="button"
                      className="btn"
                      disabled={busy || userSearch.trim().length < 2}
                      onClick={() => {
                        setBusy(true)
                        void adminUsersApi
                          .search(userSearch.trim())
                          .then((hits) => {
                            setUserHits(hits)
                            setSelectedUserId(hits[0]?.id ?? '')
                          })
                          .catch((err) => setError(getErrorDetail(err)))
                          .finally(() => setBusy(false))
                      }}
                    >
                      Ara
                    </button>
                  </div>
                </Field>
                {userHits.length > 0 ? (
                  <Field label="Sonuç">
                    <select
                      value={selectedUserId}
                      onChange={(e) => setSelectedUserId(e.target.value)}
                    >
                      {userHits.map((u) => (
                        <option key={u.id} value={u.id}>
                          {u.fullName} · {u.email}
                        </option>
                      ))}
                    </select>
                  </Field>
                ) : null}
                <button
                  type="button"
                  className="btn primary"
                  disabled={busy || !selectedUserId}
                  onClick={() => {
                    if (!eligibilityCategory || !selectedUserId) return
                    setBusy(true)
                    void adminCategoryEligibilityApi
                      .grant({
                        userId: selectedUserId,
                        categoryId: eligibilityCategory.id,
                      })
                      .then(async () => {
                        setMessage('İzin verildi.')
                        setEligibilities(
                          await adminCategoryEligibilityApi.list(eligibilityCategory.id),
                        )
                      })
                      .catch((err) => setError(getErrorDetail(err)))
                      .finally(() => setBusy(false))
                  }}
                >
                  İzin Ver
                </button>
                <h3 style={{ margin: '1rem 0 0.5rem' }}>İzinli kullanıcılar</h3>
                {eligibilities.length === 0 ? (
                  <EmptyState>Bu kategoriye özel izin yok.</EmptyState>
                ) : (
                  <ul className="tx-list">
                    {eligibilities.map((e) => (
                      <li key={e.id}>
                        <div>
                          <strong>{e.userFullName}</strong>
                          <p className="muted">{e.userEmail}</p>
                        </div>
                        <button
                          type="button"
                          className="btn"
                          disabled={busy}
                          onClick={() => {
                            setBusy(true)
                            void adminCategoryEligibilityApi
                              .revoke(e.id)
                              .then(async () => {
                                setMessage('İzin kaldırıldı.')
                                if (eligibilityCategory) {
                                  setEligibilities(
                                    await adminCategoryEligibilityApi.list(eligibilityCategory.id),
                                  )
                                }
                              })
                              .catch((err) => setError(getErrorDetail(err)))
                              .finally(() => setBusy(false))
                          }}
                        >
                          Kaldır
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
              </Modal>
            ) : null}
          </div>
        ) : null}

        {tab === 'topup' ? (
          <div className="stack">
            {!topUpSelectedUser ? (
              <>
                <Form
                  onSubmit={() => {
                    const q = topUpSearchQuery.trim()
                    if (q.length < 2) {
                      setError('En az 2 karakter girin.')
                      return
                    }
                    setBusy(true)
                    setError(null)
                    setMessage(null)
                    void adminUsersApi
                      .search(q)
                      .then((hits) => {
                        setTopUpUserHits(hits)
                        if (hits.length === 0) {
                          setMessage('Kullanıcı bulunamadı.')
                        }
                      })
                      .catch((err) => setError(getErrorDetail(err)))
                      .finally(() => setBusy(false))
                  }}
                >
                  <Field label="Önce kullanıcıyı ara (ad / e-posta)">
                    <div className="copy-row">
                      <input
                        value={topUpSearchQuery}
                        onChange={(e) => setTopUpSearchQuery(e.target.value)}
                        placeholder="Örn. test1@gmail.com"
                      />
                      <button className="btn" type="submit" disabled={busy}>
                        Ara
                      </button>
                    </div>
                  </Field>
                </Form>

                {topUpUserHits.length > 0 ? (
                  <div className="stack">
                    <p className="muted">Kullanıcı seçin; hesapları sonra listelenir.</p>
                    {topUpUserHits.map((u) => (
                      <div key={u.id} className="request-card">
                        <div className="copy-row">
                          <strong>{u.fullName}</strong>
                          <span className="muted">{u.role}</span>
                        </div>
                        <p className="muted">{u.email}</p>
                        <button
                          type="button"
                          className="btn primary"
                          disabled={busy}
                          onClick={() => void selectTopUpUser(u)}
                        >
                          Bu kullanıcıya gir
                        </button>
                      </div>
                    ))}
                  </div>
                ) : null}
              </>
            ) : (
              <>
                <div className="request-card">
                  <div className="copy-row">
                    <div>
                      <p style={{ margin: 0 }}>
                        <strong>{topUpSelectedUser.fullName}</strong>
                      </p>
                      <p className="muted">{topUpSelectedUser.email}</p>
                    </div>
                    <button type="button" className="btn" onClick={resetTopUpUser}>
                      Kullanıcı değiştir
                    </button>
                  </div>
                </div>

                {topUpUserAccounts.length === 0 ? (
                  <EmptyState>Bu kullanıcının hesabı yok.</EmptyState>
                ) : (
                  <Field label="Hesap seç (kategori)">
                    <div className="stack">
                      {topUpUserAccounts.map((a) => (
                        <button
                          key={a.id}
                          type="button"
                          className={`btn ${a.id === foundAccount?.id ? 'primary' : ''}`}
                          onClick={() => {
                            setFoundAccount(a)
                            setMessage(
                              `Seçili hesap: ${a.categoryName || 'Hesap'} · ${a.accountNumber}`,
                            )
                          }}
                        >
                          {a.categoryName || 'Hesap'} · {a.accountNumber} — {a.balance}{' '}
                          {a.currency}
                        </button>
                      ))}
                    </div>
                  </Field>
                )}

                {foundAccount ? (
                  <Form
                    onSubmit={() => {
                      void topUp.run()
                    }}
                  >
                    <div className="request-card">
                      <p style={{ margin: 0 }}>
                        <strong>
                          {foundAccount.categoryName || 'Hesap'} · {foundAccount.accountNumber}
                        </strong>
                      </p>
                      <p className="muted">
                        Bakiye {foundAccount.balance} {foundAccount.currency}
                      </p>
                    </div>
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
                    <Field label="Açıklama">
                      <input
                        value={topUpDescription}
                        onChange={(e) => setTopUpDescription(e.target.value)}
                      />
                    </Field>
                    <button className="btn primary" type="submit" disabled={topUp.busy}>
                      {topUp.submitLabel}
                    </button>
                  </Form>
                ) : null}
              </>
            )}
          </div>
        ) : null}

        {tab === 'adjustment' ? (
          <div className="stack">
            {!adjSelectedUser ? (
              <>
                <Form
                  onSubmit={() => {
                    const q = adjSearchQuery.trim()
                    if (q.length < 2) {
                      setError('En az 2 karakter girin.')
                      return
                    }
                    setBusy(true)
                    setError(null)
                    setMessage(null)
                    void adminUsersApi
                      .search(q)
                      .then((hits) => {
                        setAdjUserHits(hits)
                        if (hits.length === 0) {
                          setMessage('Kullanıcı bulunamadı.')
                        }
                      })
                      .catch((err) => setError(getErrorDetail(err)))
                      .finally(() => setBusy(false))
                  }}
                >
                  <Field label="Önce kullanıcıyı ara (ad / e-posta)">
                    <div className="copy-row">
                      <input
                        value={adjSearchQuery}
                        onChange={(e) => setAdjSearchQuery(e.target.value)}
                        placeholder="Örn. test1@gmail.com"
                      />
                      <button className="btn" type="submit" disabled={busy}>
                        Ara
                      </button>
                    </div>
                  </Field>
                </Form>

                {adjUserHits.length > 0 ? (
                  <div className="stack">
                    <p className="muted">Kullanıcı seçin; hesapları sonra listelenir.</p>
                    {adjUserHits.map((u) => (
                      <div key={u.id} className="request-card">
                        <div className="copy-row">
                          <strong>{u.fullName}</strong>
                          <span className="muted">{u.role}</span>
                        </div>
                        <p className="muted">{u.email}</p>
                        <button
                          type="button"
                          className="btn primary"
                          disabled={busy}
                          onClick={() => void selectAdjUser(u)}
                        >
                          Bu kullanıcıya gir
                        </button>
                      </div>
                    ))}
                  </div>
                ) : null}
              </>
            ) : (
              <>
                <div className="request-card">
                  <div className="copy-row">
                    <div>
                      <p style={{ margin: 0 }}>
                        <strong>{adjSelectedUser.fullName}</strong>
                      </p>
                      <p className="muted">{adjSelectedUser.email}</p>
                    </div>
                    <button type="button" className="btn" onClick={resetAdjUser}>
                      Kullanıcı değiştir
                    </button>
                  </div>
                </div>

                {adjUserAccounts.length === 0 ? (
                  <EmptyState>Bu kullanıcının hesabı yok.</EmptyState>
                ) : (
                  <Field label="Hesap seç (kategori)">
                    <div className="stack">
                      {adjUserAccounts.map((a) => (
                        <button
                          key={a.id}
                          type="button"
                          className={`btn ${a.id === adjAccount?.id ? 'primary' : ''}`}
                          onClick={() => {
                            setAdjAccount(a)
                            setMessage(
                              `Seçili hesap: ${a.categoryName || 'Hesap'} · ${a.accountNumber}`,
                            )
                          }}
                        >
                          {a.categoryName || 'Hesap'} · {a.accountNumber} — {a.balance}{' '}
                          {a.currency}
                        </button>
                      ))}
                    </div>
                  </Field>
                )}

                {adjAccount ? (
                  <Form
                    onSubmit={() => {
                      void adjustment.run()
                    }}
                  >
                    <div className="request-card">
                      <p style={{ margin: 0 }}>
                        <strong>
                          {adjAccount.categoryName || 'Hesap'} · {adjAccount.accountNumber}
                        </strong>
                      </p>
                      <p className="muted">
                        Bakiye {adjAccount.balance} {adjAccount.currency}
                      </p>
                    </div>
                    <Field label="Yön">
                      <select
                        value={adjDirection}
                        onChange={(e) =>
                          setAdjDirection(e.target.value as 'Increase' | 'Decrease')
                        }
                      >
                        <option value="Decrease">Azalt (Decrease)</option>
                        <option value="Increase">Artır (Increase)</option>
                      </select>
                    </Field>
                    <Field label="Tutar">
                      <input
                        type="number"
                        min="0.01"
                        step="0.01"
                        value={adjAmount}
                        onChange={(e) => setAdjAmount(e.target.value)}
                        required
                      />
                    </Field>
                    <Field label="Sebep (zorunlu)">
                      <textarea
                        value={adjReason}
                        onChange={(e) => setAdjReason(e.target.value)}
                        rows={3}
                        required
                        placeholder="Örn. Yanlışlıkla 3 kez yüklendi, 2 fazla yükleme geri alınıyor"
                      />
                    </Field>
                    <button
                      className="btn primary"
                      type="submit"
                      disabled={adjustment.busy || !adjReason.trim()}
                    >
                      {adjustment.submitLabel}
                    </button>
                  </Form>
                ) : null}
              </>
            )}
          </div>
        ) : null}

        {tab === 'advanced' ? (
          <Panel
            title="Direkt hesap aç"
            subtitle="İstisnai durumlar için POST /accounts. Normal akış: hesap talebi."
          >
            <Field label="Kullanıcı ara (ad / e-posta)">
              <div className="copy-row">
                <input
                  value={advancedUserSearch}
                  onChange={(e) => setAdvancedUserSearch(e.target.value)}
                  placeholder="En az 2 karakter"
                />
                <button
                  type="button"
                  className="btn"
                  disabled={busy || advancedUserSearch.trim().length < 2}
                  onClick={() => {
                    setBusy(true)
                    setError(null)
                    void adminUsersApi
                      .search(advancedUserSearch.trim())
                      .then((hits) => {
                        setAdvancedUserHits(hits)
                        setTargetUserId(hits[0]?.id ?? '')
                      })
                      .catch((err) => setError(getErrorDetail(err)))
                      .finally(() => setBusy(false))
                  }}
                >
                  Ara
                </button>
              </div>
            </Field>
            {advancedUserHits.length > 0 ? (
              <Field label="Kullanıcı">
                <select
                  value={targetUserId}
                  onChange={(e) => setTargetUserId(e.target.value)}
                >
                  {advancedUserHits.map((u) => (
                    <option key={u.id} value={u.id}>
                      {u.fullName} · {u.email}
                    </option>
                  ))}
                </select>
              </Field>
            ) : null}
            <Form
              onSubmit={() => {
                setBusy(true)
                setError(null)
                setMessage(null)
                void accountsApi
                  .create({ userId: targetUserId, currency })
                  .then((account) =>
                    setMessage(
                      `Hesap oluşturuldu: ${account.accountNumber} (${account.id})`,
                    ),
                  )
                  .catch((err) => setError(getErrorDetail(err)))
                  .finally(() => setBusy(false))
              }}
            >
              <Field label="Para birimi">
                <input value={currency} onChange={(e) => setCurrency(e.target.value)} required />
              </Field>
              <button className="btn primary" type="submit" disabled={busy || !targetUserId}>
                Hesap oluştur
              </button>
            </Form>
          </Panel>
        ) : null}
      </Panel>

      {panReveal ? (
        <Modal
          title="Kart numarası (tek seferlik)"
          onClose={() => setPanReveal(null)}
          footer={
            <>
              <button type="button" className="btn" onClick={() => void copyPan()}>
                Kopyala
              </button>
              <button
                type="button"
                className="btn primary"
                onClick={() => setPanReveal(null)}
              >
                Anladım
              </button>
            </>
          }
        >
          <Alert tone="ok">
            Bu numara bir daha gösterilmeyecek. Kullanıcıya güvenli kanaldan iletin.
            Sayfa yenilenince veya talebi tekrar açınca PAN geri getirilemez (yalnızca
            hash saklanır).
          </Alert>
          {panReveal.label ? <p className="muted">Etiket: {panReveal.label}</p> : null}
          <p className="field-label">Kart Numarası</p>
          <div className="pan-box">{formatPan(panReveal.rawCardNumber)}</div>
          <p className="muted">Maskeli: {panReveal.maskedNumber}</p>
        </Modal>
      ) : null}
    </div>
  )
}

import { useState } from 'react'
import { Link } from 'react-router-dom'
import { getErrorDetail, useAuth } from '@/auth'
import { Alert, Field, Form } from '@/components/ui'

export function LoginPage() {
  const { login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  return (
    <div className="auth-scene">
      <div className="auth-hero">
        <p className="brand">VaultLedger</p>
        <h1>Kapalı döngüde güvenli bakiye.</h1>
        <p className="lede">
          Kampüs, kurum ve etkinlik ödemeleri için çift kayıtlı ledger ile çalışan cüzdan.
        </p>
      </div>

      <div className="auth-panel">
        <h2>Giriş</h2>
        <Form
          onSubmit={() => {
            setBusy(true)
            setError(null)
            void login({ email, password })
              .catch((err) => setError(getErrorDetail(err)))
              .finally(() => setBusy(false))
          }}
        >
          <Field label="E-posta">
            <input
              type="email"
              autoComplete="username"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </Field>
          <Field label="Şifre">
            <input
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </Field>
          {error ? <Alert>{error}</Alert> : null}
          <button className="btn primary" type="submit" disabled={busy}>
            {busy ? 'Giriş yapılıyor…' : 'Giriş yap'}
          </button>
        </Form>
        <p className="muted foot">
          Hesabın yok mu? <Link to="/register">Kayıt ol</Link>
        </p>
      </div>
    </div>
  )
}

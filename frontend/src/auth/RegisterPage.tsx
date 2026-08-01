import { useState } from 'react'
import { Link } from 'react-router-dom'
import { getErrorDetail, useAuth } from '@/auth'
import { Alert, Field, Form } from '@/components/ui'

export function RegisterPage() {
  const { register } = useAuth()
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  return (
    <div className="auth-scene">
      <div className="auth-hero">
        <p className="brand">VaultLedger</p>
        <h1>Yeni cüzdan, aynı güvenlik kuralları.</h1>
        <p className="lede">
          Kayıt sonrası JWT + refresh rotation ile oturum açılır; kart PAN’ı asla saklanmaz.
        </p>
      </div>

      <div className="auth-panel">
        <h2>Kayıt</h2>
        <Form
          onSubmit={() => {
            setBusy(true)
            setError(null)
            void register({ fullName, email, password })
              .catch((err) => setError(getErrorDetail(err)))
              .finally(() => setBusy(false))
          }}
        >
          <Field label="Ad soyad">
            <input value={fullName} onChange={(e) => setFullName(e.target.value)} required />
          </Field>
          <Field label="E-posta">
            <input
              type="email"
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </Field>
          <Field label="Şifre" hint="En az 8 karakter önerilir.">
            <input
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={8}
            />
          </Field>
          {error ? <Alert>{error}</Alert> : null}
          <button className="btn primary" type="submit" disabled={busy}>
            {busy ? 'Kaydediliyor…' : 'Hesap oluştur'}
          </button>
        </Form>
        <p className="muted foot">
          Zaten üye misin? <Link to="/login">Giriş yap</Link>
        </p>
      </div>
    </div>
  )
}

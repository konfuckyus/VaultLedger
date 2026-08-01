import type { FormEvent, ReactNode } from 'react'

type FieldProps = {
  label: string
  children: ReactNode
  hint?: string
}

export function Field({ label, children, hint }: FieldProps) {
  return (
    <label className="field">
      <span className="field-label">{label}</span>
      {children}
      {hint ? <span className="field-hint">{hint}</span> : null}
    </label>
  )
}

type PanelProps = {
  title: string
  subtitle?: string
  children: ReactNode
  actions?: ReactNode
}

export function Panel({ title, subtitle, children, actions }: PanelProps) {
  return (
    <section className="panel">
      <div className="panel-head">
        <div>
          <h2>{title}</h2>
          {subtitle ? <p className="muted">{subtitle}</p> : null}
        </div>
        {actions}
      </div>
      {children}
    </section>
  )
}

type FormProps = {
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  children: ReactNode
  className?: string
}

export function Form({ onSubmit, children, className }: FormProps) {
  return (
    <form
      className={className ?? 'stack'}
      onSubmit={(event) => {
        event.preventDefault()
        onSubmit(event)
      }}
    >
      {children}
    </form>
  )
}

export function Money({
  amount,
  currency = 'TRY',
}: {
  amount: number
  currency?: string
}) {
  const formatted = new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
  }).format(amount)

  return <span className="money">{formatted}</span>
}

export function Alert({ children, tone = 'error' }: { children: ReactNode; tone?: 'error' | 'ok' }) {
  return <div className={`alert ${tone}`}>{children}</div>
}

export function EmptyState({ children }: { children: ReactNode }) {
  return <p className="empty">{children}</p>
}

type ModalProps = {
  title: string
  children: ReactNode
  onClose: () => void
  footer?: ReactNode
}

export function Modal({ title, children, onClose, footer }: ModalProps) {
  return (
    <div className="modal-backdrop" role="presentation" onClick={onClose}>
      <div
        className="modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-head">
          <h2 id="modal-title">{title}</h2>
          <button type="button" className="btn ghost modal-close" onClick={onClose} aria-label="Kapat">
            ×
          </button>
        </div>
        <div className="modal-body">{children}</div>
        {footer ? <div className="modal-foot">{footer}</div> : null}
      </div>
    </div>
  )
}

export function StatusBadge({ status }: { status: string }) {
  const tone =
    status === 'Approved' || status === 'Active' || status === 'Completed'
      ? 'ok'
      : status === 'Rejected' || status === 'Blocked' || status === 'Failed'
        ? 'danger'
        : 'pending'
  return <span className={`badge ${tone}`}>{status}</span>
}


import type { Page } from '@playwright/test'
import { expect } from '@playwright/test'

export function uniqueEmail(prefix = 'user') {
  return `${prefix}.${Date.now()}.${Math.floor(Math.random() * 1e6)}@e2e.local`
}

export async function registerViaUi(
  page: Page,
  input: { fullName: string; email: string; password: string },
) {
  await page.goto('/register')
  await page.locator('input').nth(0).fill(input.fullName)
  await page.locator('input[type="email"]').fill(input.email)
  await page.locator('input[type="password"]').fill(input.password)
  await page.getByRole('button', { name: 'Hesap oluştur' }).click()
  await page.waitForURL('**/accounts', { timeout: 30_000 })
}

export async function loginViaUi(page: Page, email: string, password: string) {
  await page.goto('/login')
  await page.locator('input[type="email"]').fill(email)
  await page.locator('input[type="password"]').fill(password)
  await page.getByRole('button', { name: 'Giriş yap' }).click()
  await page.waitForURL('**/accounts', { timeout: 30_000 })
}

export async function readStoredUserId(page: Page): Promise<string> {
  const raw = await page.evaluate(() => localStorage.getItem('vl.user'))
  if (!raw) throw new Error('vl.user missing in localStorage')
  const parsed = JSON.parse(raw) as { userId: string }
  return parsed.userId
}

export async function logoutViaUi(page: Page) {
  await page.getByRole('button', { name: 'Çıkış' }).click()
  await page.waitForURL('**/login')
}

/** After seeding an account in Postgres, force the UI to reload dashboard. */
export async function gotoAccountsAndExpectAccount(page: Page, accountNumber: string) {
  await page.goto('/accounts')
  const accountsPanel = page.locator('section.panel').filter({
    has: page.getByRole('heading', { name: 'Hesaplarım' }),
  })
  await expect(accountsPanel.getByText(accountNumber)).toBeVisible({
    timeout: 20_000,
  })
}

export async function gotoTransactionsReady(page: Page) {
  await page.goto('/transactions')
  await expect(page.locator('select').first()).toBeVisible({ timeout: 20_000 })
  await expect(page.locator('select option')).not.toHaveCount(0)
}

export async function setTransactionPinViaUi(page: Page, pin = '1234') {
  await page.goto('/accounts')
  // Prefer the panel whose heading is the PIN section — the dashboard panel also
  // contains an Alert with "İşlem PIN" when the user has no PIN yet.
  const panel = page.locator('section.panel').filter({
    has: page.getByRole('heading', { name: "İşlem PIN'i" }),
  })
  await expect(panel).toBeVisible({ timeout: 20_000 })
  const inputs = panel.locator('input')
  const count = await inputs.count()
  // First-time setup has one field; change flow has old + new.
  if (count > 1) {
    await inputs.nth(0).fill(pin)
    await inputs.nth(1).fill(pin)
  } else {
    await inputs.first().fill(pin)
  }
  await panel.getByRole('button', { name: /PIN/i }).click()
  await expect(page.locator('.alert.ok')).toContainText(/PIN/i, { timeout: 15_000 })
}

export async function confirmPinModal(page: Page, pin = '1234') {
  const modal = page.locator('.modal')
  await expect(modal).toBeVisible({ timeout: 10_000 })
  await modal.locator('input').fill(pin)
  await modal.getByRole('button', { name: 'Onayla' }).click()
}

/** Lookup destination by 10-digit account number, then run transfer with PIN. */
export async function transferViaLookup(
  page: Page,
  accountNumber: string,
  amount: string,
  pin = '1234',
) {
  await page.getByPlaceholder('10 haneli numara').fill(accountNumber)
  await page.getByRole('button', { name: 'Doğrula' }).click()
  await expect(page.locator('.alert.ok', { hasText: accountNumber })).toBeVisible()
  const transferPanel = page.locator('.panel', { hasText: 'Transfer' })
  await transferPanel.locator('input[type="number"]').fill(amount)
  await page.getByRole('button', { name: 'Transfer' }).click()
  await confirmPinModal(page, pin)
}

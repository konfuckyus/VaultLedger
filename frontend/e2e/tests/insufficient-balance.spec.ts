import { expect, test } from '@playwright/test'
import { createCardForAccount, createUserAccount } from '../helpers/db'
import {
  confirmPinModal,
  gotoTransactionsReady,
  readStoredUserId,
  registerViaUi,
  setTransactionPinViaUi,
  uniqueEmail,
} from '../helpers/ui'

const password = 'Password123!'

test('insufficient balance shows a user-facing error', async ({ page }) => {
  const email = uniqueEmail('broke')
  await registerViaUi(page, { fullName: 'Broke User', email, password })
  const userId = await readStoredUserId(page)
  const account = await createUserAccount(userId, 5)
  await createCardForAccount(account.id)

  await setTransactionPinViaUi(page)
  await gotoTransactionsReady(page)
  await page.locator('input[type="number"]').first().fill('500')
  await page.getByRole('button', { name: 'Harca' }).click()
  await confirmPinModal(page)

  const alert = page.locator('.alert.error').filter({ hasText: /Yetersiz bakiye/i })
  await expect(alert).toBeVisible({ timeout: 15_000 })
})

test('transfer insufficient balance shows Yetersiz bakiye toast', async ({
  page,
  request,
}) => {
  const password = 'Password123!'
  const api = process.env.E2E_API_URL ?? 'http://localhost:5154'
  const emailA = uniqueEmail('tx-broke')
  const emailB = uniqueEmail('tx-peer')

  await registerViaUi(page, { fullName: 'Tx Broke', email: emailA, password })
  const userIdA = await readStoredUserId(page)
  const accountA = await createUserAccount(userIdA, 5)

  const registerB = await request.post(`${api}/auth/register`, {
    data: { fullName: 'Tx Peer', email: emailB, password },
  })
  expect(registerB.ok()).toBeTruthy()
  const bodyB = (await registerB.json()) as { userId: string }
  const accountB = await createUserAccount(bodyB.userId, 0)

  await setTransactionPinViaUi(page)
  await gotoTransactionsReady(page)
  await page.getByPlaceholder('10 haneli numara').fill(accountB.accountNumber)
  await page.getByRole('button', { name: 'Doğrula' }).click()
  await expect(page.locator('.alert.ok', { hasText: accountB.accountNumber })).toBeVisible()

  const transferPanel = page.locator('.panel', { hasText: 'Transfer' })
  await transferPanel.locator('input[type="number"]').fill('500')
  await page.getByRole('button', { name: 'Transfer' }).click()
  await confirmPinModal(page)

  // Spend panel may also show "aktif kart yok"; assert the insufficient-balance alert specifically.
  const alert = page.locator('.alert.error').filter({ hasText: /Yetersiz bakiye/i })
  await expect(alert).toBeVisible({ timeout: 15_000 })
})

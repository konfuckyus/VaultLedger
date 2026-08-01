import { expect, test } from '@playwright/test'
import { countCompletedTransfersBetween, createUserAccount } from '../helpers/db'
import {
  gotoTransactionsReady,
  readStoredUserId,
  registerViaUi,
  setTransactionPinViaUi,
  uniqueEmail,
} from '../helpers/ui'

const password = 'Password123!'
const api = process.env.E2E_API_URL ?? 'http://localhost:5154'

test('double-click transfer creates exactly one transaction_record', async ({
  page,
  request,
}) => {
  const emailA = uniqueEmail('dbl-a')
  const emailB = uniqueEmail('dbl-b')

  await registerViaUi(page, { fullName: 'Dbl A', email: emailA, password })
  const userIdA = await readStoredUserId(page)
  const accountA = await createUserAccount(userIdA, 200)

  const registerB = await request.post(`${api}/auth/register`, {
    data: { fullName: 'Dbl B', email: emailB, password },
  })
  expect(registerB.ok()).toBeTruthy()
  const bodyB = (await registerB.json()) as { userId: string }
  const accountB = await createUserAccount(bodyB.userId, 0)

  await setTransactionPinViaUi(page)
  await gotoTransactionsReady(page)
  await page.locator('select').first().selectOption(accountA.id)
  await page.getByPlaceholder('10 haneli numara').fill(accountB.accountNumber)
  await page.getByRole('button', { name: 'Doğrula' }).click()
  await expect(page.locator('.alert.ok', { hasText: accountB.accountNumber })).toBeVisible()

  const transferPanel = page.locator('.panel', { hasText: 'Transfer' })
  await transferPanel.locator('input[type="number"]').fill('10')

  const before = await countCompletedTransfersBetween(accountA.id, accountB.id)
  expect(before).toBe(0)

  await page.getByRole('button', { name: 'Transfer' }).click()
  const modal = page.locator('.modal')
  await expect(modal).toBeVisible()
  await modal.locator('input').fill('1234')

  const confirmBtn = modal.getByRole('button', { name: 'Onayla' })
  await confirmBtn.evaluate((el: HTMLButtonElement) => {
    el.click()
    el.click()
  })

  await expect(page.getByText('Transfer tamamlandı.')).toBeVisible({ timeout: 30_000 })
  await expect
    .poll(async () => countCompletedTransfersBetween(accountA.id, accountB.id), {
      timeout: 10_000,
    })
    .toBe(1)
})

import { expect, test } from '@playwright/test'
import { createCardForAccount, createUserAccount } from '../helpers/db'
import {
  gotoAccountsAndExpectAccount,
  gotoTransactionsReady,
  loginViaUi,
  logoutViaUi,
  readStoredUserId,
  registerViaUi,
  setTransactionPinViaUi,
  transferViaLookup,
  uniqueEmail,
} from '../helpers/ui'

const password = 'Password123!'
const api = process.env.E2E_API_URL ?? 'http://localhost:5154'

test('full user flow: register → spend → transfer → history → logout', async ({
  page,
  request,
}) => {
  const emailA = uniqueEmail('alice')
  const emailB = uniqueEmail('bob')

  await registerViaUi(page, {
    fullName: 'Alice E2E',
    email: emailA,
    password,
  })
  const userIdA = await readStoredUserId(page)
  // Opening balance via DB — TopUp is Admin-only.
  const accountA = await createUserAccount(userIdA, 100)
  await createCardForAccount(accountA.id, 'Yemek')

  const registerB = await request.post(`${api}/auth/register`, {
    data: {
      fullName: 'Bob E2E',
      email: emailB,
      password,
    },
  })
  expect(registerB.ok()).toBeTruthy()
  const bodyB = (await registerB.json()) as { userId: string }
  const accountB = await createUserAccount(bodyB.userId, 0)

  await setTransactionPinViaUi(page)
  await gotoAccountsAndExpectAccount(page, accountA.accountNumber)
  await gotoTransactionsReady(page)

  await page.locator('select').first().selectOption(accountA.id)
  await page.locator('input[type="number"]').first().fill('25')
  await page.getByRole('button', { name: 'Harca' }).click()
  await page.locator('.modal input').fill('1234')
  await page.locator('.modal').getByRole('button', { name: 'Onayla' }).click()
  await expect(page.getByText('Harcama tamamlandı.')).toBeVisible()

  await transferViaLookup(page, accountB.accountNumber, '15')
  await expect(page.getByText('Transfer tamamlandı.')).toBeVisible()

  await expect(page.locator('.tx-list li')).toHaveCount(2, { timeout: 20_000 })
  await expect(page.locator('.tx-list')).toContainText('Spend')
  await expect(page.locator('.tx-list')).toContainText('Transfer')
  // Card label is shown only in the expanded spend detail row.
  await page.locator('.tx-list li').filter({ hasText: 'Spend' }).getByRole('button').click()
  await expect(page.locator('.tx-list')).toContainText('Yemek')

  await logoutViaUi(page)
  await expect(page.getByRole('heading', { name: 'Giriş' })).toBeVisible()

  await loginViaUi(page, emailA, password)
  await expect(page.getByRole('link', { name: 'Dashboard' })).toBeVisible()
})

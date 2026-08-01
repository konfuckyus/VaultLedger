import { expect, test } from '@playwright/test'
import { createUserAccount } from '../helpers/db'
import {
  gotoTransactionsReady,
  readStoredUserId,
  registerViaUi,
  uniqueEmail,
} from '../helpers/ui'

const password = 'Password123!'

test('expired access token refreshes without logging the user out', async ({ page }) => {
  const email = uniqueEmail('refresh')
  await registerViaUi(page, { fullName: 'Refresh User', email, password })
  const userId = await readStoredUserId(page)
  await createUserAccount(userId, 50)

  const tokenBefore = await page.evaluate(() => localStorage.getItem('vl.accessToken'))
  expect(tokenBefore).toBeTruthy()

  // Force access token expiry while keeping a valid refresh token (rotation path).
  await page.evaluate(() => {
    localStorage.setItem('vl.accessToken', 'eyJhbGciOiJub25lIn0.e30.invalid')
  })

  const refreshResponse = page.waitForResponse(
    (res) => res.url().includes('/auth/refresh-token') && res.status() === 200,
  )

  await gotoTransactionsReady(page)
  await refreshResponse

  await expect(page).not.toHaveURL(/\/login/)
  await expect(page.getByRole('button', { name: 'Çıkış' })).toBeVisible()

  const tokenAfter = await page.evaluate(() => localStorage.getItem('vl.accessToken'))
  expect(tokenAfter).toBeTruthy()
  expect(tokenAfter).not.toEqual(tokenBefore)
  expect(tokenAfter).not.toEqual('eyJhbGciOiJub25lIn0.e30.invalid')
})

import { expect, test } from '@playwright/test'
import { registerViaUi, uniqueEmail } from '../helpers/ui'

const password = 'Password123!'

test('non-admin user is blocked from /admin', async ({ page }) => {
  const email = uniqueEmail('user-only')
  await registerViaUi(page, { fullName: 'Normal User', email, password })

  await page.goto('/admin')
  await expect(page).toHaveURL(/\/accounts/)
  await expect(page.getByRole('link', { name: 'Admin' })).toHaveCount(0)
})

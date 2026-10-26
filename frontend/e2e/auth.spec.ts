import { expect, test } from '@playwright/test'

/**
 * Authentication journeys: register, logout, login (success + failure),
 * and the guest redirect-to-login flow.
 */
test.describe('authentication', () => {
  test('registers a new account, shows it in the header, then logs out', async ({ page }) => {
    const email = `e2e${Date.now()}@example.test`

    await page.goto('/register')
    await page.getByTestId('register-name').fill('E2E Shopper')
    await page.getByTestId('register-email').fill(email)
    await page.getByTestId('register-password').fill('Str0ng@Pass1')
    await page.getByTestId('register-confirm').fill('Str0ng@Pass1')
    await page.getByTestId('accept-terms').click()
    await page.getByTestId('register-submit').click()

    // Registered users land on the storefront signed in.
    await expect(page).toHaveURL('/')
    await expect(page.getByTestId('account-menu')).toBeVisible()

    // Logout from the hover dropdown.
    await page.getByTestId('account-menu').hover()
    await page.getByTestId('logout').click()
    await expect(page.getByTestId('login-link')).toBeVisible()
  })

  test('shows an error for a wrong password', async ({ page }) => {
    await page.goto('/login')
    await page.getByTestId('login-email').fill('rahul.sharma@demo.dev')
    await page.getByTestId('login-password').fill('definitely-wrong')
    await page.getByTestId('login-submit').click()

    await expect(page.getByTestId('login-error')).toBeVisible()
    await expect(page).toHaveURL('/login')
  })

  test('redirects a guest from /orders to login and returns after signing in', async ({ page }) => {
    await page.goto('/orders')
    await expect(page).toHaveURL('/login?redirect=/orders')

    await page.getByTestId('login-email').fill('rahul.sharma@demo.dev')
    await page.getByTestId('login-password').fill('Demo@12345')
    await page.getByTestId('login-submit').click()

    await expect(page).toHaveURL('/orders')
    await expect(page.getByTestId('account-menu')).toBeVisible()
  })
})

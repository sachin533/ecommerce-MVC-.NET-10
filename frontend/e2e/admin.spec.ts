import { expect, test, type Page } from '@playwright/test'

/** Admin journeys: role-based access, dashboard stats, products and orders. */

async function loginAs(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/login')
  await page.getByTestId('login-email').fill(email)
  await page.getByTestId('login-password').fill(password)
  await page.getByTestId('login-submit').click()
  // Wait for the sign-in to land before navigating away.
  await expect(page.getByTestId('account-menu')).toBeVisible()
}

test.describe('admin', () => {
  test('a customer cannot reach the admin area', async ({ page }) => {
    await loginAs(page, 'rahul.sharma@demo.dev', 'Demo@12345')
    await page.goto('/admin')
    await expect(page).toHaveURL('/access-denied')
    await expect(page.getByTestId('access-denied-title')).toBeVisible()
  })

  test('admin dashboard shows store statistics', async ({ page }) => {
    await loginAs(page, 'admin@quickshop.dev', 'Admin@12345')
    await page.goto('/admin')

    await expect(page.getByTestId('dashboard-title')).toBeVisible()
    await expect(page.getByTestId('kpi-revenue')).not.toBeEmpty()
    await expect(page.getByTestId('kpi-orders')).not.toBeEmpty()
    await expect(page.getByTestId('kpi-customers')).not.toBeEmpty()
    await expect(page.getByTestId('kpi-products')).toHaveText('186')
    await expect(page.getByTestId('kpi-low-stock')).toBeVisible()
  })

  test('admin can browse products and orders management tables', async ({ page }) => {
    await loginAs(page, 'admin@quickshop.dev', 'Admin@12345')

    await page.goto('/admin/products')
    await expect(page.getByTestId('admin-products-title')).toBeVisible()
    await expect(page.getByTestId('admin-product-table')).toBeVisible()
    await expect(page.getByTestId('admin-product-row').first()).toBeVisible()

    await page.goto('/admin/orders')
    await expect(page.getByTestId('admin-orders-title')).toBeVisible()
    await expect(page.getByTestId('admin-order-table')).toBeVisible()
    // 17 seeded orders exist before this suite runs.
    expect(await page.getByTestId('admin-order-row').count()).toBeGreaterThan(0)
  })
})

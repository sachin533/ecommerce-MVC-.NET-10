import { expect, test } from '@playwright/test'

/**
 * Guest cart journeys: add, change quantity, remove, and the
 * "guests must sign in before checkout" rule.
 */
test.describe('cart', () => {
  test('guest can add an item, change quantity, and remove it', async ({ page }) => {
    // Add the first product from the shop grid.
    await page.goto('/shop')
    await page.getByTestId('product-card').first().getByTestId('add-to-cart').click()
    await expect(page.getByTestId('cart-count')).toHaveText('1')

    // Cart page shows the line and the server-derived summary.
    await page.goto('/cart')
    await expect(page.getByTestId('cart-title')).toBeVisible()
    await expect(page.getByTestId('cart-line')).toHaveCount(1)
    await expect(page.getByTestId('cart-subtotal')).toBeVisible()

    // Increase quantity → line quantity updates.
    await page.getByTestId('cart-line').getByRole('button', { name: 'Increase quantity' }).click()
    await expect(page.locator('[data-testid^="qty-"]').first()).toHaveText('2')
    await expect(page.getByTestId('cart-count')).toHaveText('2')

    // Remove → empty state.
    await page.locator('[data-testid^="remove-"]').first().click()
    await expect(page.getByTestId('empty-cart')).toBeVisible()
    await expect(page.locator('[data-testid^="cart-line"]')).toHaveCount(0)
  })

  test('guest checkout is redirected to login with a return path', async ({ page }) => {
    await page.goto('/shop')
    await page.getByTestId('product-card').first().getByTestId('add-to-cart').click()
    await expect(page.getByTestId('cart-count')).toHaveText('1')

    await page.goto('/cart')
    await page.getByTestId('checkout-button').click()

    await expect(page).toHaveURL('/login?redirect=/checkout')
  })
})

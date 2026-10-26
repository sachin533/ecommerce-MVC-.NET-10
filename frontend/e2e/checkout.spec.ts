import { expect, test, type Page } from '@playwright/test'

/**
 * Checkout journeys: the four steps (address → shipping → payment → review),
 * a successful mock-card payment, an order-history entry, and the
 * decline path that preserves the cart.
 */

async function registerFreshUser(page: Page): Promise<void> {
  const email = `e2e${Date.now()}@example.test`
  await page.goto('/register')
  await page.getByTestId('register-name').fill('E2E Checkout')
  await page.getByTestId('register-email').fill(email)
  await page.getByTestId('register-password').fill('Str0ng@Pass1')
  await page.getByTestId('register-confirm').fill('Str0ng@Pass1')
  await page.getByTestId('accept-terms').click()
  await page.getByTestId('register-submit').click()
  await expect(page.getByTestId('account-menu')).toBeVisible()
}

async function addFirstProductToCart(page: Page): Promise<void> {
  await page.goto('/shop')
  await page.getByTestId('product-card').first().getByTestId('add-to-cart').click()
  await expect(page.getByTestId('cart-count')).not.toHaveText('0')
}

async function fillAddressAndContinue(page: Page): Promise<void> {
  await expect(page.getByTestId('checkout-stepper')).toBeVisible()
  await page.locator('#addr-name').fill('E2E Checkout')
  await page.locator('#addr-phone').fill('9876543210')
  await page.locator('#addr-postal').fill('560001')
  await page.locator('#addr-line1').fill('42 Residency Road')
  await page.locator('#addr-city').fill('Bengaluru')
  await page.locator('#addr-state').fill('Karnataka')
  await page.getByTestId('next-shipping').click()
  await expect(page.getByTestId('step-shipping')).toBeVisible()
}

async function fillPaymentAndReview(page: Page, cardNumber: string): Promise<void> {
  await page.getByTestId('card-number').fill(cardNumber)
  await page.locator('#card-name').fill('E2E Checkout')
  await page.getByTestId('card-expiry').fill('1229')
  await page.getByTestId('card-cvv').fill('123')
  await page.getByTestId('next-review').click()
  await expect(page.getByTestId('step-review')).toBeVisible()
}

test.describe('checkout', () => {
  test('completes checkout with a successful mock card and records the order', async ({ page }) => {
    await registerFreshUser(page)
    await addFirstProductToCart(page)

    await page.goto('/cart')
    await page.getByTestId('checkout-button').click()
    await expect(page).toHaveURL('/checkout')

    await fillAddressAndContinue(page)

    // Shipping: standard is preselected; continue to payment.
    await page.getByTestId('shipping-standard').click()
    await page.getByTestId('next-payment').click()
    await expect(page.getByTestId('step-payment')).toBeVisible()

    // Mock card ending in an even digit succeeds.
    await fillPaymentAndReview(page, '4242424242424242')
    await page.getByTestId('place-order').click()

    // Confirmation shows the order number and payment status.
    await expect(page).toHaveURL(/\/order-confirmation\/\d+/)
    await expect(page.getByTestId('confirmation-hero')).toBeVisible()
    const orderNumber = await page.getByTestId('order-number').innerText()
    expect(orderNumber).toMatch(/^ORD\d+$/)
    await expect(page.getByTestId('payment-status')).toContainText('Succeeded')

    // The order appears in history.
    await page.goto('/orders')
    await expect(page.getByTestId('order-list')).toContainText(orderNumber)
  })

  test('declined card shows the provider reason and keeps the cart intact', async ({ page }) => {
    await registerFreshUser(page)
    await addFirstProductToCart(page)

    await page.goto('/cart')
    await page.getByTestId('checkout-button').click()
    await fillAddressAndContinue(page)
    await page.getByTestId('next-payment').click()

    // Mock card ending in an odd digit is declined by the mock provider.
    await fillPaymentAndReview(page, '4111111111111111')
    await page.getByTestId('place-order').click()

    await expect(page.getByTestId('payment-error')).toBeVisible()
    await expect(page.getByTestId('step-payment')).toBeVisible()

    // The cart is untouched after a decline.
    await page.goto('/cart')
    await expect(page.getByTestId('cart-line')).toHaveCount(1)
  })
})

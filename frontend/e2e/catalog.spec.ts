import { expect, test } from '@playwright/test'

const parsePrice = (text: string) => Number(text.replace(/\D/g, ''))

/** Live catalog size from the API — keeps assertions valid as the catalog grows. */
async function apiProductTotal(page: import('@playwright/test').Page): Promise<number> {
  const res = await page.request.get('/api/products?page=1&pageSize=1')
  const body = await res.json()
  return body.totalCount as number
}

/** Catalog journeys: home → shop, search, filtering, sorting, pagination, detail. */
test.describe('catalog', () => {
  test('home hero leads to the featured product detail page', async ({ page }) => {
    const total = await apiProductTotal(page)
    expect(total).toBeGreaterThanOrEqual(120)

    await page.goto('/')
    await expect(page.getByTestId('hero-carousel')).toBeVisible()
    await page.getByTestId('hero-shop-now').click()

    // Slide 1's Shop Now deep-links into the MacBook Air product page.
    await expect(page).toHaveURL('/product/apple-macbook-air-m3')
    await expect(page.getByTestId('product-title')).toContainText('MacBook Air')

    // The full seeded catalog is reachable from the shop.
    await page.goto('/shop')
    await expect(page.getByTestId('shop-title')).toBeVisible()
    await expect(page.getByTestId('result-count')).toContainText(`of ${total}`)
  })

  test('searching narrows the product grid', async ({ page }) => {
    const total = await apiProductTotal(page)
    await page.goto('/shop')
    await page.getByTestId('filter-search').fill('iphone')
    await page.getByTestId('filter-search').press('Enter')

    await expect(page.getByTestId('result-count')).not.toContainText(`of ${total}`)
    await expect(page.getByTestId('product-card').first()).toBeVisible()

    const countText = await page.getByTestId('result-count').innerText()
    const filtered = Number(countText.match(/of (\d+)/)?.[1] ?? '0')
    expect(filtered).toBeGreaterThan(0)
    expect(filtered).toBeLessThan(total)
  })

  test('category filter and price sorting work together', async ({ page }) => {
    const total = await apiProductTotal(page)
    await page.goto('/shop')
    await page.getByTestId('category-laptops').click()
    await expect(page).toHaveURL(/categorySlug=laptops/)
    await expect(page.getByTestId('result-count')).not.toContainText(`of ${total}`)

    const responses = page.waitForResponse((res) => res.url().includes('/api/products?'))
    await page.getByTestId('sort-select').selectOption('price-asc')
    await responses

    const cards = page.getByTestId('product-card')
    await expect(cards.first()).toBeVisible()
    const prices = await page.locator('[data-testid="product-card"] [data-testid="sale-price"]').allInnerTexts()
    expect(prices.length).toBeGreaterThan(1)
    const amounts = prices.map(parsePrice)
    for (let i = 1; i < amounts.length; i += 1) {
      expect(amounts[i]).toBeGreaterThanOrEqual(amounts[i - 1])
    }
  })

  test('pagination moves to the second page of results', async ({ page }) => {
    const total = await apiProductTotal(page)
    await page.goto('/shop')
    await page.getByTestId('pagination-next').click()

    await expect(page).toHaveURL(/page=2/)
    await expect(page.getByTestId('result-count')).toContainText(`13–24 of ${total}`)
  })

  test('opening a product shows its detail page', async ({ page }) => {
    await page.goto('/shop')
    await page.getByTestId('product-name').first().click()

    await expect(page.getByTestId('product-title')).toBeVisible()
    // Related-product cards reuse the same test id; scope to the detail hero.
    await expect(page.getByTestId('add-to-cart').first()).toBeVisible()
  })

  test('unknown routes render the branded 404 page', async ({ page }) => {
    await page.goto('/definitely-not-a-page')
    await expect(page.getByTestId('not-found-code')).toHaveText('404')
    await expect(page.getByTestId('not-found-title')).toBeVisible()
  })
})

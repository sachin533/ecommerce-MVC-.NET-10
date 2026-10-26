import type { AuthResponse, AuthUser, Cart, CartItem, ProductSummary } from '@/types'

/** Fully-populated ProductSummary with sensible defaults; override per test. */
export function makeProduct(overrides: Partial<ProductSummary> = {}): ProductSummary {
  return {
    id: 1,
    name: 'Aurora Wireless Headphones',
    slug: 'aurora-wireless-headphones',
    sku: 'QS-AUR-001',
    brand: 'Aurora',
    price: 4999,
    discountPercent: 20,
    salePrice: 3999,
    imageUrl: 'https://cdn.example.com/products/aurora.jpg',
    categoryId: 1,
    categoryName: 'Audio',
    categorySlug: 'audio',
    rating: 4.3,
    ratingCount: 128,
    stock: 12,
    isAvailable: true,
    isFeatured: true,
    isActive: true,
    createdAt: '2026-01-01T00:00:00.000Z',
    ...overrides,
  }
}

export function makeCartItem(overrides: Partial<CartItem> = {}): CartItem {
  return {
    id: 10,
    productId: 1,
    variantId: null,
    name: 'Aurora Wireless Headphones',
    slug: 'aurora-wireless-headphones',
    brand: 'Aurora',
    sku: 'QS-AUR-001',
    imageUrl: 'https://cdn.example.com/products/aurora.jpg',
    variantName: null,
    unitPrice: 3999,
    mrp: 4999,
    discountPercent: 20,
    quantity: 1,
    lineTotal: 3999,
    stock: 12,
    maxQuantity: 5,
    isAvailable: true,
    ...overrides,
  }
}

/** Cart totals are derived from the items, exactly like the API does. */
export function makeCart(items: CartItem[] = [], id = 'cart-1'): Cart {
  const itemCount = items.reduce((n, i) => n + i.quantity, 0)
  const subtotal = items.reduce((sum, i) => sum + i.lineTotal, 0)
  const estimatedShipping = subtotal === 0 || subtotal >= 999 ? 0 : 49
  return {
    id,
    owner: 'guest',
    items,
    itemCount,
    subtotal,
    estimatedShipping,
    total: subtotal + estimatedShipping,
    isEmpty: items.length === 0,
  }
}

export function makeUser(overrides: Partial<AuthUser> = {}): AuthUser {
  return {
    id: 'user-1',
    email: 'rahul.sharma@demo.dev',
    fullName: 'Rahul Sharma',
    phoneNumber: null,
    roles: ['Customer'],
    ...overrides,
  }
}

export function makeAuthResponse(user = makeUser()): AuthResponse {
  return { user, expiresAtUtc: '2026-12-31T23:59:59.000Z' }
}

import { http } from './http'
import type { Cart, ProductQuery } from '@/types'

export const cartService = {
  async get(): Promise<Cart> {
    const { data } = await http.get<Cart>('/cart')
    return data
  },

  async addItem(productId: number, quantity = 1, variantId?: number | null): Promise<Cart> {
    const { data } = await http.post<Cart>('/cart/items', { productId, quantity, variantId: variantId ?? null })
    return data
  },

  async updateItem(id: number, quantity: number): Promise<Cart> {
    const { data } = await http.put<Cart>(`/cart/items/${id}`, { quantity })
    return data
  },

  async removeItem(id: number): Promise<Cart> {
    const { data } = await http.delete<Cart>(`/cart/items/${id}`)
    return data
  },

  async clear(): Promise<Cart> {
    const { data } = await http.delete<Cart>('/cart')
    return data
  },

  /** Merge the guest cart into the user's cart after sign-in. */
  async merge(): Promise<Cart> {
    const { data } = await http.post<Cart>('/cart/merge')
    return data
  },
}

export const productService = {
  async list(query: ProductQuery = {}) {
    const { data } = await http.get('/products', { params: cleanParams(query) })
    return data as import('@/types').PagedResult<import('@/types').ProductSummary>
  },

  async featured() {
    const { data } = await http.get<import('@/types').ProductSummary[]>('/products/featured')
    return data
  },

  async filters(categorySlug?: string) {
    const { data } = await http.get<import('@/types').ProductFilters>('/products/filters', {
      params: categorySlug ? { categorySlug } : undefined,
    })
    return data
  },

  async bySlug(slug: string) {
    const { data } = await http.get<import('@/types').ProductDetail>(`/products/${encodeURIComponent(slug)}`)
    return data
  },
}

export const categoryService = {
  async list() {
    const { data } = await http.get<import('@/types').Category[]>('/categories')
    return data
  },
}

export const addressService = {
  async list() {
    const { data } = await http.get<import('@/types').Address[]>('/addresses')
    return data
  },

  async create(payload: import('@/types').AddressUpsert) {
    const { data } = await http.post<import('@/types').Address>('/addresses', payload)
    return data
  },

  async update(id: number, payload: import('@/types').AddressUpsert) {
    const { data } = await http.put<import('@/types').Address>(`/addresses/${id}`, payload)
    return data
  },

  async setDefault(id: number) {
    const { data } = await http.post<import('@/types').Address>(`/addresses/${id}/default`)
    return data
  },

  async remove(id: number): Promise<void> {
    await http.delete(`/addresses/${id}`)
  },
}

export const orderService = {
  async checkout(payload: import('@/types').CheckoutRequest) {
    const { data } = await http.post<import('@/types').CheckoutResponse>('/orders/checkout', payload)
    return data
  },

  async listMine(page = 1, pageSize = 10) {
    const { data } = await http.get<import('@/types').PagedResult<import('@/types').OrderSummary>>('/orders', {
      params: { page, pageSize },
    })
    return data
  },

  async getMine(id: number) {
    const { data } = await http.get<import('@/types').OrderDetail>(`/orders/${id}`)
    return data
  },
}

export const wishlistService = {
  /** API returns { items: ProductSummary[] }. */
  async get() {
    const { data } = await http.get<import('@/types').WishlistResponse>('/wishlist')
    return data.items ?? []
  },

  async add(productId: number) {
    const { data } = await http.post<import('@/types').WishlistResponse>(`/wishlist/items/${productId}`)
    return data.items ?? []
  },

  async remove(productId: number) {
    const { data } = await http.delete<import('@/types').WishlistResponse>(`/wishlist/items/${productId}`)
    return data.items ?? []
  },
}

export const adminService = {
  async dashboard() {
    const { data } = await http.get<import('@/types').DashboardStats>('/admin/dashboard')
    return data
  },

  async products(query: ProductQuery = {}) {
    const { data } = await http.get<import('@/types').PagedResult<import('@/types').ProductSummary>>('/admin/products', {
      params: cleanParams(query),
    })
    return data
  },

  async product(id: number) {
    const { data } = await http.get<import('@/types').ProductDetail>(`/admin/products/${id}`)
    return data
  },

  async createProduct(payload: import('@/types').AdminProductUpsert) {
    const { data } = await http.post<import('@/types').ProductDetail>('/admin/products', payload)
    return data
  },

  async updateProduct(id: number, payload: import('@/types').AdminProductUpsert) {
    const { data } = await http.put<import('@/types').ProductDetail>(`/admin/products/${id}`, payload)
    return data
  },

  async deleteProduct(id: number): Promise<void> {
    await http.delete(`/admin/products/${id}`)
  },

  async setStock(id: number, stock: number) {
    const { data } = await http.patch<import('@/types').ProductDetail>(`/admin/products/${id}/stock`, { stock })
    return data
  },

  async categories() {
    const { data } = await http.get<import('@/types').Category[]>('/admin/categories')
    return data
  },

  async createCategory(payload: { name: string; slug?: string; description?: string | null; imageUrl?: string | null; isActive?: boolean }) {
    const { data } = await http.post<import('@/types').Category>('/admin/categories', payload)
    return data
  },

  async updateCategory(id: number, payload: { name: string; slug?: string; description?: string | null; imageUrl?: string | null; isActive?: boolean }) {
    const { data } = await http.put<import('@/types').Category>(`/admin/categories/${id}`, payload)
    return data
  },

  async deleteCategory(id: number): Promise<void> {
    await http.delete(`/admin/categories/${id}`)
  },

  async orders(params: { status?: string; search?: string; page?: number; pageSize?: number } = {}) {
    const { data } = await http.get<import('@/types').PagedResult<import('@/types').OrderSummary>>('/admin/orders', {
      params: cleanParams(params),
    })
    return data
  },

  async order(id: number) {
    const { data } = await http.get<import('@/types').OrderDetail>(`/admin/orders/${id}`)
    return data
  },

  async updateOrderStatus(id: number, status: string) {
    const { data } = await http.put<import('@/types').OrderDetail>(`/admin/orders/${id}/status`, { status })
    return data
  },

  async customers(params: { search?: string; page?: number; pageSize?: number } = {}) {
    const { data } = await http.get<import('@/types').PagedResult<import('@/types').AdminCustomer>>('/admin/customers', {
      params: cleanParams(params),
    })
    return data
  },
}

/** Strip undefined/empty params so the query string stays clean. */
function cleanParams(params: object): Record<string, unknown> {
  const out: Record<string, unknown> = {}
  for (const [key, value] of Object.entries(params as Record<string, unknown>)) {
    if (value === undefined || value === null || value === '' || value === false) continue
    out[key] = value
  }
  return out
}

import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import ProductCard from '@/components/ProductCard.vue'
import { cartService, wishlistService } from '@/services/api'
import { ApiError } from '@/services/http'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import { makeCart, makeCartItem, makeProduct, makeUser } from '@/test/fixtures'
import type { ProductSummary } from '@/types'

const pushMock = vi.hoisted(() => vi.fn())

vi.mock('vue-router', () => ({ useRouter: () => ({ push: pushMock }) }))

vi.mock('@/services/api', () => ({
  cartService: {
    get: vi.fn(),
    addItem: vi.fn(),
    updateItem: vi.fn(),
    removeItem: vi.fn(),
    clear: vi.fn(),
    merge: vi.fn(),
  },
  wishlistService: { get: vi.fn(), add: vi.fn(), remove: vi.fn() },
  productService: { list: vi.fn(), detail: vi.fn(), related: vi.fn() },
  categoryService: { list: vi.fn(), tree: vi.fn() },
  addressService: { list: vi.fn(), create: vi.fn(), update: vi.fn(), remove: vi.fn(), setDefault: vi.fn() },
  orderService: { list: vi.fn(), detail: vi.fn(), create: vi.fn() },
  adminService: {},
}))

function mountCard(product: ProductSummary) {
  const pinia = createPinia()
  setActivePinia(pinia)
  return mount(ProductCard, { props: { product }, global: { plugins: [pinia] } })
}

describe('ProductCard', () => {
  beforeEach(() => {
    vi.resetAllMocks()
  })

  it('renders the product name, brand and sale price', () => {
    const product = makeProduct()
    const wrapper = mountCard(product)

    expect(wrapper.get('[data-testid="product-name"]').text()).toBe(product.name)
    expect(wrapper.text()).toContain(product.brand)
    expect(wrapper.text()).toContain('3,999')
    expect(wrapper.text()).toContain('4,999') // struck-through MRP
  })

  it('shows an out-of-stock badge and a disabled button', () => {
    const wrapper = mountCard(makeProduct({ isAvailable: false, stock: 0 }))

    expect(wrapper.text()).toContain('Out of stock')
    const button = wrapper.get('[data-testid="add-to-cart"]')
    expect(button.attributes('disabled')).toBeDefined()
    expect(button.text()).toBe('Out of stock')
  })

  it('warns when stock is running low', () => {
    const wrapper = mountCard(makeProduct({ stock: 3 }))
    expect(wrapper.text()).toContain('Only 3 left')
    expect(wrapper.get('[data-testid="add-to-cart"]').attributes('disabled')).toBeUndefined()
  })

  it('adds the product to the cart and confirms with a toast', async () => {
    vi.mocked(cartService.addItem).mockResolvedValue(makeCart([makeCartItem()]))
    const wrapper = mountCard(makeProduct({ id: 7 }))

    await wrapper.get('[data-testid="add-to-cart"]').trigger('click')
    await flushPromises()

    expect(cartService.addItem).toHaveBeenCalledWith(7, 1, undefined)
    const toasts = useToastStore().toasts
    expect(toasts).toHaveLength(1)
    expect(toasts[0].kind).toBe('success')
    expect(toasts[0].message).toContain('Added')
    // The in-card click must not also navigate to the detail page.
    expect(pushMock).not.toHaveBeenCalled()
  })

  it('surfaces the API problem detail when adding fails', async () => {
    vi.mocked(cartService.addItem).mockRejectedValue(
      new ApiError(409, { status: 409, title: 'Conflict', detail: 'Only 2 left in stock' }),
    )
    const wrapper = mountCard(makeProduct())

    await wrapper.get('[data-testid="add-to-cart"]').trigger('click')
    await flushPromises()

    const toasts = useToastStore().toasts
    expect(toasts[0].kind).toBe('error')
    expect(toasts[0].message).toBe('Only 2 left in stock')
  })

  it('navigates to the product detail page when the card is clicked', async () => {
    const product = makeProduct()
    const wrapper = mountCard(product)

    await wrapper.get('[data-testid="product-card"]').trigger('click')

    expect(pushMock).toHaveBeenCalledWith({ name: 'product', params: { slug: product.slug } })
  })

  it('sends guests to the login page when the wishlist button is used', async () => {
    const wrapper = mountCard(makeProduct())

    await wrapper.get('[data-testid="wishlist-toggle"]').trigger('click')
    await flushPromises()

    expect(pushMock).toHaveBeenCalledWith({ name: 'login', query: { redirect: '/wishlist' } })
    expect(wishlistService.add).not.toHaveBeenCalled()
  })

  it('adds to the wishlist for signed-in users and toggles the button', async () => {
    const product = makeProduct({ id: 42 })
    vi.mocked(wishlistService.add).mockResolvedValue([product])
    vi.mocked(wishlistService.remove).mockResolvedValue([])
    const wrapper = mountCard(product)
    useAuthStore().setUser(makeUser())

    const button = wrapper.get('[data-testid="wishlist-toggle"]')
    expect(button.attributes('aria-label')).toBe('Add to wishlist')

    await button.trigger('click')
    await flushPromises()

    expect(wishlistService.add).toHaveBeenCalledWith(42)
    expect(useToastStore().toasts[0].message).toBe('Saved to wishlist')
    expect(wrapper.get('[data-testid="wishlist-toggle"]').attributes('aria-label')).toBe(
      'Remove from wishlist',
    )

    await wrapper.get('[data-testid="wishlist-toggle"]').trigger('click')
    await flushPromises()

    expect(wishlistService.remove).toHaveBeenCalledWith(42)
    expect(useToastStore().toasts[1].message).toBe('Removed from wishlist')
  })
})

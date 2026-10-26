import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { cartService } from '@/services/api'
import { useCartStore } from '@/stores/cart'
import { makeCart, makeCartItem } from '@/test/fixtures'

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
}))

describe('cart store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.resetAllMocks()
  })

  it('starts empty and unflattened until a cart is fetched', () => {
    const store = useCartStore()
    expect(store.isEmpty).toBe(true)
    expect(store.itemCount).toBe(0)
    expect(store.subtotal).toBe(0)
    expect(store.loading).toBe(false)
  })

  it('fetch loads the server cart and its derived totals', async () => {
    const serverCart = makeCart([makeCartItem({ quantity: 2, lineTotal: 7998 })])
    vi.mocked(cartService.get).mockResolvedValue(serverCart)
    const store = useCartStore()

    await store.fetch()

    expect(cartService.get).toHaveBeenCalledTimes(1)
    expect(store.itemCount).toBe(2)
    expect(store.subtotal).toBe(7998)
    expect(store.isEmpty).toBe(false)
    expect(store.loading).toBe(false)
  })

  it('fetch toggles the loading flag even when the request fails', async () => {
    vi.mocked(cartService.get).mockRejectedValue(new Error('network down'))
    const store = useCartStore()

    await expect(store.fetch()).rejects.toThrow('network down')

    expect(store.loading).toBe(false)
    expect(store.cart).toBeNull()
  })

  it('add stores the returned cart and records lastAdded', async () => {
    const serverCart = makeCart([makeCartItem()])
    vi.mocked(cartService.addItem).mockResolvedValue(serverCart)
    const store = useCartStore()

    await store.add(5, 2, null)

    expect(cartService.addItem).toHaveBeenCalledWith(5, 2, null)
    expect(store.itemCount).toBe(1)
    expect(store.lastAdded).not.toBeNull()
  })

  it('add without a variant still calls the service with an explicit null variant', async () => {
    vi.mocked(cartService.addItem).mockResolvedValue(makeCart([makeCartItem()]))
    const store = useCartStore()

    await store.add(9)

    expect(cartService.addItem).toHaveBeenCalledWith(9, 1, undefined)
  })

  it('updateQuantity replaces the cart with the server response', async () => {
    const updated = makeCart([makeCartItem({ quantity: 3, lineTotal: 11997 })])
    vi.mocked(cartService.updateItem).mockResolvedValue(updated)
    const store = useCartStore()
    store.setCart(makeCart([makeCartItem()]))

    await store.updateQuantity(10, 3)

    expect(cartService.updateItem).toHaveBeenCalledWith(10, 3)
    expect(store.subtotal).toBe(11997)
    expect(store.itemCount).toBe(3)
  })

  it('remove and clear replace the cart from the server', async () => {
    vi.mocked(cartService.removeItem).mockResolvedValue(makeCart())
    vi.mocked(cartService.clear).mockResolvedValue(makeCart())
    const store = useCartStore()
    store.setCart(makeCart([makeCartItem()]))

    await store.remove(10)
    expect(cartService.removeItem).toHaveBeenCalledWith(10)
    expect(store.isEmpty).toBe(true)

    store.setCart(makeCart([makeCartItem()]))
    await store.clear()
    expect(cartService.clear).toHaveBeenCalledTimes(1)
    expect(store.isEmpty).toBe(true)
  })

  it('setCart applies a cart directly (used by checkout confirmation)', () => {
    const store = useCartStore()
    store.setCart(makeCart([makeCartItem({ quantity: 2 })]))
    expect(store.itemCount).toBe(2)

    store.setCart(makeCart())
    expect(store.itemCount).toBe(0)
    expect(store.isEmpty).toBe(true)
  })
})

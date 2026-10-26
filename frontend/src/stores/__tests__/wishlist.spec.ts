import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { wishlistService } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useWishlistStore } from '@/stores/wishlist'
import { makeProduct, makeUser } from '@/test/fixtures'

vi.mock('@/services/api', () => ({
  cartService: { get: vi.fn(), addItem: vi.fn(), merge: vi.fn() },
  wishlistService: { get: vi.fn(), add: vi.fn(), remove: vi.fn() },
}))

vi.mock('@/services/auth.service', () => ({
  authService: {
    register: vi.fn(),
    login: vi.fn(),
    refresh: vi.fn(),
    me: vi.fn(),
    logout: vi.fn(),
    forgotPassword: vi.fn(),
    resetPassword: vi.fn(),
    changePassword: vi.fn(),
    updateProfile: vi.fn(),
  },
}))

describe('wishlist store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.resetAllMocks()
  })

  it('starts empty', () => {
    const store = useWishlistStore()
    expect(store.count).toBe(0)
    expect(store.has(1)).toBe(false)
    expect(store.loaded).toBe(false)
  })

  it('has() reflects the current items', () => {
    const store = useWishlistStore()
    const product = makeProduct({ id: 11 })
    store.items.push(product)

    expect(store.has(11)).toBe(true)
    expect(store.has(99)).toBe(false)
    expect(store.count).toBe(1)
  })

  it('fetch for a guest clears items without calling the API', async () => {
    const store = useWishlistStore()
    store.items.push(makeProduct({ id: 1 }))
    store.loaded = false

    await store.fetch()

    expect(wishlistService.get).not.toHaveBeenCalled()
    expect(store.items).toHaveLength(0)
    expect(store.loaded).toBe(true)
  })

  it('fetch for a signed-in user loads the server wishlist once', async () => {
    useAuthStore().setUser(makeUser())
    vi.mocked(wishlistService.get).mockResolvedValue([makeProduct({ id: 3 })])
    const store = useWishlistStore()

    await store.fetch()
    await store.fetch() // second call is cached (loaded && !force)

    expect(wishlistService.get).toHaveBeenCalledTimes(1)
    expect(store.has(3)).toBe(true)

    await store.fetch(true) // force bypasses the cache
    expect(wishlistService.get).toHaveBeenCalledTimes(2)
  })

  it('toggle adds a product that is not yet wishlisted', async () => {
    const product = makeProduct({ id: 21 })
    vi.mocked(wishlistService.add).mockResolvedValue([product])
    const store = useWishlistStore()

    await store.toggle(product)

    expect(wishlistService.add).toHaveBeenCalledWith(21)
    expect(wishlistService.remove).not.toHaveBeenCalled()
    expect(store.has(21)).toBe(true)
  })

  it('toggle removes a product that is already wishlisted', async () => {
    const product = makeProduct({ id: 21 })
    vi.mocked(wishlistService.add).mockResolvedValue([product])
    vi.mocked(wishlistService.remove).mockResolvedValue([])
    const store = useWishlistStore()

    await store.toggle(product)
    await store.toggle(product)

    expect(wishlistService.remove).toHaveBeenCalledWith(21)
    expect(store.has(21)).toBe(false)
    expect(store.count).toBe(0)
  })

  it('reset clears items and the loaded flag', async () => {
    useAuthStore().setUser(makeUser())
    vi.mocked(wishlistService.get).mockResolvedValue([makeProduct({ id: 8 })])
    const store = useWishlistStore()
    await store.fetch()
    expect(store.loaded).toBe(true)

    store.reset()

    expect(store.items).toHaveLength(0)
    expect(store.loaded).toBe(false)
  })
})

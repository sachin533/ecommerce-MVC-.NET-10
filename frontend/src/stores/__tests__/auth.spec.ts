import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { cartService } from '@/services/api'
import { authService } from '@/services/auth.service'
import { ApiError } from '@/services/http'
import { useAuthStore } from '@/stores/auth'
import { makeAuthResponse, makeCart, makeUser } from '@/test/fixtures'

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

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.resetAllMocks()
  })

  it('starts as an initializing guest', () => {
    const store = useAuthStore()
    expect(store.user).toBeNull()
    expect(store.isAuthenticated).toBe(false)
    expect(store.isAdmin).toBe(false)
    expect(store.initializing).toBe(true)
  })

  it('initialize adopts the refreshed session and merges the guest cart', async () => {
    vi.mocked(authService.refresh).mockResolvedValue(makeAuthResponse(makeUser()))
    vi.mocked(cartService.merge).mockResolvedValue(makeCart())
    const store = useAuthStore()

    await store.initialize()

    expect(store.user).not.toBeNull()
    expect(store.isAuthenticated).toBe(true)
    expect(cartService.merge).toHaveBeenCalledTimes(1)
    expect(store.initializing).toBe(false)
  })

  it('initialize falls back to guest when refresh fails', async () => {
    vi.mocked(authService.refresh).mockRejectedValue(
      new ApiError(401, { status: 401, title: 'Unauthorized', detail: 'No session' }),
    )
    const store = useAuthStore()

    await store.initialize()

    expect(store.user).toBeNull()
    expect(store.initializing).toBe(false)
    expect(cartService.merge).not.toHaveBeenCalled()
  })

  it('login sets the user and merges the guest cart into the account cart', async () => {
    const user = makeUser({ email: 'new@shop.dev' })
    vi.mocked(authService.login).mockResolvedValue(makeAuthResponse(user))
    vi.mocked(cartService.merge).mockResolvedValue(makeCart())
    const store = useAuthStore()

    await store.login('new@shop.dev', 'Secret@1', true)

    expect(authService.login).toHaveBeenCalledWith({
      email: 'new@shop.dev',
      password: 'Secret@1',
      rememberMe: true,
    })
    expect(store.user).toEqual(user)
    expect(cartService.merge).toHaveBeenCalledTimes(1)
  })

  it('login failure leaves the visitor signed out', async () => {
    vi.mocked(authService.login).mockRejectedValue(
      new ApiError(401, { status: 401, title: 'Unauthorized', detail: 'Invalid credentials' }),
    )
    const store = useAuthStore()

    await expect(store.login('x@y.z', 'wrong', false)).rejects.toBeInstanceOf(ApiError)

    expect(store.user).toBeNull()
    expect(cartService.merge).not.toHaveBeenCalled()
  })

  it('register sets the user and merges the guest cart', async () => {
    const user = makeUser({ email: 'fresh@shop.dev' })
    vi.mocked(authService.register).mockResolvedValue(makeAuthResponse(user))
    vi.mocked(cartService.merge).mockResolvedValue(makeCart())
    const store = useAuthStore()

    await store.register({
      fullName: 'Fresh Shopper',
      email: 'fresh@shop.dev',
      password: 'Secret@1',
      confirmPassword: 'Secret@1',
      acceptTerms: true,
    })

    expect(store.user).toEqual(user)
    expect(cartService.merge).toHaveBeenCalledTimes(1)
  })

  it('logout clears local state even when the API call fails', async () => {
    vi.mocked(authService.refresh).mockResolvedValue(makeAuthResponse())
    vi.mocked(authService.logout).mockRejectedValue(new Error('network down'))
    const store = useAuthStore()
    await store.initialize()
    expect(store.isAuthenticated).toBe(true)

    await store.logout()

    expect(authService.logout).toHaveBeenCalledTimes(1)
    expect(store.user).toBeNull()
    expect(store.isAuthenticated).toBe(false)
  })

  it('setUser syncs profile edits and role flags', () => {
    const store = useAuthStore()

    store.setUser(makeUser({ roles: ['Customer'] }))
    expect(store.isAdmin).toBe(false)
    expect(store.fullName).toBe('Rahul Sharma')

    store.setUser(makeUser({ roles: ['Customer', 'Admin'], fullName: 'Site Owner' }))
    expect(store.isAdmin).toBe(true)
    expect(store.fullName).toBe('Site Owner')
  })

  it('isAuthError recognises only 401 ApiErrors', () => {
    const store = useAuthStore()

    const unauthorized = new ApiError(401, { status: 401, title: 'Unauthorized', detail: 'expired' })
    const forbidden = new ApiError(403, { status: 403, title: 'Forbidden', detail: 'nope' })

    expect(store.isAuthError(unauthorized)).toBe(true)
    expect(store.isAuthError(forbidden)).toBe(false)
    expect(store.isAuthError(new Error('plain'))).toBe(false)
    expect(store.isAuthError(undefined)).toBe(false)
  })
})

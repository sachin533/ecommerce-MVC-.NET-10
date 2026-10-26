import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authService } from '@/services/auth.service'
import { cartService } from '@/services/api'
import { ApiError } from '@/services/http'
import type { AuthUser } from '@/types'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthUser | null>(null)
  /** True until the initial silent refresh settles. */
  const initializing = ref(true)

  const isAuthenticated = computed(() => user.value !== null)
  const isAdmin = computed(() => user.value?.roles.includes('Admin') ?? false)
  const fullName = computed(() => user.value?.fullName ?? '')

  /**
   * On app boot: try a silent cookie refresh; otherwise the visitor is a guest.
   * The token lives in an HttpOnly cookie, so nothing is stored client-side.
   */
  async function initialize() {
    try {
      const response = await authService.refresh()
      user.value = response.user
      // Best-effort: a failed merge must never sign the user back out.
      try {
        await cartService.merge() // fold any guest cart into the account
      } catch {
        /* ignore */
      }
    } catch {
      user.value = null
    } finally {
      initializing.value = false
    }
  }

  async function login(email: string, password: string, rememberMe: boolean) {
    const response = await authService.login({ email, password, rememberMe })
    user.value = response.user
    // Best-effort: sign-in succeeded even if the cart fold fails.
    try {
      await cartService.merge() // guest cart follows the user
    } catch {
      /* ignore */
    }
  }

  async function register(payload: {
    fullName: string
    email: string
    password: string
    confirmPassword: string
    acceptTerms: boolean
  }) {
    const response = await authService.register(payload)
    user.value = response.user
    try {
      await cartService.merge()
    } catch {
      /* ignore */
    }
  }

  async function logout() {
    try {
      await authService.logout()
    } catch {
      // Cookie clear is best-effort; drop local state regardless.
    }
    user.value = null
  }

  /** Sync the store after profile edits. */
  function setUser(next: AuthUser) {
    user.value = next
  }

  /** Returns true when the error is a 401 (used by route guards). */
  function isAuthError(error: unknown): boolean {
    return error instanceof ApiError && error.status === 401
  }

  return {
    user,
    initializing,
    isAuthenticated,
    isAdmin,
    fullName,
    initialize,
    login,
    register,
    logout,
    setUser,
    isAuthError,
  }
})

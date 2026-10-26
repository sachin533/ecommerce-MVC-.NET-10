import axios, { type AxiosError } from 'axios'
import type { ApiProblem } from '@/types'

/**
 * Central HTTP client.
 *
 * - Sends cookies (HttpOnly auth session) with every request.
 * - Attaches the CSRF double-submit token (`quickshop_xsrf` cookie → X-XSRF-TOKEN
 *   header) on unsafe methods, which the API requires for cookie-authenticated
 *   requests.
 * - Guests carry an opaque cart id in the X-Cart-Id header so the server can
 *   resolve/validate their server-side cart.
 */
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api'

export const CART_ID_STORAGE_KEY = 'quickshop_cart_id'

export function getOrCreateCartId(): string {
  let id = localStorage.getItem(CART_ID_STORAGE_KEY)
  if (!id) {
    // Opaque client-generated GUID; the server validates the format and treats
    // it only as an opaque key. Never used for pricing/authorization decisions.
    id = crypto.randomUUID()
    localStorage.setItem(CART_ID_STORAGE_KEY, id)
  }
  return id
}

export function readCookie(name: string): string | null {
  const match = document.cookie.match(new RegExp(`(?:^|; )${name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}=([^;]*)`))
  return match ? decodeURIComponent(match[1]) : null
}

export const http = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true,
  timeout: 30000,
})

const UNSAFE = new Set(['post', 'put', 'patch', 'delete'])

http.interceptors.request.use((config) => {
  const method = (config.method ?? 'get').toLowerCase()
  if (UNSAFE.has(method)) {
    const xsrf = readCookie('quickshop_xsrf')
    if (xsrf) config.headers['X-XSRF-TOKEN'] = xsrf
    // Guests: identify the server-side cart (validated server-side per request).
    config.headers['X-Cart-Id'] = getOrCreateCartId()
  } else if (method === 'get') {
    config.headers['X-Cart-Id'] = getOrCreateCartId()
  }
  return config
})

/** Normalized API error carrying the RFC 7807 problem details payload. */
export class ApiError extends Error {
  readonly status: number
  readonly problem: ApiProblem

  constructor(status: number, problem: ApiProblem) {
    super(problem.detail || problem.title || 'Request failed')
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }

  /** Field-level validation errors, if the server returned any. */
  get fieldErrors(): Record<string, string[]> {
    return this.problem.errors ?? {}
  }
}

http.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiProblem>) => {
    if (error.response) {
      const problem = error.response.data ?? {
        status: error.response.status,
        title: 'Request failed',
        detail: error.message,
      }
      return Promise.reject(new ApiError(error.response.status, problem))
    }
    // Network failure / timeout — no response at all.
    return Promise.reject(
      new ApiError(0, { status: 0, title: 'Network error', detail: 'Could not reach the server. Check your connection.' }),
    )
  },
)

import { http } from './http'
import type {
  AuthResponse,
  AuthUser,
  ForgotPasswordResponse,
  LoginRequest,
  RegisterRequest,
} from '@/types'

export const authService = {
  async register(request: RegisterRequest): Promise<AuthResponse> {
    const { data } = await http.post<AuthResponse>('/auth/register', request)
    return data
  },

  async login(request: LoginRequest): Promise<AuthResponse> {
    const { data } = await http.post<AuthResponse>('/auth/login', request)
    return data
  },

  /** Sliding session renewal on app boot (cookie may still be valid). */
  async refresh(): Promise<AuthResponse> {
    const { data } = await http.post<AuthResponse>('/auth/refresh')
    return data
  },

  async me(): Promise<AuthUser> {
    const { data } = await http.get<AuthUser>('/auth/me')
    return data
  },

  async logout(): Promise<void> {
    await http.post('/auth/logout')
  },

  async forgotPassword(email: string): Promise<ForgotPasswordResponse> {
    const { data } = await http.post<ForgotPasswordResponse>('/auth/forgot-password', { email })
    return data
  },

  async resetPassword(payload: { email: string; token: string; newPassword: string; confirmNewPassword: string }): Promise<void> {
    await http.post('/auth/reset-password', payload)
  },

  async changePassword(payload: { currentPassword: string; newPassword: string; confirmNewPassword: string }): Promise<AuthUser> {
    const { data } = await http.post<AuthUser>('/auth/change-password', payload)
    return data
  },

  async updateProfile(payload: { fullName: string; phoneNumber?: string | null }): Promise<AuthUser> {
    const { data } = await http.put<AuthUser>('/auth/profile', payload)
    return data
  },
}

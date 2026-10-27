<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useToastStore } from '@/stores/toast'
import { ApiError } from '@/services/http'

const router = useRouter()
const auth = useAuthStore()
const cart = useCartStore()
const toasts = useToastStore()

const form = reactive({
  fullName: '',
  email: '',
  password: '',
  confirmPassword: '',
  acceptTerms: false,
})
const loading = ref(false)
const error = ref('')
const fieldErrors = ref<Record<string, string[]>>({})
const showPassword = ref(false)
const showConfirm = ref(false)

/** Client-side strength meter (the server enforces the real policy). */
const strength = computed(() => {
  const password = form.password
  let score = 0
  if (password.length >= 8) score++
  if (password.length >= 12) score++
  if (/[A-Z]/.test(password) && /[a-z]/.test(password)) score++
  if (/\d/.test(password)) score++
  if (/[^A-Za-z0-9]/.test(password)) score++
  if (password.length === 0) return { score: 0, label: '', color: '' }
  const levels = [
    { label: 'Too weak', color: 'bg-rose-500' },
    { label: 'Weak', color: 'bg-rose-400' },
    { label: 'Fair', color: 'bg-amber-400' },
    { label: 'Good', color: 'bg-sky-400' },
    { label: 'Strong', color: 'bg-emerald-500' },
    { label: 'Very strong', color: 'bg-emerald-500' },
  ]
  return { score, ...levels[Math.min(score, 5)] }
})

const requirements = computed(() => [
  { met: form.password.length >= 8, text: 'At least 8 characters' },
  { met: /[A-Z]/.test(form.password) && /[a-z]/.test(form.password), text: 'Upper & lowercase letters' },
  { met: /\d/.test(form.password), text: 'At least one number' },
  { met: form.password.length > 0 && form.password === form.confirmPassword, text: 'Passwords match' },
])

async function submit() {
  loading.value = true
  error.value = ''
  fieldErrors.value = {}
  try {
    await auth.register({ ...form })
    await cart.fetch()
    toasts.success('Account created — welcome to QuickShop!')
    router.push('/')
  } catch (err) {
    if (err instanceof ApiError) {
      error.value = err.message
      fieldErrors.value = err.fieldErrors
    } else {
      error.value = 'Something went wrong. Please try again.'
    }
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="grid min-h-[calc(100vh-4rem)] lg:grid-cols-2">
    <!-- Left: brand panel -->
    <aside class="relative hidden overflow-hidden bg-gradient-to-br from-brand-700 via-brand-600 to-sky-500 lg:flex lg:flex-col lg:justify-between lg:p-12">
      <div class="absolute -left-20 -top-20 h-72 w-72 rounded-full bg-white/10 blur-2xl" />
      <div class="absolute -bottom-24 -right-16 h-80 w-80 rounded-full bg-white/5 blur-2xl" />
      <router-link :to="{ name: 'home' }" class="relative flex items-center gap-2 text-white">
        <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-white/15">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-5 w-5">
            <path d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13 5.4 5M7 13l-2.3 2.3A1 1 0 0 0 5.4 17H17" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
        </span>
        <span class="text-2xl font-bold">QuickShop</span>
      </router-link>
      <div class="relative">
        <h2 class="text-3xl font-bold leading-tight text-white">Join a store<br />built for speed.</h2>
        <p class="mt-4 max-w-md text-white/80">
          Create an account to save addresses, track every order, and sync your cart and wishlist everywhere.
        </p>
        <ul class="mt-8 space-y-3 border-t border-white/20 pt-6 text-sm text-white/90">
          <li class="flex items-center gap-2"><span class="text-emerald-300">✓</span> Order history & live status</li>
          <li class="flex items-center gap-2"><span class="text-emerald-300">✓</span> Saved addresses for 1-tap checkout</li>
          <li class="flex items-center gap-2"><span class="text-emerald-300">✓</span> Wishlist with price drops</li>
        </ul>
      </div>
      <p class="relative text-xs text-white/60">Vue 3 · Vite · Pinia · ASP.NET Core Minimal API</p>
    </aside>

    <!-- Right: form -->
    <main class="flex items-center justify-center px-4 py-12 sm:px-6">
      <div class="w-full max-w-md">
        <h1 class="text-2xl font-bold text-slate-900" data-testid="register-title">Create your account</h1>
        <p class="mt-1 text-sm text-slate-500">
          Already have an account?
          <router-link :to="{ name: 'login' }" class="font-semibold text-brand-600 hover:underline">Sign in</router-link>
        </p>

        <form class="mt-6 space-y-4" @submit.prevent="submit" data-testid="register-form">
          <div v-if="error" class="rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700" data-testid="register-error">
            {{ error }}
          </div>

          <div>
            <label class="label" for="fullName">Full name</label>
            <input id="fullName" v-model="form.fullName" class="input" :class="{ 'input-error': fieldErrors.FullName }" autocomplete="name" required minlength="2" data-testid="register-name" />
            <p v-if="fieldErrors.FullName" class="field-error">{{ fieldErrors.FullName[0] }}</p>
          </div>

          <div>
            <label class="label" for="email">Email</label>
            <input id="email" v-model="form.email" type="email" class="input" :class="{ 'input-error': fieldErrors.Email }" autocomplete="email" required data-testid="register-email" />
            <p v-if="fieldErrors.Email" class="field-error">{{ fieldErrors.Email[0] }}</p>
          </div>

          <div>
            <label class="label" for="password">Password</label>
            <div class="relative">
              <input
                id="password"
                v-model="form.password"
                :type="showPassword ? 'text' : 'password'"
                class="input pr-10"
                :class="{ 'input-error': fieldErrors.Password }"
                autocomplete="new-password"
                required
                minlength="8"
                data-testid="register-password"
              />
              <button
                type="button"
                class="absolute right-2 top-1/2 -translate-y-1/2 rounded p-1.5 text-slate-400 hover:text-slate-600"
                :aria-label="showPassword ? 'Hide password' : 'Show password'"
                :aria-pressed="showPassword"
                data-testid="toggle-password"
                @click="showPassword = !showPassword"
              >
                <svg v-if="!showPassword" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="h-5 w-5"><path d="M2.5 12S6 5.5 12 5.5 21.5 12 21.5 12 18 18.5 12 18.5 2.5 12 2.5 12z" stroke-linecap="round" stroke-linejoin="round" /><circle cx="12" cy="12" r="3" /></svg>
                <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="h-5 w-5"><path d="M9.9 5.7A9.8 9.8 0 0 1 12 5.5c6 0 9.5 6.5 9.5 6.5a17.6 17.6 0 0 1-3 3.9M6.1 6.1A17.7 17.7 0 0 0 2.5 12S6 18.5 12 18.5c1.5 0 2.8-.4 4-1M3 3l18 18M10 10a3 3 0 0 0 4 4" stroke-linecap="round" stroke-linejoin="round" /></svg>
              </button>
            </div>
            <p v-if="fieldErrors.Password" class="field-error">{{ fieldErrors.Password[0] }}</p>

            <!-- Strength meter -->
            <div v-if="form.password" class="mt-2" data-testid="password-strength">
              <div class="flex gap-1">
                <span v-for="i in 5" :key="i" class="h-1 flex-1 rounded-full transition" :class="i <= strength.score ? strength.color : 'bg-slate-200'" />
              </div>
              <p class="mt-1 text-xs font-medium" :class="strength.score >= 4 ? 'text-emerald-600' : 'text-slate-500'">
                {{ strength.label }}
              </p>
            </div>

            <ul class="mt-2 space-y-1">
              <li v-for="req in requirements" :key="req.text" class="flex items-center gap-1.5 text-xs" :class="req.met ? 'text-emerald-600' : 'text-slate-400'">
                <span>{{ req.met ? '✓' : '○' }}</span> {{ req.text }}
              </li>
            </ul>
          </div>

          <div>
            <label class="label" for="confirmPassword">Confirm password</label>
            <div class="relative">
              <input
                id="confirmPassword"
                v-model="form.confirmPassword"
                :type="showConfirm ? 'text' : 'password'"
                class="input pr-10"
                :class="{ 'input-error': fieldErrors.ConfirmPassword }"
                autocomplete="new-password"
                required
                data-testid="register-confirm"
              />
              <button
                type="button"
                class="absolute right-2 top-1/2 -translate-y-1/2 rounded p-1.5 text-slate-400 hover:text-slate-600"
                :aria-label="showConfirm ? 'Hide confirm password' : 'Show confirm password'"
                :aria-pressed="showConfirm"
                data-testid="toggle-confirm"
                @click="showConfirm = !showConfirm"
              >
                <svg v-if="!showConfirm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="h-5 w-5"><path d="M2.5 12S6 5.5 12 5.5 21.5 12 21.5 12 18 18.5 12 18.5 2.5 12 2.5 12z" stroke-linecap="round" stroke-linejoin="round" /><circle cx="12" cy="12" r="3" /></svg>
                <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="h-5 w-5"><path d="M9.9 5.7A9.8 9.8 0 0 1 12 5.5c6 0 9.5 6.5 9.5 6.5a17.6 17.6 0 0 1-3 3.9M6.1 6.1A17.7 17.7 0 0 0 2.5 12S6 18.5 12 18.5c1.5 0 2.8-.4 4-1M3 3l18 18M10 10a3 3 0 0 0 4 4" stroke-linecap="round" stroke-linejoin="round" /></svg>
              </button>
            </div>
            <p v-if="fieldErrors.ConfirmPassword" class="field-error">{{ fieldErrors.ConfirmPassword[0] }}</p>
          </div>

          <label class="flex cursor-pointer items-start gap-2 text-sm text-slate-600" data-testid="accept-terms">
            <input v-model="form.acceptTerms" type="checkbox" class="mt-0.5 h-4 w-4 rounded border-slate-300 text-brand-600" required />
            <span>
              I agree to the
              <span class="font-semibold text-brand-600">Terms of Service</span> and
              <span class="font-semibold text-brand-600">Privacy Policy</span>.
            </span>
          </label>
          <p v-if="fieldErrors.AcceptTerms" class="field-error -mt-3">{{ fieldErrors.AcceptTerms[0] }}</p>

          <button type="submit" class="btn-primary w-full py-3" :disabled="loading || !form.acceptTerms" data-testid="register-submit">
            {{ loading ? 'Creating account…' : 'Create account' }}
          </button>
        </form>
      </div>
    </main>
  </div>
</template>

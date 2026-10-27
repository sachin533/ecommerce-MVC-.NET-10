<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useToastStore } from '@/stores/toast'
import { ApiError } from '@/services/http'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const cart = useCartStore()
const toasts = useToastStore()

const form = reactive({
  email: '',
  password: '',
  rememberMe: false,
})
const loading = ref(false)
const error = ref('')
const fieldErrors = ref<Record<string, string[]>>({})
const showPassword = ref(false)

async function submit() {
  loading.value = true
  error.value = ''
  fieldErrors.value = {}
  try {
    await auth.login(form.email, form.password, form.rememberMe)
    await cart.fetch()
    toasts.success('Welcome back!')
    const redirect = (route.query.redirect as string) || '/'
    router.push(redirect)
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
      <div class="absolute -right-20 -top-20 h-72 w-72 rounded-full bg-white/10 blur-2xl" />
      <div class="absolute -bottom-24 -left-16 h-80 w-80 rounded-full bg-white/5 blur-2xl" />
      <router-link :to="{ name: 'home' }" class="relative flex items-center gap-2 text-white">
        <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-white/15">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-5 w-5">
            <path d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13 5.4 5M7 13l-2.3 2.3A1 1 0 0 0 5.4 17H17" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
        </span>
        <span class="text-2xl font-bold">QuickShop</span>
      </router-link>
      <div class="relative">
        <h2 class="text-3xl font-bold leading-tight text-white">Everything you love,<br />delivered fast.</h2>
        <p class="mt-4 max-w-md text-white/80">
          Sign in to track orders, save addresses, keep a wishlist and carry your cart across devices.
        </p>
        <dl class="mt-8 grid max-w-md grid-cols-3 gap-4 border-t border-white/20 pt-6 text-sm text-white">
          <div><dt class="text-white/70">Products</dt><dd class="font-bold">170+</dd></div>
          <div><dt class="text-white/70">Categories</dt><dd class="font-bold">14</dd></div>
          <div><dt class="text-white/70">Free ship</dt><dd class="font-bold">₹999+</dd></div>
        </dl>
      </div>
      <p class="relative text-xs text-white/60">Vue 3 · Vite · Pinia · ASP.NET Core Minimal API</p>
    </aside>

    <!-- Right: form -->
    <main class="flex items-center justify-center px-4 py-12 sm:px-6">
      <div class="w-full max-w-md">
        <h1 class="text-2xl font-bold text-slate-900" data-testid="login-title">Sign in to your account</h1>
        <p class="mt-1 text-sm text-slate-500">
          New here?
          <router-link :to="{ name: 'register' }" class="font-semibold text-brand-600 hover:underline">Create an account</router-link>
        </p>

        <!-- Demo accounts -->
        <div class="mt-4 rounded-lg border border-sky-100 bg-sky-50 p-3 text-xs text-sky-800" data-testid="demo-accounts">
          <p class="font-semibold">Demo accounts</p>
          <p class="mt-1">Admin — <code class="rounded bg-white px-1">admin@quickshop.dev</code> / <code class="rounded bg-white px-1">Admin@12345</code></p>
          <p>Customer — <code class="rounded bg-white px-1">rahul.sharma@demo.dev</code> / <code class="rounded bg-white px-1">Demo@12345</code></p>
        </div>

        <form class="mt-6 space-y-4" @submit.prevent="submit" data-testid="login-form">
          <div v-if="error" class="rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700" data-testid="login-error">
            {{ error }}
          </div>

          <div>
            <label class="label" for="email">Email</label>
            <input
              id="email"
              v-model="form.email"
              type="email"
              class="input"
              :class="{ 'input-error': fieldErrors.Email }"
              autocomplete="email"
              required
              data-testid="login-email"
            />
            <p v-if="fieldErrors.Email" class="field-error">{{ fieldErrors.Email[0] }}</p>
          </div>

          <div>
            <div class="flex items-center justify-between">
              <label class="label" for="password">Password</label>
              <router-link :to="{ name: 'forgot-password' }" class="mb-1 text-xs font-semibold text-brand-600 hover:underline">
                Forgot password?
              </router-link>
            </div>
            <div class="relative">
              <input
                id="password"
                v-model="form.password"
                :type="showPassword ? 'text' : 'password'"
                class="input pr-10"
                autocomplete="current-password"
                required
                data-testid="login-password"
              />
              <button
                type="button"
                class="absolute right-2 top-1/2 -translate-y-1/2 rounded p-1.5 text-slate-400 hover:text-slate-600"
                :aria-label="showPassword ? 'Hide password' : 'Show password'"
                :aria-pressed="showPassword"
                data-testid="toggle-password"
                @click="showPassword = !showPassword"
              >
                <!-- Eye (hidden) / eye-off (visible) -->
                <svg v-if="!showPassword" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="h-5 w-5"><path d="M2.5 12S6 5.5 12 5.5 21.5 12 21.5 12 18 18.5 12 18.5 2.5 12 2.5 12z" stroke-linecap="round" stroke-linejoin="round" /><circle cx="12" cy="12" r="3" /></svg>
                <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="h-5 w-5"><path d="M9.9 5.7A9.8 9.8 0 0 1 12 5.5c6 0 9.5 6.5 9.5 6.5a17.6 17.6 0 0 1-3 3.9M6.1 6.1A17.7 17.7 0 0 0 2.5 12S6 18.5 12 18.5c1.5 0 2.8-.4 4-1M3 3l18 18M10 10a3 3 0 0 0 4 4" stroke-linecap="round" stroke-linejoin="round" /></svg>
              </button>
            </div>
          </div>

          <label class="flex cursor-pointer items-center gap-2 text-sm text-slate-600">
            <input v-model="form.rememberMe" type="checkbox" class="h-4 w-4 rounded border-slate-300 text-brand-600" data-testid="remember-me" />
            Keep me signed in on this device
          </label>

          <button type="submit" class="btn-primary w-full py-3" :disabled="loading" data-testid="login-submit">
            {{ loading ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>
      </div>
    </main>
  </div>
</template>

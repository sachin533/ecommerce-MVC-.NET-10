<script setup lang="ts">
import { ref } from 'vue'
import { authService } from '@/services/auth.service'
import { ApiError } from '@/services/http'
import type { ForgotPasswordResponse } from '@/types'

const email = ref('')
const loading = ref(false)
const sent = ref(false)
const error = ref('')
const devToken = ref<string | null>(null)

async function submit() {
  loading.value = true
  error.value = ''
  try {
    const response: ForgotPasswordResponse = await authService.forgotPassword(email.value)
    sent.value = true
    devToken.value = response.devResetToken ?? null
  } catch (err) {
    error.value = err instanceof ApiError ? err.message : 'Something went wrong. Please try again.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <main class="mx-auto flex min-h-[calc(100vh-4rem)] max-w-md flex-col justify-center px-4 py-12">
    <div class="card p-6">
      <template v-if="!sent">
        <h1 class="text-2xl font-bold text-slate-900" data-testid="forgot-title">Forgot your password?</h1>
        <p class="mt-2 text-sm text-slate-500">
          Enter the email you signed up with and we'll generate a reset link. The token expires shortly and can only be used once.
        </p>

        <form class="mt-6 space-y-4" @submit.prevent="submit">
          <div v-if="error" class="rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700" data-testid="forgot-error">
            {{ error }}
          </div>
          <div>
            <label class="label" for="email">Email</label>
            <input id="email" v-model="email" type="email" class="input" autocomplete="email" required data-testid="forgot-email" />
          </div>
          <button type="submit" class="btn-primary w-full py-3" :disabled="loading" data-testid="forgot-submit">
            {{ loading ? 'Sending…' : 'Send reset link' }}
          </button>
        </form>

        <p class="mt-4 text-center text-sm text-slate-500">
          Remembered it?
          <router-link :to="{ name: 'login' }" class="font-semibold text-brand-600 hover:underline">Back to sign in</router-link>
        </p>
      </template>

      <template v-else>
        <h1 class="text-2xl font-bold text-slate-900">Check your inbox</h1>
        <p class="mt-2 text-sm text-slate-500" data-testid="forgot-sent">
          If an account exists for <span class="font-semibold">{{ email }}</span>, a reset link has been generated.
        </p>

        <!-- DEVELOPMENT-ONLY: no SMTP in dev, so the API returns the token directly. -->
        <div v-if="devToken" class="mt-4 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800" data-testid="dev-token-notice">
          <p class="font-semibold">Development mode</p>
          <p class="mt-1">No real email is sent. Use this token (also written to <code class="rounded bg-white px-1">logs/dev-emails/</code> and the console):</p>
          <router-link
            class="btn-primary mt-3 inline-flex"
            :to="{ name: 'reset-password', query: { email, token: devToken } }"
            data-testid="dev-reset-link"
          >
            Open reset form with this token
          </router-link>
        </div>

        <router-link :to="{ name: 'login' }" class="btn-secondary mt-6 w-full">Back to sign in</router-link>
      </template>
    </div>
  </main>
</template>

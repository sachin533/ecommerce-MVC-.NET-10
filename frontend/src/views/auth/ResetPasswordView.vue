<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { authService } from '@/services/auth.service'
import { useToastStore } from '@/stores/toast'
import { ApiError } from '@/services/http'

const route = useRoute()
const router = useRouter()
const toasts = useToastStore()

const form = reactive({
  email: (route.query.email as string) ?? '',
  token: (route.query.token as string) ?? '',
  newPassword: '',
  confirmNewPassword: '',
})
const loading = ref(false)
const error = ref('')
const fieldErrors = ref<Record<string, string[]>>({})

const missingToken = computed(() => !form.token)

const strengthOk = computed(() => form.newPassword.length >= 8)

async function submit() {
  loading.value = true
  error.value = ''
  fieldErrors.value = {}
  try {
    await authService.resetPassword({ ...form })
    toasts.success('Password updated — sign in with your new password.')
    router.push({ name: 'login', query: { email: form.email } })
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
  <main class="mx-auto flex min-h-[calc(100vh-4rem)] max-w-md flex-col justify-center px-4 py-12">
    <div class="card p-6">
      <h1 class="text-2xl font-bold text-slate-900" data-testid="reset-title">Set a new password</h1>
      <p class="mt-2 text-sm text-slate-500">
        Your reset token is single-use and expires shortly. Pick a password you don't use elsewhere.
      </p>

      <div v-if="missingToken" class="mt-4 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800" data-testid="missing-token">
        This page needs a reset token. Request a new link from the
        <router-link :to="{ name: 'forgot-password' }" class="font-semibold underline">forgot password</router-link> page.
      </div>

      <form class="mt-6 space-y-4" @submit.prevent="submit">
        <div v-if="error" class="rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700" data-testid="reset-error">
          {{ error }}
        </div>

        <div>
          <label class="label" for="email">Email</label>
          <input id="email" v-model="form.email" type="email" class="input" autocomplete="email" required data-testid="reset-email" />
        </div>

        <div>
          <label class="label" for="token">Reset token</label>
          <input id="token" v-model="form.token" class="input font-mono text-xs" :class="{ 'input-error': fieldErrors.Token }" required data-testid="reset-token" />
          <p v-if="fieldErrors.Token" class="field-error">{{ fieldErrors.Token[0] }}</p>
        </div>

        <div>
          <label class="label" for="newPassword">New password</label>
          <input id="newPassword" v-model="form.newPassword" type="password" class="input" :class="{ 'input-error': fieldErrors.NewPassword || fieldErrors.Password }" autocomplete="new-password" required minlength="8" data-testid="reset-password" />
          <p v-if="fieldErrors.NewPassword" class="field-error">{{ fieldErrors.NewPassword[0] }}</p>
          <p v-if="fieldErrors.Password" class="field-error">{{ fieldErrors.Password[0] }}</p>
        </div>

        <div>
          <label class="label" for="confirmNewPassword">Confirm new password</label>
          <input id="confirmNewPassword" v-model="form.confirmNewPassword" type="password" class="input" autocomplete="new-password" required data-testid="reset-confirm" />
          <p v-if="fieldErrors.ConfirmNewPassword" class="field-error">{{ fieldErrors.ConfirmNewPassword[0] }}</p>
        </div>

        <button type="submit" class="btn-primary w-full py-3" :disabled="loading || missingToken || !strengthOk" data-testid="reset-submit">
          {{ loading ? 'Updating…' : 'Update password' }}
        </button>
      </form>
    </div>
  </main>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { authService } from '@/services/auth.service'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import { ApiError } from '@/services/http'

const auth = useAuthStore()
const toasts = useToastStore()

const profile = reactive({
  fullName: auth.user?.fullName ?? '',
  phoneNumber: auth.user?.phoneNumber ?? '',
})
const profileSaving = ref(false)
const profileErrors = ref<Record<string, string[]>>({})

const password = reactive({
  currentPassword: '',
  newPassword: '',
  confirmNewPassword: '',
})
const passwordSaving = ref(false)
const passwordErrors = ref<Record<string, string[]>>({})

async function saveProfile() {
  profileSaving.value = true
  profileErrors.value = {}
  try {
    const updated = await authService.updateProfile({
      fullName: profile.fullName,
      phoneNumber: profile.phoneNumber || null,
    })
    auth.setUser(updated)
    toasts.success('Profile updated')
  } catch (error) {
    if (error instanceof ApiError) {
      profileErrors.value = error.fieldErrors
      toasts.error(error.message)
    } else {
      toasts.error('Could not update the profile')
    }
  } finally {
    profileSaving.value = false
  }
}

async function changePassword() {
  passwordSaving.value = true
  passwordErrors.value = {}
  try {
    await authService.changePassword({ ...password })
    password.currentPassword = ''
    password.newPassword = ''
    password.confirmNewPassword = ''
    toasts.success('Password changed')
  } catch (error) {
    if (error instanceof ApiError) {
      passwordErrors.value = error.fieldErrors
      toasts.error(error.message)
    } else {
      toasts.error('Could not change the password')
    }
  } finally {
    passwordSaving.value = false
  }
}
</script>

<template>
  <div class="mx-auto max-w-3xl px-4 py-8 sm:px-6">
    <h1 class="mb-6 text-2xl font-bold text-slate-900" data-testid="profile-title">Profile</h1>

    <div class="grid gap-6 sm:grid-cols-2">
      <!-- Account summary -->
      <div class="card p-5 sm:col-span-2">
        <div class="flex items-center gap-4">
          <span class="flex h-14 w-14 items-center justify-center rounded-full bg-brand-100 text-xl font-bold text-brand-700">
            {{ (auth.user?.fullName || auth.user?.email || '?').slice(0, 1).toUpperCase() }}
          </span>
          <div>
            <p class="text-lg font-semibold text-slate-900">{{ auth.user?.fullName }}</p>
            <p class="text-sm text-slate-500">{{ auth.user?.email }}</p>
            <div class="mt-1 flex gap-1">
              <span v-for="role in auth.user?.roles" :key="role" class="badge bg-brand-50 text-brand-700">{{ role }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Profile form -->
      <form class="card p-5" @submit.prevent="saveProfile">
        <h2 class="font-semibold text-slate-900">Account details</h2>
        <div class="mt-4 space-y-4">
          <div>
            <label class="label" for="profile-name">Full name</label>
            <input id="profile-name" v-model="profile.fullName" class="input" :class="{ 'input-error': profileErrors.FullName }" required minlength="2" />
            <p v-if="profileErrors.FullName" class="field-error">{{ profileErrors.FullName[0] }}</p>
          </div>
          <div>
            <label class="label" for="profile-phone">Phone (optional)</label>
            <input id="profile-phone" v-model="profile.phoneNumber" class="input" autocomplete="tel" />
          </div>
          <div>
            <label class="label">Email</label>
            <input :value="auth.user?.email" class="input bg-slate-50" disabled />
            <p class="mt-1 text-xs text-slate-400">Email cannot be changed in this demo.</p>
          </div>
        </div>
        <button class="btn-primary mt-4 w-full" :disabled="profileSaving" data-testid="save-profile">
          {{ profileSaving ? 'Saving…' : 'Save changes' }}
        </button>
      </form>

      <!-- Password form -->
      <form class="card p-5" @submit.prevent="changePassword">
        <h2 class="font-semibold text-slate-900">Change password</h2>
        <div class="mt-4 space-y-4">
          <div>
            <label class="label" for="current-password">Current password</label>
            <input id="current-password" v-model="password.currentPassword" type="password" class="input" :class="{ 'input-error': passwordErrors.CurrentPassword }" autocomplete="current-password" required />
            <p v-if="passwordErrors.CurrentPassword" class="field-error">{{ passwordErrors.CurrentPassword[0] }}</p>
          </div>
          <div>
            <label class="label" for="new-password">New password</label>
            <input id="new-password" v-model="password.newPassword" type="password" class="input" :class="{ 'input-error': passwordErrors.NewPassword || passwordErrors.Password }" autocomplete="new-password" required minlength="8" />
            <p v-if="passwordErrors.NewPassword" class="field-error">{{ passwordErrors.NewPassword[0] }}</p>
          </div>
          <div>
            <label class="label" for="confirm-password">Confirm new password</label>
            <input id="confirm-password" v-model="password.confirmNewPassword" type="password" class="input" autocomplete="new-password" required />
          </div>
        </div>
        <button class="btn-primary mt-4 w-full" :disabled="passwordSaving" data-testid="save-password">
          {{ passwordSaving ? 'Updating…' : 'Update password' }}
        </button>
      </form>
    </div>

    <!-- Quick links -->
    <div class="mt-6 grid gap-3 sm:grid-cols-2">
      <router-link :to="{ name: 'orders' }" class="card flex items-center justify-between p-4 text-sm font-semibold text-slate-700 hover:border-brand-300">
        My orders <span>→</span>
      </router-link>
      <router-link :to="{ name: 'addresses' }" class="card flex items-center justify-between p-4 text-sm font-semibold text-slate-700 hover:border-brand-300">
        Manage addresses <span>→</span>
      </router-link>
    </div>
  </div>
</template>

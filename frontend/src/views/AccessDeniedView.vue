<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
</script>

<template>
  <main class="mx-auto flex min-h-[calc(100vh-4rem)] max-w-md flex-col justify-center px-4 py-12 text-center">
    <span class="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-rose-100 text-3xl">🔒</span>
    <h1 class="mt-4 text-2xl font-bold text-slate-900" data-testid="access-denied-title">Access denied</h1>
    <p class="mt-2 text-sm text-slate-500">
      You're signed in as <span class="font-semibold">{{ auth.user?.email }}</span>, but this area requires the Admin role.
      Contact an administrator if you believe this is a mistake.
    </p>
    <div class="mt-6 flex justify-center gap-3">
      <router-link :to="{ name: 'home' }" class="btn-secondary">Go home</router-link>
      <router-link v-if="auth.isAuthenticated" :to="{ name: 'orders' }" class="btn-primary">My orders</router-link>
      <router-link v-else :to="{ name: 'login' }" class="btn-primary">Sign in</router-link>
    </div>
  </main>
</template>

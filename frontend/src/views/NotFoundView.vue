<script setup lang="ts">
import { useRoute } from 'vue-router'

const route = useRoute()
</script>

<template>
  <main class="mx-auto flex min-h-[calc(100vh-4rem)] max-w-md flex-col justify-center px-4 py-12 text-center">
    <p class="text-7xl font-black text-brand-600" data-testid="not-found-code">404</p>
    <h1 class="mt-2 text-2xl font-bold text-slate-900" data-testid="not-found-title">Page not found</h1>
    <p class="mt-2 text-sm text-slate-500">
      The page <code class="rounded bg-slate-100 px-1">{{ route.fullPath }}</code> doesn't exist or may have moved.
    </p>
    <div class="mt-6 flex justify-center gap-3">
      <router-link :to="{ name: 'home' }" class="btn-secondary">Go home</router-link>
      <router-link :to="{ name: 'shop' }" class="btn-primary">Browse products</router-link>
    </div>
  </main>
</template>

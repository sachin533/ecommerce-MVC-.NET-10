<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useWishlistStore } from '@/stores/wishlist'

const router = useRouter()
const auth = useAuthStore()
const cart = useCartStore()
const wishlist = useWishlistStore()

const open = ref(false)

const nav = [
  { name: 'Dashboard', to: { name: 'admin-dashboard' }, icon: '📊' },
  { name: 'Products', to: { name: 'admin-products' }, icon: '📦' },
  { name: 'Categories', to: { name: 'admin-categories' }, icon: '🗂️' },
  { name: 'Orders', to: { name: 'admin-orders' }, icon: '🧾' },
  { name: 'Customers', to: { name: 'admin-customers' }, icon: '👥' },
]

async function signOut() {
  await auth.logout()
  wishlist.reset()
  await cart.fetch()
  router.push({ name: 'home' })
}
</script>

<template>
  <div class="min-h-screen bg-slate-100">
    <!-- Top bar -->
    <header class="sticky top-0 z-40 border-b border-slate-200 bg-white">
      <div class="flex h-14 items-center gap-3 px-4">
        <button class="rounded p-2 text-slate-500 hover:bg-slate-100 lg:hidden" aria-label="Toggle menu" @click="open = !open">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-5 w-5"><path d="M4 6h16M4 12h16M4 18h16" stroke-linecap="round" /></svg>
        </button>
        <router-link :to="{ name: 'admin-dashboard' }" class="flex items-center gap-2">
          <span class="flex h-8 w-8 items-center justify-center rounded-lg bg-brand-600 text-sm font-bold text-white">QS</span>
          <span class="font-bold text-slate-900">QuickShop <span class="text-brand-600">Admin</span></span>
        </router-link>
        <div class="ml-auto flex items-center gap-2">
          <router-link :to="{ name: 'home' }" class="btn-secondary hidden text-sm sm:inline-flex">← Storefront</router-link>
          <span class="hidden text-sm text-slate-500 sm:inline">{{ auth.user?.email }}</span>
          <button class="btn-danger text-sm" data-testid="admin-logout" @click="signOut">Sign out</button>
        </div>
      </div>
    </header>

    <div class="flex">
      <!-- Sidebar -->
      <aside
        class="fixed inset-y-14 left-0 z-30 w-56 border-r border-slate-200 bg-white transition-transform lg:static lg:translate-x-0"
        :class="open ? 'translate-x-0' : '-translate-x-full'"
      >
        <nav class="space-y-1 p-3">
          <router-link
            v-for="item in nav"
            :key="item.name"
            :to="item.to"
            class="flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50"
            active-class="!bg-brand-50 !text-brand-700"
            :data-testid="`admin-nav-${item.name.toLowerCase()}`"
            @click="open = false"
          >
            <span>{{ item.icon }}</span> {{ item.name }}
          </router-link>
        </nav>
        <div class="border-t border-slate-100 p-3">
          <p class="text-xs text-slate-400">Signed in as</p>
          <p class="truncate text-sm font-medium text-slate-700">{{ auth.user?.fullName }}</p>
          <p class="truncate text-xs text-slate-400">{{ auth.user?.roles.join(', ') }}</p>
        </div>
      </aside>

      <!-- Content -->
      <main class="min-w-0 flex-1 p-4 lg:p-8" data-testid="admin-content">
        <router-view />
      </main>
    </div>
  </div>
</template>

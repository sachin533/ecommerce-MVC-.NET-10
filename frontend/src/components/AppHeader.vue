<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useWishlistStore } from '@/stores/wishlist'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const cart = useCartStore()
const wishlist = useWishlistStore()

const search = ref((route.query.search as string) ?? '')
const mobileOpen = ref(false)

const navLinks = [
  { name: 'Home', to: { name: 'home' } },
  { name: 'Shop', to: { name: 'shop' } },
]

const initials = computed(() =>
  (auth.fullName || auth.user?.email || '?')
    .split(/[\s@.]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join(''),
)

function submitSearch() {
  router.push({ name: 'shop', query: search.value.trim() ? { search: search.value.trim() } : {} })
  mobileOpen.value = false
}

async function signOut() {
  await auth.logout()
  wishlist.reset()
  await cart.fetch() // back to the guest cart
  router.push({ name: 'home' })
}
</script>

<template>
  <header class="sticky top-0 z-40 border-b border-slate-200 bg-white/95 backdrop-blur">
    <div class="mx-auto flex h-16 max-w-7xl items-center gap-4 px-4 sm:px-6">
      <!-- Brand -->
      <router-link :to="{ name: 'home' }" class="flex items-center gap-2">
        <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-brand-600 text-white shadow-sm">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-5 w-5">
            <path d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13 5.4 5M7 13l-2.3 2.3A1 1 0 0 0 5.4 17H17" stroke-linecap="round" stroke-linejoin="round" />
            <circle cx="9" cy="20" r="1" /><circle cx="17" cy="20" r="1" />
          </svg>
        </span>
        <span class="text-xl font-bold tracking-tight text-slate-900">Quick<span class="text-brand-600">Shop</span></span>
      </router-link>

      <!-- Desktop nav -->
      <nav class="hidden items-center gap-1 md:flex">
        <router-link
          v-for="link in navLinks"
          :key="link.name"
          :to="link.to"
          class="rounded-lg px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 hover:text-slate-900"
          active-class="!bg-brand-50 !text-brand-700"
        >
          {{ link.name }}
        </router-link>
        <router-link
          v-if="auth.isAdmin"
          :to="{ name: 'admin-dashboard' }"
          class="rounded-lg px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 hover:text-slate-900"
          active-class="!bg-brand-50 !text-brand-700"
        >
          Admin
        </router-link>
      </nav>

      <!-- Search -->
      <form class="relative ml-auto hidden max-w-xs flex-1 md:block" @submit.prevent="submitSearch">
        <input
          v-model="search"
          type="search"
          placeholder="Search products…"
          aria-label="Search products"
          class="input pr-9"
          data-testid="header-search"
        />
        <button type="submit" class="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600" aria-label="Search">
          <svg viewBox="0 0 20 20" fill="currentColor" class="h-5 w-5"><path fill-rule="evenodd" d="M9 3.5a5.5 5.5 0 1 0 3.4 9.8l3.1 3.1a1 1 0 0 0 1.4-1.4l-3.1-3.1A5.5 5.5 0 0 0 9 3.5ZM6.5 9a2.5 2.5 0 1 1 5 0 2.5 2.5 0 0 1-5 0Z" clip-rule="evenodd" /></svg>
        </button>
      </form>

      <!-- Actions -->
      <div class="flex items-center gap-1">
        <router-link
          :to="{ name: 'wishlist' }"
          class="relative rounded-lg p-2 text-slate-600 hover:bg-slate-100"
          aria-label="Wishlist"
          data-testid="wishlist-link"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-6 w-6">
            <path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1-1.1a5.5 5.5 0 0 0-7.8 7.8l1 1L12 21l7.8-7.6 1-1a5.5 5.5 0 0 0 0-7.8Z" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
          <span
            v-if="wishlist.count > 0"
            class="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-rose-500 px-1 text-[10px] font-bold text-white"
            data-testid="wishlist-count"
          >
            {{ wishlist.count }}
          </span>
        </router-link>

        <router-link
          :to="{ name: 'cart' }"
          class="relative rounded-lg p-2 text-slate-600 hover:bg-slate-100"
          aria-label="Cart"
          data-testid="cart-link"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-6 w-6">
            <path d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13 5.4 5M7 13l-2.3 2.3A1 1 0 0 0 5.4 17H17" stroke-linecap="round" stroke-linejoin="round" />
            <circle cx="9" cy="20" r="1" /><circle cx="17" cy="20" r="1" />
          </svg>
          <span
            v-if="cart.itemCount > 0"
            class="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-brand-600 px-1 text-[10px] font-bold text-white"
            data-testid="cart-count"
          >
            {{ cart.itemCount }}
          </span>
        </router-link>

        <!-- Account -->
        <div v-if="auth.isAuthenticated" class="relative ml-1 group">
          <button
            class="flex h-9 w-9 items-center justify-center rounded-full bg-brand-100 text-sm font-bold text-brand-700 ring-brand-200 hover:ring-2"
            data-testid="account-menu"
            aria-label="Account menu"
          >
            {{ initials }}
          </button>
          <div class="invisible absolute right-0 top-full w-48 pt-2 opacity-0 transition group-hover:visible group-hover:opacity-100">
            <div class="rounded-xl border border-slate-200 bg-white py-1 shadow-lg">
              <router-link :to="{ name: 'profile' }" class="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50">Profile</router-link>
              <router-link :to="{ name: 'orders' }" class="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50">My orders</router-link>
              <router-link :to="{ name: 'addresses' }" class="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50">Addresses</router-link>
              <button class="block w-full px-4 py-2 text-left text-sm text-rose-600 hover:bg-rose-50" data-testid="logout" @click="signOut">
                Sign out
              </button>
            </div>
          </div>
        </div>
        <router-link v-else :to="{ name: 'login' }" class="btn-primary ml-1 hidden sm:inline-flex" data-testid="login-link">
          Sign in
        </router-link>

        <!-- Mobile toggle -->
        <button
          class="rounded-lg p-2 text-slate-600 hover:bg-slate-100 md:hidden"
          aria-label="Menu"
          data-testid="mobile-menu-toggle"
          @click="mobileOpen = !mobileOpen"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-6 w-6"><path d="M4 6h16M4 12h16M4 18h16" stroke-linecap="round" /></svg>
        </button>
      </div>
    </div>

    <!-- Mobile panel -->
    <div v-if="mobileOpen" class="border-t border-slate-200 bg-white px-4 py-3 md:hidden">
      <form class="mb-3" @submit.prevent="submitSearch">
        <input v-model="search" type="search" placeholder="Search products…" aria-label="Search products" class="input" />
      </form>
      <nav class="flex flex-col gap-1">
        <router-link v-for="link in navLinks" :key="link.name" :to="link.to" class="rounded-lg px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50" @click="mobileOpen = false">
          {{ link.name }}
        </router-link>
        <router-link v-if="auth.isAdmin" :to="{ name: 'admin-dashboard' }" class="rounded-lg px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50" @click="mobileOpen = false">
          Admin
        </router-link>
        <template v-if="auth.isAuthenticated">
          <router-link :to="{ name: 'profile' }" class="rounded-lg px-3 py-2 text-sm text-slate-700 hover:bg-slate-50" @click="mobileOpen = false">Profile</router-link>
          <router-link :to="{ name: 'orders' }" class="rounded-lg px-3 py-2 text-sm text-slate-700 hover:bg-slate-50" @click="mobileOpen = false">My orders</router-link>
          <button class="rounded-lg px-3 py-2 text-left text-sm text-rose-600 hover:bg-rose-50" @click="signOut(); mobileOpen = false">Sign out</button>
        </template>
        <router-link v-else :to="{ name: 'login' }" class="btn-primary mt-1" @click="mobileOpen = false">Sign in</router-link>
      </nav>
    </div>
  </header>
</template>

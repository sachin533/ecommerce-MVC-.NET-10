<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { categoryService } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useWishlistStore } from '@/stores/wishlist'
import CategoryIcon from '@/components/CategoryIcon.vue'
import type { Category } from '@/types'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const cart = useCartStore()
const wishlist = useWishlistStore()

const search = ref((route.query.search as string) ?? '')
const mobileOpen = ref(false)
const categoriesOpen = ref(false)
const categories = ref<Category[]>([])

const navLinks = [
  { name: 'Home', to: { name: 'home' } },
  { name: 'Shop', to: { name: 'shop' } },
  { name: 'Deals', to: { name: 'deals' } },
  { name: 'New Arrivals', to: { name: 'new-arrivals' } },
  { name: 'Brands', to: { name: 'brands' } },
]

const initials = computed(() =>
  (auth.fullName || auth.user?.email || '?')
    .split(/[\s@.]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join(''),
)

onMounted(async () => {
  try {
    categories.value = (await categoryService.list()).filter((c) => c.isActive)
  } catch {
    categories.value = []
  }
})

function submitSearch() {
  router.push({ name: 'shop', query: search.value.trim() ? { search: search.value.trim() } : {} })
  mobileOpen.value = false
}

async function signOut() {
  await auth.logout()
  wishlist.reset()
  await cart.fetch()
  router.push({ name: 'home' })
}
</script>

<template>
  <header class="sticky top-0 z-40 border-b border-slate-200 bg-white/95 backdrop-blur" data-testid="app-header">
    <div class="mx-auto flex max-w-7xl items-center gap-3 px-4 py-3 sm:px-6">
      <!-- Logo -->
      <router-link :to="{ name: 'home' }" class="flex shrink-0 items-center gap-2" data-testid="header-logo" aria-label="QuickShop home">
        <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-brand-600 text-white">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-5 w-5">
            <path d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13 5.4 5M7 13l-2.3 2.3A1 1 0 0 0 5.4 17H17" stroke-linecap="round" stroke-linejoin="round" />
            <circle cx="9" cy="20" r="1" /><circle cx="17" cy="20" r="1" />
          </svg>
        </span>
        <span class="text-lg font-extrabold tracking-tight text-slate-900">Quick<span class="text-brand-600">Shop</span></span>
      </router-link>

      <!-- Primary nav -->
      <nav class="ml-4 hidden items-center gap-1 md:flex" aria-label="Primary">
        <router-link
          v-for="link in navLinks.slice(0, 2)"
          :key="link.name"
          :to="link.to"
          class="rounded-lg px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-100 hover:text-brand-700"
          :class="{ 'text-brand-700': route.name === link.to.name }"
        >
          {{ link.name }}
        </router-link>

        <!-- Categories dropdown -->
        <div class="relative" @focusin="categoriesOpen = true" @focusout="categoriesOpen = false" @mouseenter="categoriesOpen = true" @mouseleave="categoriesOpen = false">
          <button
            class="flex items-center gap-1 rounded-lg px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-100 hover:text-brand-700"
            :aria-expanded="categoriesOpen"
            aria-haspopup="true"
            data-testid="nav-categories"
            @click="categoriesOpen = !categoriesOpen"
          >
            Categories
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-3.5 w-3.5"><path d="M6 9l6 6 6-6" stroke-linecap="round" stroke-linejoin="round" /></svg>
          </button>
          <div
            v-show="categoriesOpen"
            class="absolute left-0 top-full z-50 mt-1 w-[34rem] rounded-xl border border-slate-200 bg-white p-3 shadow-xl"
            data-testid="categories-dropdown"
          >
            <div class="grid grid-cols-3 gap-1">
              <router-link
                v-for="category in categories"
                :key="category.slug"
                :to="{ name: 'category', params: { slug: category.slug } }"
                class="flex items-center gap-2.5 rounded-lg px-2.5 py-2 text-sm text-slate-700 transition hover:bg-brand-50 hover:text-brand-700"
                :data-testid="`category-${category.slug}`"
                @click="categoriesOpen = false"
              >
                <span class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-slate-100 text-slate-600">
                  <CategoryIcon :slug="category.slug" :label="category.name" />
                </span>
                <span class="truncate">{{ category.name }}</span>
              </router-link>
            </div>
            <router-link
              :to="{ name: 'shop' }"
              class="mt-2 block rounded-lg bg-slate-50 px-3 py-2 text-center text-xs font-semibold text-brand-700 hover:bg-brand-50"
              @click="categoriesOpen = false"
            >
              Browse all products →
            </router-link>
          </div>
        </div>

        <router-link
          v-for="link in navLinks.slice(2)"
          :key="link.name"
          :to="link.to"
          class="rounded-lg px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-100 hover:text-brand-700"
          :class="{ 'text-brand-700': route.name === link.to.name }"
        >
          {{ link.name }}
        </router-link>
      </nav>

      <!-- Search -->
      <form class="relative ml-auto hidden max-w-xs flex-1 lg:block" role="search" @submit.prevent="submitSearch">
        <input
          v-model="search"
          type="search"
          placeholder="Search products, brands…"
          aria-label="Search products"
          class="w-full rounded-full border border-slate-300 bg-slate-50 py-2 pl-10 pr-4 text-sm transition placeholder:text-slate-400 focus:border-brand-500 focus:bg-white focus:outline-none focus:ring-2 focus:ring-brand-200"
          data-testid="header-search"
        />
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" aria-hidden="true">
          <circle cx="11" cy="11" r="7" /><path d="M20 20l-3.5-3.5" stroke-linecap="round" />
        </svg>
      </form>

      <div class="flex shrink-0 items-center gap-1">
        <!-- Wishlist -->
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

        <!-- Cart -->
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
          <div class="invisible absolute right-0 top-full w-48 pt-2 opacity-0 transition group-hover:visible group-hover:opacity-100 group-focus-within:visible group-focus-within:opacity-100">
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
          :aria-expanded="mobileOpen"
          data-testid="mobile-menu-toggle"
          @click="mobileOpen = !mobileOpen"
        >
          <svg v-if="!mobileOpen" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-6 w-6"><path d="M4 6h16M4 12h16M4 18h16" stroke-linecap="round" /></svg>
          <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-6 w-6"><path d="M6 6l12 12M18 6L6 18" stroke-linecap="round" /></svg>
        </button>
      </div>
    </div>

    <!-- Mobile panel -->
    <div v-if="mobileOpen" class="border-t border-slate-200 bg-white px-4 py-3 md:hidden" data-testid="mobile-panel">
      <form class="relative mb-3" role="search" @submit.prevent="submitSearch">
        <input v-model="search" type="search" placeholder="Search products…" aria-label="Search products" class="input pl-10" data-testid="mobile-search" />
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400"><circle cx="11" cy="11" r="7" /><path d="M20 20l-3.5-3.5" stroke-linecap="round" /></svg>
      </form>
      <nav class="flex flex-col gap-1" aria-label="Mobile">
        <router-link v-for="link in navLinks" :key="link.name" :to="link.to" class="rounded-lg px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50" @click="mobileOpen = false">
          {{ link.name }}
        </router-link>
        <p class="mt-2 px-3 text-xs font-semibold uppercase tracking-wide text-slate-400">Categories</p>
        <router-link
          v-for="category in categories"
          :key="`m-${category.slug}`"
          :to="{ name: 'category', params: { slug: category.slug } }"
          class="rounded-lg px-3 py-2 text-sm text-slate-700 hover:bg-slate-50"
          :data-testid="`category-${category.slug}`"
          @click="mobileOpen = false"
        >
          {{ category.name }}
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

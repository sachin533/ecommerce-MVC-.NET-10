<script setup lang="ts">
import { ref } from 'vue'

const year = new Date().getFullYear()
const email = ref('')

const links = [
  { label: 'Shop all', to: { name: 'shop' } },
  { label: 'My orders', to: { name: 'orders' } },
  { label: 'Wishlist', to: { name: 'wishlist' } },
  { label: 'Cart', to: { name: 'cart' } },
]
</script>

<template>
  <footer class="mt-16 border-t border-slate-200 bg-white">
    <div class="mx-auto grid max-w-7xl gap-8 px-4 py-10 sm:grid-cols-2 sm:px-6 lg:grid-cols-4">
      <div>
        <div class="flex items-center gap-2">
          <span class="flex h-8 w-8 items-center justify-center rounded-lg bg-brand-600 text-white">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-4 w-4">
              <path d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13 5.4 5M7 13l-2.3 2.3A1 1 0 0 0 5.4 17H17" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
          </span>
          <span class="text-lg font-bold text-slate-900">QuickShop</span>
        </div>
        <p class="mt-3 text-sm text-slate-500">
          A modern full-stack eCommerce demo: Vue 3 + ASP.NET Core Minimal API. All payments are handled by a clearly labelled mock provider.
        </p>
      </div>

      <div>
        <h3 class="text-sm font-semibold text-slate-900">Explore</h3>
        <ul class="mt-3 space-y-2">
          <li v-for="link in links" :key="link.label">
            <router-link :to="link.to" class="text-sm text-slate-500 hover:text-brand-600">{{ link.label }}</router-link>
          </li>
        </ul>
      </div>

      <div>
        <h3 class="text-sm font-semibold text-slate-900">Support</h3>
        <ul class="mt-3 space-y-2 text-sm text-slate-500">
          <li>support@quickshop.dev</li>
          <li>+91 98765 43210</li>
          <li>Mon–Sat, 9:00–21:00 IST</li>
        </ul>
      </div>

      <div>
        <h3 class="text-sm font-semibold text-slate-900">Get deals updates</h3>
        <form class="mt-3 flex gap-2" @submit.prevent="email = ''">
          <input v-model="email" type="email" placeholder="you@example.com" aria-label="Email address" class="input" required />
          <button class="btn-primary shrink-0" type="submit">Join</button>
        </form>
        <p class="mt-2 text-xs text-slate-400">Demo only — nothing is stored.</p>
      </div>
    </div>
    <div class="border-t border-slate-100 py-4 text-center text-xs text-slate-400">
      © {{ year }} QuickShop demo. Prices in ₹ (INR). Built with Vue 3, Vite & ASP.NET Core.
    </div>
  </footer>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useRoute } from 'vue-router'
import AppHeader from '@/components/AppHeader.vue'
import AppFooter from '@/components/AppFooter.vue'
import ToastHost from '@/components/ToastHost.vue'
import { useCartStore } from '@/stores/cart'
import { useWishlistStore } from '@/stores/wishlist'

const route = useRoute()
const cart = useCartStore()
const wishlist = useWishlistStore()

const isAdminRoute = () => route.path.startsWith('/admin')

onMounted(async () => {
  // Initial cart + wishlist hydration (guest cart resolves via X-Cart-Id).
  await Promise.allSettled([cart.fetch(), wishlist.fetch()])
})
</script>

<template>
  <div class="flex min-h-screen flex-col">
    <AppHeader v-if="!isAdminRoute()" />
    <main class="flex-1">
      <router-view />
    </main>
    <AppFooter v-if="!isAdminRoute()" />
    <ToastHost />
  </div>
</template>

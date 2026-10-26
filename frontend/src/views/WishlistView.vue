<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import ProductCard from '@/components/ProductCard.vue'
import { useAuthStore } from '@/stores/auth'
import { useWishlistStore } from '@/stores/wishlist'

const router = useRouter()
const auth = useAuthStore()
const wishlist = useWishlistStore()

onMounted(async () => {
  if (!auth.isAuthenticated) {
    router.push({ name: 'login', query: { redirect: '/wishlist' } })
    return
  }
  await wishlist.fetch(true)
})
</script>

<template>
  <div class="mx-auto max-w-7xl px-4 py-8 sm:px-6">
    <h1 class="mb-6 text-2xl font-bold text-slate-900" data-testid="wishlist-title">My wishlist</h1>

    <div v-if="wishlist.items.length === 0" class="rounded-xl border border-dashed border-slate-300 p-16 text-center" data-testid="empty-wishlist">
      <p class="text-lg font-semibold text-slate-700">Your wishlist is empty</p>
      <p class="mt-1 text-sm text-slate-500">Tap the heart on any product to save it for later.</p>
      <router-link :to="{ name: 'shop' }" class="btn-primary mt-4">Browse products</router-link>
    </div>

    <div v-else class="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-4">
      <ProductCard v-for="product in wishlist.items" :key="product.id" :product="product" />
    </div>
  </div>
</template>

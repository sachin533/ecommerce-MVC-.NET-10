<script setup lang="ts">
import { useRouter } from 'vue-router'
import ProductImage from '@/components/ProductImage.vue'
import PriceDisplay from '@/components/PriceDisplay.vue'
import StarRating from '@/components/StarRating.vue'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useToastStore } from '@/stores/toast'
import { useWishlistStore } from '@/stores/wishlist'
import { ApiError } from '@/services/http'
import type { ProductSummary } from '@/types'

const props = defineProps<{ product: ProductSummary }>()

const router = useRouter()
const auth = useAuthStore()
const cart = useCartStore()
const wishlist = useWishlistStore()
const toasts = useToastStore()

function goDetail() {
  router.push({ name: 'product', params: { slug: props.product.slug } })
}

async function addToCart(event: Event) {
  event.stopPropagation()
  try {
    await cart.add(props.product.id, 1)
    toasts.success(`Added “${props.product.name}” to cart`)
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not add to cart')
  }
}

async function toggleWishlist(event: Event) {
  event.stopPropagation()
  if (!auth.isAuthenticated) {
    router.push({ name: 'login', query: { redirect: '/wishlist' } })
    return
  }
  try {
    await wishlist.toggle(props.product)
    toasts.info(wishlist.has(props.product.id) ? 'Saved to wishlist' : 'Removed from wishlist')
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not update wishlist')
  }
}
</script>

<template>
  <article
    class="card group relative flex cursor-pointer flex-col overflow-hidden transition hover:-translate-y-0.5 hover:shadow-md"
    data-testid="product-card"
    @click="goDetail"
  >
    <div class="relative aspect-square overflow-hidden bg-slate-100">
      <ProductImage :src="product.imageUrl" :alt="product.name" img-class="h-full w-full object-cover transition duration-300 group-hover:scale-105" />
      <span v-if="!product.isAvailable" class="absolute left-2 top-2 badge bg-slate-800/90 text-white">Out of stock</span>
      <span v-else-if="product.stock <= 5" class="absolute left-2 top-2 badge bg-amber-100 text-amber-800">Only {{ product.stock }} left</span>
      <button
        class="absolute right-2 top-2 rounded-full bg-white/90 p-2 text-slate-500 shadow-sm transition hover:text-rose-500"
        :aria-label="wishlist.has(product.id) ? 'Remove from wishlist' : 'Add to wishlist'"
        data-testid="wishlist-toggle"
        @click="toggleWishlist"
      >
        <svg viewBox="0 0 24 24" :fill="wishlist.has(product.id) ? 'currentColor' : 'none'" stroke="currentColor" stroke-width="2" class="h-5 w-5" :class="wishlist.has(product.id) ? 'text-rose-500' : ''">
          <path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1-1.1a5.5 5.5 0 0 0-7.8 7.8l1 1L12 21l7.8-7.6 1-1a5.5 5.5 0 0 0 0-7.8Z" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </button>
    </div>

    <div class="flex flex-1 flex-col p-3">
      <p class="text-xs font-medium uppercase tracking-wide text-slate-400">{{ product.categoryName || 'General' }}</p>
      <h3 class="mt-0.5 line-clamp-2 text-sm font-semibold text-slate-900" data-testid="product-name">{{ product.name }}</h3>
      <p class="mt-0.5 text-xs text-slate-500">{{ product.brand }}</p>
      <div class="mt-1"><StarRating :rating="product.rating" :count="product.ratingCount" /></div>
      <div class="mt-2 flex items-end justify-between gap-2">
        <PriceDisplay :price="product.salePrice" :mrp="product.price" :discount-percent="product.discountPercent" />
      </div>
      <button
        class="btn-primary mt-3 w-full"
        :disabled="!product.isAvailable"
        data-testid="add-to-cart"
        @click="addToCart"
      >
        {{ product.isAvailable ? 'Add to cart' : 'Out of stock' }}
      </button>
    </div>
  </article>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
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

/** Add-to-cart button state machine: idle → adding → added (then back). */
const addState = ref<'idle' | 'adding' | 'added'>('idle')
let resetTimer: ReturnType<typeof setTimeout> | undefined

const soldOut = computed(() => !props.product.isAvailable || props.product.stock <= 0)

function goDetail() {
  router.push({ name: 'product', params: { slug: props.product.slug } })
}

async function addToCart(event: Event) {
  event.stopPropagation()
  if (addState.value !== 'idle') return
  addState.value = 'adding'
  try {
    await cart.add(props.product.id, 1)
    addState.value = 'added'
    toasts.success(`Added “${props.product.name}” to cart`)
    if (resetTimer) clearTimeout(resetTimer)
    resetTimer = setTimeout(() => (addState.value = 'idle'), 1600)
  } catch (error) {
    addState.value = 'idle'
    toasts.error(error instanceof ApiError ? error.message : 'Could not add to cart')
  }
}

async function toggleWishlist(event: Event) {
  event.stopPropagation()
  if (!auth.isAuthenticated) {
    router.push({ name: 'login', query: { redirect: '/wishlist' } })
    return
  }
  await wishlist.toggle(props.product)
  toasts.info(wishlist.has(props.product.id) ? 'Saved to wishlist' : 'Removed from wishlist')
}

const addLabel = computed(() => {
  if (soldOut.value) return 'Out of stock'
  if (addState.value === 'adding') return 'Adding…'
  if (addState.value === 'added') return 'Added ✓'
  return 'Add to cart'
})
</script>

<template>
  <article
    class="group relative flex h-full flex-col overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm transition duration-200 hover:-translate-y-0.5 hover:border-slate-300 hover:shadow-lg"
    :data-testid="`product-card`"
    role="link"
    tabindex="0"
    :aria-label="`View ${product.name}`"
    @click="goDetail"
    @keydown.enter="goDetail"
  >
    <!-- Image area: fixed aspect, contained image, discount + stock badges -->
    <div class="relative aspect-square overflow-hidden bg-slate-50">
      <ProductImage
        :src="product.imageUrl"
        :alt="`${product.name} — ${product.brand}`"
        img-class="h-full w-full object-contain p-4 transition duration-300 group-hover:scale-105"
      />

      <span
        v-if="product.discountPercent > 0"
        class="absolute left-3 top-3 rounded-full bg-rose-600 px-2.5 py-1 text-[11px] font-bold text-white shadow-sm"
        data-testid="discount-chip"
      >
        {{ Math.round(product.discountPercent) }}% OFF
      </span>
      <span
        v-if="soldOut"
        class="absolute left-3 top-3 rounded-full bg-slate-800/90 px-2.5 py-1 text-[11px] font-bold text-white"
      >
        Sold out
      </span>

      <!-- Wishlist heart -->
      <button
        class="absolute right-3 top-3 flex h-9 w-9 items-center justify-center rounded-full bg-white/90 text-slate-500 shadow-sm backdrop-blur transition hover:scale-110 hover:text-rose-500"
        :aria-label="wishlist.has(product.id) ? 'Remove from wishlist' : 'Add to wishlist'"
        :aria-pressed="wishlist.has(product.id)"
        data-testid="wishlist-toggle"
        @click="toggleWishlist"
      >
        <svg
          viewBox="0 0 24 24"
          :fill="wishlist.has(product.id) ? 'currentColor' : 'none'"
          stroke="currentColor"
          stroke-width="2"
          class="h-5 w-5"
          :class="wishlist.has(product.id) ? 'text-rose-500' : ''"
        >
          <path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1-1.1a5.5 5.5 0 0 0-7.8 7.8l1 1L12 21l7.8-7.6 1-1a5.5 5.5 0 0 0 0-7.8Z" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </button>
    </div>

    <!-- Body -->
    <div class="flex flex-1 flex-col p-4">
      <p class="text-[11px] font-semibold uppercase tracking-wide text-slate-400">
        {{ product.categoryName }} · {{ product.brand }}
      </p>
      <h3
        class="mt-1 line-clamp-2 min-h-[2.5rem] text-sm font-semibold leading-snug text-slate-900 group-hover:text-brand-700"
        data-testid="product-name"
      >
        {{ product.name }}
      </h3>

      <div class="mt-1.5">
        <StarRating :rating="product.rating" :count="product.ratingCount" />
      </div>

      <div class="mt-2">
        <PriceDisplay :price="product.salePrice" :mrp="product.price" :discount-percent="product.discountPercent" />
      </div>

      <p class="mt-1.5 text-xs" :class="soldOut ? 'text-slate-400' : product.stock <= 5 ? 'text-amber-600' : 'text-emerald-600'">
        <template v-if="soldOut">Out of stock</template>
        <template v-else-if="product.stock <= 5">Only {{ product.stock }} left</template>
        <template v-else>In stock</template>
      </p>

      <button
        class="mt-3 w-full rounded-lg px-4 py-2.5 text-sm font-semibold transition disabled:cursor-not-allowed"
        :class="
          soldOut
            ? 'bg-slate-100 text-slate-400'
            : addState === 'added'
              ? 'bg-emerald-600 text-white'
              : 'bg-brand-600 text-white hover:bg-brand-700'
        "
        :disabled="soldOut || addState !== 'idle'"
        data-testid="add-to-cart"
        @click="addToCart"
      >
        <span v-if="addState === 'adding'" class="inline-flex items-center gap-2">
          <svg class="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 0 1 8-8v4a4 4 0 0 0-4 4H4z" />
          </svg>
          Adding…
        </span>
        <template v-else>{{ addLabel }}</template>
      </button>
    </div>
  </article>
</template>

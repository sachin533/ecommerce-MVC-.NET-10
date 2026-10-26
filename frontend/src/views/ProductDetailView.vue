<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { productService } from '@/services/api'
import ProductImage from '@/components/ProductImage.vue'
import PriceDisplay from '@/components/PriceDisplay.vue'
import StarRating from '@/components/StarRating.vue'
import ProductCard from '@/components/ProductCard.vue'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useToastStore } from '@/stores/toast'
import { useWishlistStore } from '@/stores/wishlist'
import { ApiError } from '@/services/http'
import type { ProductDetail, ProductSummary } from '@/types'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const cart = useCartStore()
const wishlist = useWishlistStore()
const toasts = useToastStore()

const product = ref<ProductDetail | null>(null)
const related = ref<ProductSummary[]>([])
const loading = ref(true)
const notFound = ref(false)
const quantity = ref(1)
const selectedVariantId = ref<number | null>(null)
const activeImage = ref('')
const adding = ref(false)

const selectedVariant = computed(
  () => product.value?.variants.find((v) => v.id === selectedVariantId.value) ?? null,
)

/** Unit price reflects the selected variant (server-derived). */
const unitPrice = computed(() => selectedVariant.value?.effectivePrice ?? product.value?.salePrice ?? 0)
const inStock = computed(() =>
  selectedVariant.value ? selectedVariant.value.stock > 0 : (product.value?.isAvailable ?? false),
)
const stockLeft = computed(() => selectedVariant.value?.stock ?? product.value?.stock ?? 0)

async function load(slug: string) {
  loading.value = true
  notFound.value = false
  quantity.value = 1
  selectedVariantId.value = null
  try {
    product.value = await productService.bySlug(slug)
    activeImage.value = product.value.images[0]?.url ?? product.value.imageUrl
    const siblings = await productService.list({ categorySlug: product.value.categorySlug, pageSize: 8 })
    related.value = siblings.items.filter((p) => p.id !== product.value!.id).slice(0, 4)
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) notFound.value = true
    else toasts.error('Could not load the product')
  } finally {
    loading.value = false
  }
}

function changeQuantity(delta: number) {
  const max = Math.min(stockLeft.value || 1, 20)
  quantity.value = Math.min(Math.max(1, quantity.value + delta), max)
}

async function addToCart() {
  if (!product.value) return
  adding.value = true
  try {
    await cart.add(product.value.id, quantity.value, selectedVariantId.value)
    toasts.success(`Added ${quantity.value} × ${product.value.name} to cart`)
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not add to cart')
  } finally {
    adding.value = false
  }
}

async function buyNow() {
  await addToCart()
  if (cart.itemCount > 0) router.push({ name: 'cart' })
}

async function toggleWishlist() {
  if (!product.value) return
  if (!auth.isAuthenticated) {
    router.push({ name: 'login', query: { redirect: route.fullPath } })
    return
  }
  await wishlist.toggle(product.value)
  toasts.info(wishlist.has(product.value.id) ? 'Saved to wishlist' : 'Removed from wishlist')
}

watch(
  () => route.params.slug,
  (slug) => {
    if (typeof slug === 'string') load(slug)
  },
)

onMounted(() => {
  const slug = route.params.slug
  if (typeof slug === 'string') load(slug)
})
</script>

<template>
  <div class="mx-auto max-w-7xl px-4 py-8 sm:px-6">
    <!-- Breadcrumb -->
    <nav class="mb-4 text-sm text-slate-500" aria-label="Breadcrumb">
      <router-link :to="{ name: 'home' }" class="hover:text-brand-600">Home</router-link>
      <span class="mx-1">/</span>
      <router-link :to="{ name: 'shop' }" class="hover:text-brand-600">Shop</router-link>
      <template v-if="product">
        <span class="mx-1">/</span>
        <router-link :to="{ name: 'category', params: { slug: product.categorySlug } }" class="hover:text-brand-600">
          {{ product.categoryName }}
        </router-link>
      </template>
    </nav>

    <div v-if="loading" class="grid gap-8 lg:grid-cols-2">
      <div class="aspect-square animate-pulse rounded-2xl bg-slate-200" />
      <div class="space-y-4">
        <div class="h-8 w-3/4 animate-pulse rounded bg-slate-200" />
        <div class="h-4 w-1/3 animate-pulse rounded bg-slate-200" />
        <div class="h-24 animate-pulse rounded bg-slate-200" />
      </div>
    </div>

    <div v-else-if="notFound" class="rounded-xl border border-dashed border-slate-300 p-16 text-center" data-testid="product-not-found">
      <p class="text-lg font-semibold text-slate-700">Product not found</p>
      <p class="mt-1 text-sm text-slate-500">It may have been removed or the link is incorrect.</p>
      <router-link :to="{ name: 'shop' }" class="btn-primary mt-4">Back to shop</router-link>
    </div>

    <template v-else-if="product">
      <div class="grid gap-8 lg:grid-cols-2">
        <!-- Gallery -->
        <div>
          <div class="aspect-square overflow-hidden rounded-2xl border border-slate-200 bg-slate-100">
            <ProductImage :src="activeImage" :alt="product.name" />
          </div>
          <div v-if="product.images.length > 1" class="mt-3 flex gap-2 overflow-x-auto">
            <button
              v-for="image in product.images"
              :key="image.id"
              class="h-20 w-20 shrink-0 overflow-hidden rounded-lg border-2 transition"
              :class="activeImage === image.url ? 'border-brand-500' : 'border-transparent hover:border-slate-300'"
              @click="activeImage = image.url"
            >
              <ProductImage :src="image.url" :alt="`${product.name} thumbnail`" />
            </button>
          </div>
        </div>

        <!-- Info -->
        <div>
          <p class="text-sm font-medium uppercase tracking-wide text-brand-600">{{ product.categoryName }}</p>
          <h1 class="mt-1 text-3xl font-bold text-slate-900" data-testid="product-title">{{ product.name }}</h1>
          <p class="mt-1 text-sm text-slate-500">{{ product.brand }} · SKU {{ product.sku }}</p>

          <div class="mt-3 flex items-center gap-3">
            <StarRating :rating="product.rating" :count="product.ratingCount" />
            <span v-if="!product.isAvailable" class="badge bg-slate-100 text-slate-600">Out of stock</span>
            <span v-else-if="product.stock <= 5" class="badge bg-amber-100 text-amber-800" data-testid="low-stock">Only {{ product.stock }} left</span>
            <span v-else class="badge bg-emerald-100 text-emerald-700">In stock</span>
          </div>

          <div class="mt-5 border-y border-slate-100 py-4">
            <PriceDisplay :price="unitPrice" :mrp="product.price" :discount-percent="product.discountPercent" size="lg" />
            <p class="mt-1 text-xs text-slate-400">Inclusive of all taxes · Free shipping over ₹999</p>
          </div>

          <!-- Variants -->
          <div v-if="product.variants.length > 0" class="mt-5">
            <h2 class="mb-2 text-sm font-semibold text-slate-700">Choose an option</h2>
            <div class="flex flex-wrap gap-2" data-testid="variant-list">
              <button
                v-for="variant in product.variants"
                :key="variant.id"
                class="rounded-lg border px-3 py-2 text-sm transition disabled:cursor-not-allowed disabled:opacity-40"
                :class="selectedVariantId === variant.id ? 'border-brand-500 bg-brand-50 font-semibold text-brand-700' : 'border-slate-300 text-slate-700 hover:border-slate-400'"
                :disabled="variant.stock <= 0"
                :data-testid="`variant-${variant.name}`"
                @click="selectedVariantId = selectedVariantId === variant.id ? null : variant.id"
              >
                {{ variant.name }}
                <span class="block text-xs text-slate-400">+₹{{ variant.priceDelta }}</span>
              </button>
            </div>
          </div>

          <!-- Quantity + actions -->
          <div class="mt-6 flex flex-wrap items-center gap-4">
            <div class="flex items-center rounded-lg border border-slate-300">
              <button class="px-3 py-2 text-slate-600 hover:bg-slate-50 disabled:opacity-40" aria-label="Decrease quantity" :disabled="quantity <= 1" @click="changeQuantity(-1)">−</button>
              <span class="w-10 text-center text-sm font-semibold" data-testid="quantity">{{ quantity }}</span>
              <button class="px-3 py-2 text-slate-600 hover:bg-slate-50 disabled:opacity-40" aria-label="Increase quantity" :disabled="quantity >= Math.min(stockLeft || 1, 20)" @click="changeQuantity(1)">+</button>
            </div>
            <button class="btn-primary flex-1 px-8 py-3" :disabled="!inStock || adding" data-testid="add-to-cart" @click="addToCart">
              {{ adding ? 'Adding…' : inStock ? 'Add to cart' : 'Out of stock' }}
            </button>
            <button class="btn-secondary px-6 py-3" :disabled="!inStock" @click="buyNow">Buy now</button>
            <button
              class="rounded-lg border border-slate-300 p-3 text-slate-500 transition hover:text-rose-500"
              :aria-label="wishlist.has(product.id) ? 'Remove from wishlist' : 'Add to wishlist'"
              data-testid="wishlist-toggle"
              @click="toggleWishlist"
            >
              <svg viewBox="0 0 24 24" :fill="wishlist.has(product.id) ? 'currentColor' : 'none'" stroke="currentColor" stroke-width="2" class="h-5 w-5" :class="wishlist.has(product.id) ? 'text-rose-500' : ''">
                <path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1-1.1a5.5 5.5 0 0 0-7.8 7.8l1 1L12 21l7.8-7.6 1-1a5.5 5.5 0 0 0 0-7.8Z" stroke-linecap="round" stroke-linejoin="round" />
              </svg>
            </button>
          </div>

          <p v-if="inStock && stockLeft <= 5" class="mt-2 text-sm text-amber-600">Hurry — only {{ stockLeft }} in stock.</p>

          <!-- Description -->
          <div class="mt-8">
            <h2 class="text-lg font-semibold text-slate-900">Description</h2>
            <p class="mt-2 whitespace-pre-line leading-relaxed text-slate-600" data-testid="product-description">{{ product.description }}</p>
          </div>

          <!-- Meta -->
          <dl class="mt-6 grid grid-cols-2 gap-3 rounded-xl bg-slate-50 p-4 text-sm">
            <div><dt class="text-slate-500">Brand</dt><dd class="font-medium text-slate-900">{{ product.brand }}</dd></div>
            <div><dt class="text-slate-500">SKU</dt><dd class="font-medium text-slate-900">{{ product.sku }}</dd></div>
            <div><dt class="text-slate-500">Category</dt><dd class="font-medium text-slate-900">{{ product.categoryName }}</dd></div>
            <div><dt class="text-slate-500">Availability</dt><dd class="font-medium text-slate-900">{{ product.isAvailable ? 'In stock' : 'Unavailable' }}</dd></div>
          </dl>
        </div>
      </div>

      <!-- Related -->
      <section v-if="related.length > 0" class="mt-14">
        <h2 class="mb-4 text-xl font-bold text-slate-900">You may also like</h2>
        <div class="grid grid-cols-2 gap-4 md:grid-cols-4">
          <ProductCard v-for="item in related" :key="item.id" :product="item" />
        </div>
      </section>
    </template>
  </div>
</template>

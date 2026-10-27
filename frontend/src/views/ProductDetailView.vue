<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
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
import { useRecentlyViewed } from '@/composables/useRecentlyViewed'
import { ApiError } from '@/services/http'
import type { ProductDetail, ProductSummary } from '@/types'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const cart = useCartStore()
const wishlist = useWishlistStore()
const toasts = useToastStore()
const { record, recent } = useRecentlyViewed()

const product = ref<ProductDetail | null>(null)
const related = ref<ProductSummary[]>([])
const recentlyViewed = ref<ProductSummary[]>([])
const loading = ref(true)
const notFound = ref(false)
const quantity = ref(1)
const selectedVariantId = ref<number | null>(null)
const activeIndex = ref(0)
const adding = ref(false)
const zoomOpen = ref(false)

const selectedVariant = computed(
  () => product.value?.variants.find((v) => v.id === selectedVariantId.value) ?? null,
)

/** Unit price reflects the selected variant (server-derived). */
const unitPrice = computed(() => selectedVariant.value?.effectivePrice ?? product.value?.salePrice ?? 0)
const inStock = computed(() =>
  selectedVariant.value ? selectedVariant.value.stock > 0 : (product.value?.isAvailable ?? false),
)
const stockLeft = computed(() => selectedVariant.value?.stock ?? product.value?.stock ?? 0)

const gallery = computed(() => product.value?.images ?? [])
const activeImage = computed(() => gallery.value[activeIndex.value] ?? null)
const activeUrl = computed(() => activeImage.value?.url ?? product.value?.imageUrl ?? '')
const activeAlt = computed(() => activeImage.value?.altText ?? product.value?.name ?? '')

function setActive(index: number) {
  if (index < 0 || index >= gallery.value.length) return
  activeIndex.value = index
}

function nextImage() {
  if (gallery.value.length > 1) setActive((activeIndex.value + 1) % gallery.value.length)
}
function prevImage() {
  if (gallery.value.length > 1) setActive((activeIndex.value - 1 + gallery.value.length) % gallery.value.length)
}

function toggleZoom() {
  if (gallery.value.length > 0) zoomOpen.value = !zoomOpen.value
}
function closeZoom() {
  zoomOpen.value = false
}

// ---- swipe support (touch) ----
const touchStartX = ref(0)
const touchStartY = ref(0)
function onTouchStart(event: TouchEvent) {
  const t = event.changedTouches[0]
  touchStartX.value = t.clientX
  touchStartY.value = t.clientY
}
function onTouchEnd(event: TouchEvent) {
  const t = event.changedTouches[0]
  const dx = t.clientX - touchStartX.value
  const dy = t.clientY - touchStartY.value
  if (Math.abs(dx) > 40 && Math.abs(dx) > Math.abs(dy)) {
    if (dx < 0) nextImage()
    else prevImage()
  }
}

// ---- keyboard for the gallery/lightbox ----
function onKeydown(event: KeyboardEvent) {
  if (!zoomOpen.value) return
  if (event.key === 'Escape') closeZoom()
  if (event.key === 'ArrowRight') nextImage()
  if (event.key === 'ArrowLeft') prevImage()
}

async function load(slug: string) {
  loading.value = true
  notFound.value = false
  quantity.value = 1
  selectedVariantId.value = null
  activeIndex.value = 0
  zoomOpen.value = false
  try {
    product.value = await productService.bySlug(slug)
    // Record for the "Recently Viewed" section (homepage + this page).
    record(product.value)
    recentlyViewed.value = recent(4, product.value.slug)
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
  window.addEventListener('keydown', onKeydown)
})

onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))
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
        <!-- Gallery: main image + thumbnails -->
        <div>
          <div
            class="relative aspect-square overflow-hidden rounded-2xl border border-slate-200 bg-slate-100 qs-zoom"
            data-testid="gallery-main"
            @click="toggleZoom"
            @touchstart.passive="onTouchStart"
            @touchend.passive="onTouchEnd"
          >
            <ProductImage
              :key="activeUrl"
              :src="activeUrl"
              :alt="activeAlt"
              img-class="h-full w-full object-contain p-4 qs-slide-enter"
            />

            <!-- Prev / next arrows (visible on hover on desktop) -->
            <button
              v-if="gallery.length > 1"
              class="absolute left-2 top-1/2 -translate-y-1/2 rounded-full bg-white/90 p-2 text-slate-700 shadow transition hover:bg-white opacity-0 focus-visible:opacity-100 hover:opacity-100 sm:hover:opacity-100"
              aria-label="Previous image"
              data-testid="gallery-prev"
              @click.stop="prevImage"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" class="h-5 w-5"><path d="M15 5l-7 7 7 7" stroke-linecap="round" stroke-linejoin="round" /></svg>
            </button>
            <button
              v-if="gallery.length > 1"
              class="absolute right-2 top-1/2 -translate-y-1/2 rounded-full bg-white/90 p-2 text-slate-700 shadow transition hover:bg-white opacity-0 focus-visible:opacity-100 hover:opacity-100 sm:hover:opacity-100"
              aria-label="Next image"
              data-testid="gallery-next"
              @click.stop="nextImage"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" class="h-5 w-5"><path d="M9 5l7 7-7 7" stroke-linecap="round" stroke-linejoin="round" /></svg>
            </button>

            <!-- Image counter -->
            <span
              v-if="gallery.length > 1"
              class="absolute bottom-2 left-1/2 -translate-x-1/2 rounded-full bg-slate-900/70 px-2.5 py-1 text-xs font-medium text-white"
              data-testid="gallery-counter"
            >
              {{ activeIndex + 1 }} / {{ gallery.length }}
            </span>

            <!-- Zoom hint -->
            <span class="absolute right-2 top-2 rounded-md bg-white/90 px-2 py-1 text-[11px] font-medium text-slate-600 shadow-sm">
              Click to zoom
            </span>
          </div>

          <!-- Thumbnails -->
          <div v-if="gallery.length > 1" class="mt-3 flex gap-2 overflow-x-auto pb-1" data-testid="gallery-thumbnails">
            <button
              v-for="(image, i) in gallery"
              :key="image.id"
              class="h-20 w-20 shrink-0 overflow-hidden rounded-lg border-2 transition"
              :class="i === activeIndex ? 'border-brand-500 ring-1 ring-brand-200' : 'border-transparent hover:border-slate-300'"
              :aria-label="`View image ${i + 1}`"
              :aria-current="i === activeIndex"
              @click="setActive(i)"
            >
              <ProductImage :src="image.url" :alt="image.altText || `${product.name} view ${i + 1}`" img-class="h-full w-full object-contain bg-white p-1" />
            </button>
          </div>

          <!-- Zoom lightbox -->
          <div
            v-if="zoomOpen"
            class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/90 p-4"
            role="dialog"
            aria-modal="true"
            aria-label="Image viewer"
            data-testid="gallery-lightbox"
            @click.self="closeZoom"
          >
            <button class="absolute right-4 top-4 rounded-full bg-white/10 p-2 text-white hover:bg-white/20" aria-label="Close viewer" data-testid="lightbox-close" @click="closeZoom">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-6 w-6"><path d="M6 6l12 12M18 6L6 18" stroke-linecap="round" /></svg>
            </button>
            <button
              v-if="gallery.length > 1"
              class="absolute left-4 top-1/2 -translate-y-1/2 rounded-full bg-white/10 p-3 text-white hover:bg-white/20"
              aria-label="Previous image"
              @click.stop="prevImage"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" class="h-6 w-6"><path d="M15 5l-7 7 7 7" stroke-linecap="round" stroke-linejoin="round" /></svg>
            </button>
            <button
              v-if="gallery.length > 1"
              class="absolute right-4 top-1/2 -translate-y-1/2 rounded-full bg-white/10 p-3 text-white hover:bg-white/20"
              aria-label="Next image"
              @click.stop="nextImage"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" class="h-6 w-6"><path d="M9 5l7 7-7 7" stroke-linecap="round" stroke-linejoin="round" /></svg>
            </button>
            <img
              :src="activeUrl"
              :alt="activeAlt"
              class="max-h-[85vh] max-w-[92vw] rounded-lg object-contain"
              data-testid="lightbox-image"
            />
            <p class="absolute bottom-4 left-1/2 -translate-x-1/2 text-xs text-white/70">{{ activeIndex + 1 }} / {{ gallery.length }} · Use ← → to navigate, Esc to close</p>
          </div>
        </div>

        <!-- Info -->
        <div>
          <p class="text-sm font-medium uppercase tracking-wide text-brand-600">{{ product.categoryName }}</p>
          <h1 class="mt-1 text-3xl font-bold text-slate-900" data-testid="product-title">{{ product.name }}</h1>
          <p class="mt-1 text-sm text-slate-500">{{ product.brand }} · SKU {{ product.sku }}</p>

          <div class="mt-3 flex flex-wrap items-center gap-3">
            <StarRating :rating="product.rating" :count="product.ratingCount" />
            <span class="text-xs text-slate-400">Sample rating</span>
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
            <button class="btn-secondary px-6 py-3" :disabled="!inStock" data-testid="buy-now" @click="buyNow">Buy now</button>
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

          <!-- Delivery info -->
          <ul class="mt-6 grid gap-2 rounded-xl bg-slate-50 p-4 text-sm text-slate-600 sm:grid-cols-2" data-testid="delivery-info">
            <li class="flex items-center gap-2">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="h-4 w-4 shrink-0 text-brand-600"><path d="M3 7h11v8H3zM14 10h4l3 3v2h-7z" stroke-linecap="round" stroke-linejoin="round"/></svg>
              Free delivery over ₹999
            </li>
            <li class="flex items-center gap-2">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="h-4 w-4 shrink-0 text-brand-600"><path d="M3 12a9 9 0 1 0 3-6.7L3 8M3 3v5h5" stroke-linecap="round" stroke-linejoin="round"/></svg>
              7-day easy returns
            </li>
            <li class="flex items-center gap-2">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="h-4 w-4 shrink-0 text-brand-600"><path d="M12 2l8 4v6c0 5-3.5 8.5-8 10-4.5-1.5-8-5-8-10V6l8-4z" stroke-linecap="round" stroke-linejoin="round"/></svg>
              1-year brand warranty
            </li>
            <li class="flex items-center gap-2">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="h-4 w-4 shrink-0 text-brand-600"><path d="M9 12l2 2 4-4M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z" stroke-linecap="round" stroke-linejoin="round"/></svg>
              Secure payments
            </li>
          </ul>

          <!-- Description -->
          <div class="mt-8">
            <h2 class="text-lg font-semibold text-slate-900">Description</h2>
            <p class="mt-2 whitespace-pre-line leading-relaxed text-slate-600" data-testid="product-description">{{ product.description }}</p>
          </div>

          <!-- Specifications -->
          <dl class="mt-6 grid grid-cols-2 gap-3 rounded-xl bg-slate-50 p-4 text-sm">
            <div><dt class="text-slate-500">Brand</dt><dd class="font-medium text-slate-900">{{ product.brand }}</dd></div>
            <div><dt class="text-slate-500">SKU</dt><dd class="font-medium text-slate-900">{{ product.sku }}</dd></div>
            <div><dt class="text-slate-500">Category</dt><dd class="font-medium text-slate-900">{{ product.categoryName }}</dd></div>
            <div><dt class="text-slate-500">Availability</dt><dd class="font-medium text-slate-900">{{ product.isAvailable ? 'In stock' : 'Unavailable' }}</dd></div>
            <div><dt class="text-slate-500">Rating</dt><dd class="font-medium text-slate-900">{{ product.rating.toFixed(1) }} / 5 (sample data)</dd></div>
            <div><dt class="text-slate-500">Images</dt><dd class="font-medium text-slate-900">{{ gallery.length }} views</dd></div>
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

      <!-- Recently viewed -->
      <section v-if="recentlyViewed.length > 0" class="mt-12">
        <h2 class="mb-4 text-xl font-bold text-slate-900">Recently Viewed</h2>
        <div class="grid grid-cols-2 gap-4 md:grid-cols-4">
          <ProductCard v-for="item in recentlyViewed" :key="`rv-${item.id}`" :product="item" />
        </div>
      </section>
    </template>
  </div>
</template>

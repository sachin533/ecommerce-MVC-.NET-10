<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { productService, categoryService } from '@/services/api'
import PromoTicker from '@/components/PromoTicker.vue'
import HeroCarousel from '@/components/HeroCarousel.vue'
import CategoryStrip from '@/components/CategoryStrip.vue'
import PromoBanners from '@/components/PromoBanners.vue'
import ProductSection from '@/components/ProductSection.vue'
import ProductCard from '@/components/ProductCard.vue'
import { useRecentlyViewed } from '@/composables/useRecentlyViewed'
import type { Category, ProductSummary } from '@/types'

const { recent } = useRecentlyViewed()

const featured = ref<ProductSummary[]>([])
const bestSellers = ref<ProductSummary[]>([])
const deals = ref<ProductSummary[]>([])
const newest = ref<ProductSummary[]>([])
const recommended = ref<ProductSummary[]>([])
const categories = ref<Category[]>([])
const loading = ref(true)
const recentlyViewed = ref<ProductSummary[]>([])

/** Deals of the Day countdown resets at local midnight. */
const countdown = ref('--:--:--')
let countdownTimer: number | undefined

function tickCountdown() {
  const now = new Date()
  const end = new Date(now)
  end.setHours(24, 0, 0, 0)
  const diff = Math.max(0, end.getTime() - now.getTime())
  const hours = Math.floor(diff / 3_600_000)
  const minutes = Math.floor((diff % 3_600_000) / 60_000)
  const seconds = Math.floor((diff % 60_000) / 1000)
  countdown.value = [hours, minutes, seconds].map((n) => String(n).padStart(2, '0')).join(':')
}

const hasRecentlyViewed = computed(() => recentlyViewed.value.length > 0)

onMounted(async () => {
  tickCountdown()
  countdownTimer = window.setInterval(tickCountdown, 1000)

  try {
    const [featuredRes, dealsRes, bestRes, newestRes, recommendedRes, categoriesRes] = await Promise.all([
      productService.featured(),
      productService.list({ sort: 'discount', pageSize: 8 }),
      productService.list({ sort: 'popularity', pageSize: 8 }),
      productService.list({ sort: 'newest', pageSize: 8 }),
      productService.list({ sort: 'rating', pageSize: 8 }),
      categoryService.list(),
    ])
    featured.value = featuredRes
    deals.value = dealsRes.items
    bestSellers.value = bestRes.items
    newest.value = newestRes.items
    recommended.value = recommendedRes.items.filter((p) => p.isAvailable)
    categories.value = categoriesRes.filter((c) => c.isActive)
  } finally {
    loading.value = false
    recentlyViewed.value = recent(8)
  }
})

// Clean up the countdown interval on unmount.
onBeforeUnmount(() => window.clearInterval(countdownTimer))
</script>

<template>
  <div>
    <!-- Animated promotional ticker (black bar, white text) -->
    <PromoTicker />

    <!-- Hero carousel with real product photography -->
    <HeroCarousel />

    <div class="mx-auto max-w-7xl space-y-10 px-4 py-10 sm:px-6">
      <!-- Category strip -->
      <section aria-labelledby="categories-heading">
        <div class="mb-5 text-center">
          <h2 id="categories-heading" class="text-xl font-bold text-slate-900 sm:text-2xl" data-testid="categories-heading">
            Shop by Category
          </h2>
          <p class="mt-1 text-sm text-slate-500">Everything from flagship phones to home essentials</p>
        </div>
        <CategoryStrip :categories="categories" />
      </section>

      <!-- Trust badges -->
      <section class="grid grid-cols-2 gap-3 rounded-2xl border border-slate-200 bg-white p-4 text-center sm:grid-cols-4" aria-label="Why shop with us">
        <div class="flex flex-col items-center gap-1 p-2">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="h-6 w-6 text-brand-600"><path d="M3 7h11v8H3zM14 10h4l3 3v2h-7zM6.5 18a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3zM17.5 18a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3z" stroke-linecap="round" stroke-linejoin="round"/></svg>
          <p class="text-xs font-semibold text-slate-800">Free delivery</p>
          <p class="text-[11px] text-slate-500">On orders over ₹999</p>
        </div>
        <div class="flex flex-col items-center gap-1 p-2">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="h-6 w-6 text-brand-600"><path d="M3 12a9 9 0 1 0 3-6.7L3 8M3 3v5h5" stroke-linecap="round" stroke-linejoin="round"/></svg>
          <p class="text-xs font-semibold text-slate-800">7-day returns</p>
          <p class="text-[11px] text-slate-500">No-questions policy</p>
        </div>
        <div class="flex flex-col items-center gap-1 p-2">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="h-6 w-6 text-brand-600"><path d="M12 2l8 4v6c0 5-3.5 8.5-8 10-4.5-1.5-8-5-8-10V6l8-4zM9 12l2 2 4-4" stroke-linecap="round" stroke-linejoin="round"/></svg>
          <p class="text-xs font-semibold text-slate-800">Secure checkout</p>
          <p class="text-[11px] text-slate-500">Encrypted payments</p>
        </div>
        <div class="flex flex-col items-center gap-1 p-2">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="h-6 w-6 text-brand-600"><path d="M12 8v4l3 2M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z" stroke-linecap="round" stroke-linejoin="round"/></svg>
          <p class="text-xs font-semibold text-slate-800">Support 24/7</p>
          <p class="text-[11px] text-slate-500">Real humans, real help</p>
        </div>
      </section>

      <!-- Promo banners -->
      <PromoBanners />

      <!-- Deals of the Day with countdown -->
      <section data-testid="deals-of-the-day">
        <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="text-xl font-bold text-slate-900 sm:text-2xl" data-testid="section-title">Deals of the Day</h2>
            <p class="mt-0.5 text-sm text-slate-500">Discounts refreshed every 24 hours</p>
          </div>
          <div class="flex items-center gap-3">
            <span class="flex items-center gap-2 rounded-lg bg-slate-900 px-3 py-1.5 text-sm font-bold text-white tabular-nums" aria-label="Time left in this sale" data-testid="deals-countdown">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-4 w-4 text-amber-300"><path d="M12 8v4l3 2M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z" stroke-linecap="round" stroke-linejoin="round"/></svg>
              {{ countdown }}
            </span>
            <router-link :to="{ name: 'deals' }" class="text-sm font-semibold text-brand-600 hover:text-brand-700 hover:underline" data-testid="section-view-all">
              View all →
            </router-link>
          </div>
        </div>

        <div v-if="loading" class="grid grid-cols-2 gap-4 md:grid-cols-4">
          <div v-for="i in 4" :key="i" class="card animate-pulse p-4">
            <div class="aspect-square animate-pulse rounded-lg bg-slate-200" />
            <div class="mt-3 h-4 w-3/4 rounded bg-slate-200" />
          </div>
        </div>
        <div v-else class="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-4">
          <ProductCard v-for="product in deals" :key="product.id" :product="product" />
        </div>
      </section>

      <!-- Featured -->
      <ProductSection
        title="Featured Products"
        subtitle="Hand-picked by the QuickShop team"
        section-id="featured-products"
        :products="featured"
        :loading="loading"
        :view-all="{ name: 'shop', query: { featured: 'true' } }"
      />

      <!-- Best sellers -->
      <ProductSection
        title="Best Sellers"
        subtitle="What everyone is buying right now"
        section-id="best-sellers"
        :products="bestSellers"
        :loading="loading"
        :view-all="{ name: 'shop', query: { sort: 'popularity' } }"
      />

      <!-- New arrivals -->
      <ProductSection
        title="New Arrivals"
        subtitle="Fresh stock, just landed"
        section-id="new-arrivals"
        :products="newest"
        :loading="loading"
        :view-all="{ name: 'new-arrivals' }"
      />

      <!-- Recommended -->
      <ProductSection
        title="Recommended for You"
        subtitle="Top-rated picks across the store"
        section-id="recommended-products"
        :products="recommended"
        :loading="loading"
        :view-all="{ name: 'shop', query: { sort: 'rating' } }"
      />

      <!-- Recently viewed -->
      <ProductSection
        v-if="hasRecentlyViewed"
        title="Recently Viewed"
        subtitle="Pick up where you left off"
        section-id="recently-viewed"
        :products="recentlyViewed"
      />
    </div>
  </div>
</template>

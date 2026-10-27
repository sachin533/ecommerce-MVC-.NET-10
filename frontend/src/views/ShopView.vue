<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { productService } from '@/services/api'
import ProductCard from '@/components/ProductCard.vue'
import PaginationBar from '@/components/PaginationBar.vue'
import type { PagedResult, ProductFilters, ProductSummary } from '@/types'

const route = useRoute()
const router = useRouter()

const results = ref<PagedResult<ProductSummary> | null>(null)
const filters = ref<ProductFilters | null>(null)
const loading = ref(true)
const error = ref('')

// Filter state (initialized from the URL, pushed back on change).
const state = reactive({
  search: (route.query.search as string) ?? '',
  categorySlug: (route.params.slug as string) ?? (route.query.categorySlug as string) ?? '',
  brand: (route.query.brand as string) ?? '',
  minPrice: (route.query.minPrice as string) ?? '',
  maxPrice: (route.query.maxPrice as string) ?? '',
  inStockOnly: route.query.inStockOnly === 'true',
  featured: route.query.featured === 'true',
  sort: (route.query.sort as string) ?? 'newest',
  page: Number(route.query.page ?? 1),
})

const brands = computed(() => filters.value?.brands ?? [])
const categories = computed(() => filters.value?.categories ?? [])
const priceBounds = computed(() => filters.value?.priceRange ?? { min: 0, max: 0 })

const activeCategoryName = computed(
  () => categories.value.find((c) => c.slug === state.categorySlug)?.name ?? '',
)

const sortOptions = [
  { value: 'newest', label: 'Newest first' },
  { value: 'price-asc', label: 'Price: low to high' },
  { value: 'price-desc', label: 'Price: high to low' },
  { value: 'rating', label: 'Customer rating' },
  { value: 'popularity', label: 'Most popular' },
]

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [products, filterData] = await Promise.all([
      productService.list({
        search: state.search || undefined,
        categorySlug: state.categorySlug || undefined,
        brand: state.brand || undefined,
        minPrice: state.minPrice ? Number(state.minPrice) : undefined,
        maxPrice: state.maxPrice ? Number(state.maxPrice) : undefined,
        inStockOnly: state.inStockOnly || undefined,
        featured: state.featured || undefined,
        sort: state.sort as never,
        page: state.page,
        pageSize: 12,
      }),
      productService.filters(state.categorySlug || undefined),
    ])
    results.value = products
    filters.value = filterData
  } catch (err) {
    error.value = 'Could not load products. Please try again.'
    console.error(err)
  } finally {
    loading.value = false
  }
}

/** Mirror the state into the URL so filters are shareable. */
function syncUrl() {
  const query: Record<string, string> = {}
  if (state.search) query.search = state.search
  if (state.categorySlug) query.categorySlug = state.categorySlug
  if (state.brand) query.brand = state.brand
  if (state.minPrice) query.minPrice = state.minPrice
  if (state.maxPrice) query.maxPrice = state.maxPrice
  if (state.inStockOnly) query.inStockOnly = 'true'
  if (state.featured) query.featured = 'true'
  if (state.sort !== 'newest') query.sort = state.sort
  if (state.page > 1) query.page = String(state.page)
  router.replace({ query, params: route.params })
}

function apply() {
  state.page = 1
  syncUrl()
  load()
}

function changePage(page: number) {
  state.page = page
  syncUrl()
  load()
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

function clearFilters() {
  state.search = ''
  state.categorySlug = ''
  state.brand = ''
  state.minPrice = ''
  state.maxPrice = ''
  state.inStockOnly = false
  state.featured = false
  state.sort = 'newest'
  apply()
}

function selectCategory(slug: string) {
  state.categorySlug = slug
  apply()
}

// Keep in sync when the route changes (e.g. header search or category nav).
watch(
  () => [route.params.slug, route.query.search, route.query.categorySlug, route.query.sort, route.query.featured],
  () => {
    state.categorySlug = (route.params.slug as string) ?? (route.query.categorySlug as string) ?? ''
    state.search = (route.query.search as string) ?? ''
    state.sort = (route.query.sort as string) ?? 'newest'
    state.featured = route.query.featured === 'true'
    state.page = Number(route.query.page ?? 1)
    load()
  },
)

onMounted(load)
</script>

<template>
  <div class="mx-auto max-w-7xl px-4 py-8 sm:px-6">
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-slate-900" data-testid="shop-title">
        {{ activeCategoryName || 'All products' }}
      </h1>
      <p class="text-sm text-slate-500">
        {{ results ? `${results.totalCount} products` : 'Loading…' }}
      </p>
    </div>

    <div class="grid gap-6 lg:grid-cols-[260px_1fr]">
      <!-- Filter sidebar -->
      <aside class="space-y-5 self-start lg:sticky lg:top-20" data-testid="filter-sidebar">
        <div class="card p-4">
          <div class="mb-3 flex items-center justify-between">
            <h2 class="font-semibold text-slate-900">Filters</h2>
            <button class="text-xs font-semibold text-brand-600 hover:underline" data-testid="clear-filters" @click="clearFilters">
              Clear all
            </button>
          </div>

          <!-- Search -->
          <form class="mb-4" @submit.prevent="apply">
            <input v-model="state.search" type="search" placeholder="Search…" aria-label="Search products" class="input" data-testid="filter-search" />
          </form>

          <!-- Categories -->
          <div class="mb-4">
            <h3 class="mb-2 text-sm font-semibold text-slate-700">Category</h3>
            <ul class="max-h-56 space-y-1 overflow-y-auto pr-1" data-testid="category-list">
              <li>
                <button
                  class="flex w-full items-center justify-between rounded-md px-2 py-1.5 text-left text-sm"
                  :class="state.categorySlug === '' ? 'bg-brand-50 font-semibold text-brand-700' : 'text-slate-600 hover:bg-slate-50'"
                  @click="selectCategory('')"
                >
                  <span>All</span>
                  <span class="text-xs text-slate-400">{{ filters?.totalProducts }}</span>
                </button>
              </li>
              <li v-for="category in categories" :key="category.id">
                <button
                  class="flex w-full items-center justify-between rounded-md px-2 py-1.5 text-left text-sm"
                  :class="state.categorySlug === category.slug ? 'bg-brand-50 font-semibold text-brand-700' : 'text-slate-600 hover:bg-slate-50'"
                  :data-testid="`category-${category.slug}`"
                  @click="selectCategory(category.slug)"
                >
                  <span>{{ category.name }}</span>
                  <span class="text-xs text-slate-400">{{ category.productCount }}</span>
                </button>
              </li>
            </ul>
          </div>

          <!-- Brand -->
          <div class="mb-4">
            <h3 class="mb-2 text-sm font-semibold text-slate-700">Brand</h3>
            <select v-model="state.brand" class="input" data-testid="brand-filter" @change="apply">
              <option value="">All brands</option>
              <option v-for="brand in brands" :key="brand" :value="brand">{{ brand }}</option>
            </select>
          </div>

          <!-- Price -->
          <div class="mb-4">
            <h3 class="mb-2 text-sm font-semibold text-slate-700">Price (₹)</h3>
            <div class="flex items-center gap-2">
              <input v-model="state.minPrice" type="number" min="0" placeholder="Min" aria-label="Minimum price" class="input" data-testid="min-price" />
              <span class="text-slate-400">–</span>
              <input v-model="state.maxPrice" type="number" min="0" placeholder="Max" aria-label="Maximum price" class="input" data-testid="max-price" />
            </div>
            <p class="mt-1 text-xs text-slate-400">
              Range ₹{{ Math.round(priceBounds.min) }} – ₹{{ Math.round(priceBounds.max) }}
            </p>
            <button class="btn-secondary mt-2 w-full" @click="apply">Apply price</button>
          </div>

          <!-- Availability -->
          <label class="flex cursor-pointer items-center gap-2 text-sm text-slate-700">
            <input v-model="state.inStockOnly" type="checkbox" class="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500" data-testid="in-stock-filter" @change="apply" />
            In stock only
          </label>
        </div>
      </aside>

      <!-- Results -->
      <section>
        <!-- Active chips -->
        <div v-if="state.brand || state.minPrice || state.maxPrice || state.inStockOnly || state.featured || state.search" class="mb-4 flex flex-wrap gap-2">
          <span v-if="state.search" class="badge bg-slate-100 text-slate-700">“{{ state.search }}”</span>
          <span v-if="state.brand" class="badge bg-slate-100 text-slate-700">{{ state.brand }}</span>
          <span v-if="state.minPrice || state.maxPrice" class="badge bg-slate-100 text-slate-700">
            ₹{{ state.minPrice || 0 }} – ₹{{ state.maxPrice || '∞' }}
          </span>
          <span v-if="state.inStockOnly" class="badge bg-slate-100 text-slate-700">In stock</span>
          <span v-if="state.featured" class="badge bg-amber-100 text-amber-800">Featured only</span>
        </div>

        <!-- Sort bar -->
        <div class="mb-4 flex items-center justify-between gap-3">
          <p class="text-sm text-slate-500" data-testid="result-count">
            <template v-if="results">{{ (results.page - 1) * results.pageSize + 1 }}–{{ Math.min(results.page * results.pageSize, results.totalCount) }} of {{ results.totalCount }}</template>
          </p>
          <label class="flex items-center gap-2 text-sm text-slate-600">
            Sort by
            <select v-model="state.sort" class="input w-auto" data-testid="sort-select" @change="apply">
              <option v-for="option in sortOptions" :key="option.value" :value="option.value">{{ option.label }}</option>
            </select>
          </label>
        </div>

        <div v-if="loading" class="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-4">
          <div v-for="i in 8" :key="i" class="h-96 animate-pulse rounded-xl bg-slate-200" />
        </div>

        <p v-else-if="error" class="rounded-xl bg-rose-50 p-6 text-center text-rose-700">{{ error }}</p>

        <div v-else-if="results && results.items.length === 0" class="rounded-xl border border-dashed border-slate-300 p-12 text-center" data-testid="empty-state">
          <p class="text-lg font-semibold text-slate-700">No products match your filters</p>
          <p class="mt-1 text-sm text-slate-500">Try removing some filters or search for something else.</p>
          <button class="btn-primary mt-4" @click="clearFilters">Clear filters</button>
        </div>

        <template v-else-if="results">
          <div class="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-4">
            <ProductCard v-for="product in results.items" :key="product.id" :product="product" />
          </div>
          <PaginationBar
            :page="results.page"
            :total-pages="results.totalPages"
            :total-count="results.totalCount"
            @change="changePage"
          />
        </template>
      </section>
    </div>
  </div>
</template>

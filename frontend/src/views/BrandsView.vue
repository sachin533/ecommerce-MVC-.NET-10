<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { productService } from '@/services/api'
import type { ProductFilters } from '@/types'

const router = useRouter()
const filters = ref<ProductFilters | null>(null)
const loading = ref(true)
const query = ref('')

onMounted(async () => {
  try {
    filters.value = await productService.filters()
  } finally {
    loading.value = false
  }
})

const brands = computed(() => {
  const all = filters.value?.brands ?? []
  const q = query.value.trim().toLowerCase()
  return q ? all.filter((b) => b.toLowerCase().includes(q)) : all
})

function openBrand(brand: string) {
  router.push({ name: 'shop', query: { brand } })
}
</script>

<template>
  <div class="mx-auto max-w-7xl px-4 py-8 sm:px-6">
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-slate-900" data-testid="brands-title">Shop by brand</h1>
      <p class="mt-1 text-sm text-slate-500">Pick a brand to see everything we carry.</p>
    </div>

    <input
      v-model="query"
      type="search"
      placeholder="Filter brands…"
      aria-label="Filter brands"
      class="input mb-6 max-w-sm"
      data-testid="brand-search"
    />

    <div v-if="loading" class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
      <div v-for="i in 8" :key="i" class="h-28 animate-pulse rounded-xl bg-slate-200" />
    </div>

    <div v-else-if="brands.length === 0" class="rounded-xl border border-dashed border-slate-300 p-12 text-center text-slate-500" data-testid="brands-empty">
      No brands match “{{ query }}”.
    </div>

    <div v-else class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4" data-testid="brand-grid">
      <button
        v-for="brand in brands"
        :key="brand"
        class="card flex h-28 flex-col items-center justify-center gap-1 p-4 text-center transition hover:-translate-y-0.5 hover:border-brand-300 hover:shadow-md"
        :data-testid="`brand-${brand.toLowerCase().replace(/[^a-z0-9]+/g, '-')}`"
        @click="openBrand(brand)"
      >
        <span class="text-lg font-bold text-slate-900">{{ brand }}</span>
        <span class="text-xs text-slate-400">View products →</span>
      </button>
    </div>
  </div>
</template>

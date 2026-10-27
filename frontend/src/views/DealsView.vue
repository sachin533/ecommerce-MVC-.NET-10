<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { productService } from '@/services/api'
import ProductCard from '@/components/ProductCard.vue'
import PaginationBar from '@/components/PaginationBar.vue'
import type { PagedResult, ProductSummary } from '@/types'

/** Deals page: products with a live discount, biggest first. */
const results = ref<PagedResult<ProductSummary> | null>(null)
const page = ref(1)
const loading = ref(true)

async function load() {
  loading.value = true
  try {
    results.value = await productService.list({ sort: 'discount', page: page.value, pageSize: 12 })
  } finally {
    loading.value = false
  }
}

function changePage(next: number) {
  page.value = next
  load()
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

onMounted(load)
</script>

<template>
  <div class="mx-auto max-w-7xl px-4 py-8 sm:px-6">
    <div class="mb-6 rounded-2xl bg-gradient-to-r from-rose-600 to-amber-500 p-6 text-white sm:p-8">
      <h1 class="text-2xl font-extrabold sm:text-3xl" data-testid="deals-title">Deals of the Day</h1>
      <p class="mt-1 text-sm text-white/85 sm:text-base">
        Hand-picked discounts across the store — updated daily, while stocks last.
      </p>
    </div>

    <p v-if="results && !loading" class="mb-4 text-sm text-slate-500" data-testid="deals-count">{{ results.totalCount }} deals</p>

    <div v-if="loading" class="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-4">
      <div v-for="i in 8" :key="i" class="h-96 animate-pulse rounded-xl bg-slate-200" />
    </div>

    <template v-else-if="results">
      <div class="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-4">
        <ProductCard v-for="product in results.items" :key="product.id" :product="product" />
      </div>
      <PaginationBar :page="results.page" :total-pages="results.totalPages" :total-count="results.totalCount" @change="changePage" />
    </template>
  </div>
</template>

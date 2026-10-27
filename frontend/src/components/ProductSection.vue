<script setup lang="ts">
import ProductCard from '@/components/ProductCard.vue'
import type { ProductSummary } from '@/types'

/** Titled product row used by every homepage section. */
defineProps<{
  title: string
  subtitle?: string
  products: ProductSummary[]
  loading?: boolean
  /** Optional "View all" link target. */
  viewAll?: Record<string, unknown>
  /** Test hook for e2e (`deals-of-the-day`, etc.). */
  sectionId?: string
}>()
</script>

<template>
  <section class="mt-12" :data-testid="sectionId">
    <div class="mb-4 flex items-end justify-between gap-4">
      <div>
        <h2 class="text-xl font-bold text-slate-900 sm:text-2xl" data-testid="section-title">{{ title }}</h2>
        <p v-if="subtitle" class="mt-0.5 text-sm text-slate-500">{{ subtitle }}</p>
      </div>
      <router-link
        v-if="viewAll"
        :to="viewAll"
        class="shrink-0 text-sm font-semibold text-brand-600 hover:text-brand-700 hover:underline"
        data-testid="section-view-all"
      >
        View all →
      </router-link>
    </div>

    <div v-if="loading" class="grid grid-cols-2 gap-4 lg:grid-cols-4" data-testid="section-loading">
      <div v-for="i in 4" :key="i" class="card animate-pulse p-4">
        <div class="aspect-square animate-pulse rounded-lg bg-slate-200" />
        <div class="mt-3 h-4 w-3/4 rounded bg-slate-200" />
        <div class="mt-2 h-4 w-1/2 rounded bg-slate-200" />
      </div>
    </div>

    <div v-else-if="products.length === 0" class="rounded-xl border border-dashed border-slate-300 p-10 text-center text-sm text-slate-500">
      Nothing to show here yet.
    </div>

    <div v-else class="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-4">
      <ProductCard v-for="product in products" :key="product.id" :product="product" />
    </div>
  </section>
</template>

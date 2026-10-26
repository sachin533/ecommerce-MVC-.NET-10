<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { adminService } from '@/services/api'
import ProductImage from '@/components/ProductImage.vue'
import PriceDisplay from '@/components/PriceDisplay.vue'
import PaginationBar from '@/components/PaginationBar.vue'
import { useToastStore } from '@/stores/toast'
import { ApiError } from '@/services/http'
import type { PagedResult, ProductSummary } from '@/types'

const toasts = useToastStore()

const results = ref<PagedResult<ProductSummary> | null>(null)
const loading = ref(true)
const search = ref('')
const page = ref(1)
const stockEdits = ref<Record<number, number>>({})
const busyId = ref<number | null>(null)

async function load() {
  loading.value = true
  try {
    results.value = await adminService.products({ search: search.value || undefined, page: page.value, pageSize: 15 })
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not load products')
  } finally {
    loading.value = false
  }
}

async function saveStock(product: ProductSummary) {
  const stock = stockEdits.value[product.id]
  if (stock == null || stock < 0 || stock === product.stock) return
  busyId.value = product.id
  try {
    await adminService.setStock(product.id, stock)
    toasts.success(`Stock updated for ${product.name}`)
    await load()
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not update stock')
  } finally {
    busyId.value = null
  }
}

async function remove(product: ProductSummary) {
  if (!confirm(`Delete “${product.name}”? This cannot be undone.`)) return
  busyId.value = product.id
  try {
    await adminService.deleteProduct(product.id)
    toasts.success('Product deleted')
    await load()
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not delete product')
  } finally {
    busyId.value = null
  }
}

watch(page, load)
onMounted(load)
</script>

<template>
  <div>
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-2xl font-bold text-slate-900" data-testid="admin-products-title">Products</h1>
        <p class="text-sm text-slate-500">{{ results?.totalCount ?? '…' }} products</p>
      </div>
      <router-link :to="{ name: 'admin-product-new' }" class="btn-primary" data-testid="new-product">+ Add product</router-link>
    </div>

    <div class="card mb-4 p-3">
      <input v-model="search" type="search" placeholder="Search name, brand or SKU…" class="input max-w-md" data-testid="admin-product-search" @input="page = 1; load()" />
    </div>

    <div v-if="loading" class="space-y-2">
      <div v-for="i in 6" :key="i" class="h-16 animate-pulse rounded-lg bg-slate-200" />
    </div>

    <div v-else-if="results" class="card overflow-x-auto" data-testid="admin-product-table">
      <table class="w-full min-w-[820px] text-left text-sm">
        <thead class="border-b border-slate-200 bg-slate-50 text-xs uppercase text-slate-500">
          <tr>
            <th class="px-4 py-3">Product</th>
            <th class="px-4 py-3">SKU</th>
            <th class="px-4 py-3">Price</th>
            <th class="px-4 py-3">Stock</th>
            <th class="px-4 py-3">Status</th>
            <th class="px-4 py-3 text-right">Actions</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100">
          <tr v-for="product in results.items" :key="product.id" data-testid="admin-product-row">
            <td class="px-4 py-3">
              <div class="flex items-center gap-3">
                <div class="h-10 w-10 shrink-0 overflow-hidden rounded-md bg-slate-100">
                  <ProductImage :src="product.imageUrl" :alt="product.name" />
                </div>
                <div class="min-w-0">
                  <p class="truncate font-medium text-slate-900">{{ product.name }}</p>
                  <p class="text-xs text-slate-500">{{ product.brand }} · {{ product.categoryName }}</p>
                </div>
              </div>
            </td>
            <td class="px-4 py-3 font-mono text-xs text-slate-500">{{ product.sku }}</td>
            <td class="px-4 py-3"><PriceDisplay :price="product.salePrice" :mrp="product.price" :discount-percent="product.discountPercent" size="sm" /></td>
            <td class="px-4 py-3">
              <div class="flex items-center gap-2">
                <input
                  v-model.number="stockEdits[product.id]"
                  type="number"
                  min="0"
                  :placeholder="String(product.stock)"
                  class="input w-20 py-1 text-sm"
                  :data-testid="`stock-input-${product.id}`"
                />
                <button
                  v-if="stockEdits[product.id] != null && stockEdits[product.id] !== product.stock"
                  class="text-xs font-semibold text-brand-600 hover:underline"
                  :disabled="busyId === product.id"
                  :data-testid="`save-stock-${product.id}`"
                  @click="saveStock(product)"
                >
                  Save
                </button>
                <span v-else class="text-xs" :class="product.stock === 0 ? 'text-rose-600' : product.stock <= 10 ? 'text-amber-600' : 'text-slate-500'">
                  {{ product.stock === 0 ? 'Out' : product.stock }}
                </span>
              </div>
            </td>
            <td class="px-4 py-3">
              <span class="badge" :class="product.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'">
                {{ product.isActive ? 'Active' : 'Hidden' }}
              </span>
              <span v-if="product.isFeatured" class="badge ml-1 bg-brand-50 text-brand-700">Featured</span>
            </td>
            <td class="px-4 py-3 text-right">
              <div class="flex justify-end gap-2">
                <router-link :to="{ name: 'admin-product-edit', params: { id: product.id } }" class="text-xs font-semibold text-brand-600 hover:underline" :data-testid="`edit-${product.id}`">
                  Edit
                </router-link>
                <button class="text-xs font-semibold text-rose-600 hover:underline" :disabled="busyId === product.id" :data-testid="`delete-${product.id}`" @click="remove(product)">
                  Delete
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <PaginationBar
      v-if="results"
      :page="results.page"
      :total-pages="results.totalPages"
      :total-count="results.totalCount"
      @change="page = $event"
    />
  </div>
</template>

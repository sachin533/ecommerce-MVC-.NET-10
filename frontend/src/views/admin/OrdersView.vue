<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { adminService } from '@/services/api'
import OrderStatusBadge from '@/components/OrderStatusBadge.vue'
import PaginationBar from '@/components/PaginationBar.vue'
import { useToastStore } from '@/stores/toast'
import { ApiError } from '@/services/http'
import type { OrderSummary, PagedResult } from '@/types'

const toasts = useToastStore()

const results = ref<PagedResult<OrderSummary> | null>(null)
const loading = ref(true)
const search = ref('')
const status = ref('')
const page = ref(1)

const statuses = ['Pending', 'Paid', 'Processing', 'Shipped', 'Delivered', 'Cancelled']

async function load() {
  loading.value = true
  try {
    results.value = await adminService.orders({
      search: search.value || undefined,
      status: status.value || undefined,
      page: page.value,
      pageSize: 15,
    })
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not load orders')
  } finally {
    loading.value = false
  }
}

watch([page, status], () => {
  if (page.value !== 1 && status.value !== undefined) page.value = 1
  load()
})
onMounted(load)
</script>

<template>
  <div>
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-slate-900" data-testid="admin-orders-title">Orders</h1>
      <p class="text-sm text-slate-500">{{ results?.totalCount ?? '…' }} orders</p>
    </div>

    <div class="card mb-4 flex flex-wrap items-center gap-3 p-3">
      <input v-model="search" type="search" placeholder="Search order #, email or name…" class="input max-w-xs" data-testid="order-search" @input="page = 1; load()" />
      <select v-model="status" class="input w-auto" data-testid="status-filter" @change="page = 1">
        <option value="">All statuses</option>
        <option v-for="s in statuses" :key="s" :value="s">{{ s }}</option>
      </select>
    </div>

    <div v-if="loading" class="space-y-2">
      <div v-for="i in 6" :key="i" class="h-16 animate-pulse rounded-lg bg-slate-200" />
    </div>

    <div v-else-if="results && results.items.length === 0" class="rounded-xl border border-dashed border-slate-300 p-12 text-center">
      <p class="font-semibold text-slate-700">No orders match</p>
    </div>

    <div v-else-if="results" class="card overflow-x-auto" data-testid="admin-order-table">
      <table class="w-full min-w-[720px] text-left text-sm">
        <thead class="border-b border-slate-200 bg-slate-50 text-xs uppercase text-slate-500">
          <tr>
            <th class="px-4 py-3">Order</th>
            <th class="px-4 py-3">Customer</th>
            <th class="px-4 py-3">Date</th>
            <th class="px-4 py-3">Items</th>
            <th class="px-4 py-3">Total</th>
            <th class="px-4 py-3">Status</th>
            <th class="px-4 py-3"></th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100">
          <tr v-for="order in results.items" :key="order.id" data-testid="admin-order-row">
            <td class="px-4 py-3 font-medium text-slate-900">{{ order.orderNumber }}</td>
            <td class="px-4 py-3">
              <p class="text-slate-900">{{ order.customerName }}</p>
              <p class="text-xs text-slate-500">{{ order.customerEmail }}</p>
            </td>
            <td class="px-4 py-3 text-slate-600">{{ new Date(order.placedAt).toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' }) }}</td>
            <td class="px-4 py-3 text-slate-600">{{ order.itemCount }}</td>
            <td class="px-4 py-3 font-medium text-slate-900">₹{{ order.total.toLocaleString('en-IN') }}</td>
            <td class="px-4 py-3"><OrderStatusBadge :status="order.status" /></td>
            <td class="px-4 py-3 text-right">
              <router-link :to="{ name: 'admin-order-detail', params: { id: order.id } }" class="text-xs font-semibold text-brand-600 hover:underline" :data-testid="`view-order-${order.id}`">
                View →
              </router-link>
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

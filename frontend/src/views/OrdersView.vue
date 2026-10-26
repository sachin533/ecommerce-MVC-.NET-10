<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { orderService } from '@/services/api'
import PaginationBar from '@/components/PaginationBar.vue'
import ProductImage from '@/components/ProductImage.vue'
import OrderStatusBadge from '@/components/OrderStatusBadge.vue'
import type { OrderSummary, PagedResult } from '@/types'

const orders = ref<PagedResult<OrderSummary> | null>(null)
const loading = ref(true)

async function load(page = 1) {
  loading.value = true
  try {
    orders.value = await orderService.listMine(page, 10)
  } finally {
    loading.value = false
  }
}

onMounted(() => load())
</script>

<template>
  <div class="mx-auto max-w-4xl px-4 py-8 sm:px-6">
    <h1 class="mb-6 text-2xl font-bold text-slate-900" data-testid="orders-title">My orders</h1>

    <div v-if="loading" class="space-y-3">
      <div v-for="i in 3" :key="i" class="h-28 animate-pulse rounded-xl bg-slate-200" />
    </div>

    <div v-else-if="!orders || orders.items.length === 0" class="rounded-xl border border-dashed border-slate-300 p-16 text-center" data-testid="empty-orders">
      <p class="text-lg font-semibold text-slate-700">No orders yet</p>
      <p class="mt-1 text-sm text-slate-500">When you place an order it will show up here.</p>
      <router-link :to="{ name: 'shop' }" class="btn-primary mt-4">Start shopping</router-link>
    </div>

    <template v-else>
      <ul class="space-y-4" data-testid="order-list">
        <li v-for="order in orders.items" :key="order.id" class="card p-4 transition hover:shadow-md" data-testid="order-row">
          <router-link :to="{ name: 'order-detail', params: { id: order.id } }" class="flex items-center gap-4">
            <div class="h-16 w-16 shrink-0 overflow-hidden rounded-lg bg-slate-100">
              <ProductImage :src="order.thumbnailUrl ?? ''" :alt="order.orderNumber" />
            </div>
            <div class="min-w-0 flex-1">
              <div class="flex flex-wrap items-center gap-2">
                <span class="font-semibold text-slate-900" data-testid="order-number">{{ order.orderNumber }}</span>
                <OrderStatusBadge :status="order.status" />
              </div>
              <p class="mt-0.5 text-sm text-slate-500">
                {{ order.itemCount }} item{{ order.itemCount === 1 ? '' : 's' }} ·
                {{ new Date(order.placedAt).toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' }) }}
              </p>
            </div>
            <div class="text-right">
              <p class="font-bold text-slate-900" data-testid="order-total">₹{{ order.total.toLocaleString('en-IN') }}</p>
              <p class="text-xs font-semibold text-brand-600">View details →</p>
            </div>
          </router-link>
        </li>
      </ul>

      <PaginationBar
        :page="orders.page"
        :total-pages="orders.totalPages"
        :total-count="orders.totalCount"
        @change="load"
      />
    </template>
  </div>
</template>

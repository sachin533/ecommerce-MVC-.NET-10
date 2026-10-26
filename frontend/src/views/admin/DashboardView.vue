<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { adminService } from '@/services/api'
import OrderStatusBadge from '@/components/OrderStatusBadge.vue'
import type { DashboardStats } from '@/types'

const stats = ref<DashboardStats | null>(null)
const loading = ref(true)
const error = ref('')

const fmt = (value: number) => `₹${Math.round(value).toLocaleString('en-IN')}`

const maxDailyRevenue = computed(() =>
  Math.max(1, ...(stats.value?.revenueLast7Days.map((d) => d.revenue) ?? [1])),
)

onMounted(async () => {
  try {
    stats.value = await adminService.dashboard()
  } catch (err) {
    error.value = 'Could not load dashboard stats.'
    console.error(err)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div>
    <div class="mb-6 flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-slate-900" data-testid="dashboard-title">Dashboard</h1>
        <p class="text-sm text-slate-500">Store performance at a glance.</p>
      </div>
    </div>

    <div v-if="loading" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <div v-for="i in 6" :key="i" class="h-28 animate-pulse rounded-xl bg-slate-200" />
    </div>

    <p v-else-if="error" class="rounded-xl bg-rose-50 p-6 text-rose-700">{{ error }}</p>

    <template v-else-if="stats">
      <!-- KPI cards -->
      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3" data-testid="kpi-grid">
        <div class="card p-5">
          <p class="text-sm text-slate-500">Total revenue</p>
          <p class="mt-1 text-2xl font-bold text-slate-900" data-testid="kpi-revenue">{{ fmt(stats.totalRevenue) }}</p>
          <p class="text-xs text-slate-400">Excludes cancelled orders</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-slate-500">Orders</p>
          <p class="mt-1 text-2xl font-bold text-slate-900" data-testid="kpi-orders">{{ stats.totalOrders }}</p>
          <p class="text-xs text-slate-400">{{ stats.pendingOrders }} awaiting action</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-slate-500">Customers</p>
          <p class="mt-1 text-2xl font-bold text-slate-900" data-testid="kpi-customers">{{ stats.totalCustomers }}</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-slate-500">Products</p>
          <p class="mt-1 text-2xl font-bold text-slate-900" data-testid="kpi-products">{{ stats.totalProducts }}</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-slate-500">Pending orders</p>
          <p class="mt-1 text-2xl font-bold text-amber-600">{{ stats.pendingOrders }}</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-slate-500">Low stock products</p>
          <p class="mt-1 text-2xl font-bold" :class="stats.lowStockProducts > 0 ? 'text-rose-600' : 'text-slate-900'" data-testid="kpi-low-stock">
            {{ stats.lowStockProducts }}
          </p>
          <p class="text-xs text-slate-400">Stock ≤ 10</p>
        </div>
      </div>

      <div class="mt-6 grid gap-4 lg:grid-cols-3">
        <!-- Revenue chart -->
        <div class="card p-5 lg:col-span-2">
          <h2 class="font-semibold text-slate-900">Revenue — last 7 days</h2>
          <div class="mt-4 flex h-44 items-end gap-2" data-testid="revenue-chart">
            <div v-for="point in stats.revenueLast7Days" :key="point.date" class="group flex flex-1 flex-col items-center gap-1">
              <span class="text-xs font-medium text-slate-500 opacity-0 transition group-hover:opacity-100">{{ fmt(point.revenue) }}</span>
              <div
                class="w-full rounded-t bg-brand-500 transition hover:bg-brand-600"
                :style="{ height: `${Math.max(4, (point.revenue / maxDailyRevenue) * 140)}px` }"
              />
              <span class="text-[10px] text-slate-400">{{ new Date(point.date).toLocaleDateString('en-IN', { weekday: 'short' }) }}</span>
            </div>
          </div>
        </div>

        <!-- Top categories -->
        <div class="card p-5">
          <h2 class="font-semibold text-slate-900">Top categories</h2>
          <ul class="mt-3 space-y-2 text-sm">
            <li v-for="(category, index) in stats.topCategories" :key="category.categoryName" class="flex items-center justify-between">
              <span class="text-slate-600"><span class="mr-1 font-bold text-slate-400">{{ index + 1 }}</span>{{ category.categoryName }}</span>
              <span class="font-medium text-slate-900">{{ fmt(category.revenue) }}</span>
            </li>
            <li v-if="stats.topCategories.length === 0" class="text-slate-400">No sales yet.</li>
          </ul>
        </div>
      </div>

      <div class="mt-6 grid gap-4 lg:grid-cols-2">
        <!-- Recent orders -->
        <div class="card p-5">
          <div class="flex items-center justify-between">
            <h2 class="font-semibold text-slate-900">Recent orders</h2>
            <router-link :to="{ name: 'admin-orders' }" class="text-sm font-semibold text-brand-600 hover:underline">All orders →</router-link>
          </div>
          <ul class="mt-3 divide-y divide-slate-100 text-sm">
            <li v-for="order in stats.recentOrders" :key="order.id" class="flex items-center justify-between gap-2 py-2">
              <router-link :to="{ name: 'admin-order-detail', params: { id: order.id } }" class="min-w-0">
                <p class="font-medium text-slate-900 hover:text-brand-600">{{ order.orderNumber }}</p>
                <p class="truncate text-xs text-slate-500">{{ order.customerName }} · {{ order.itemCount }} items</p>
              </router-link>
              <div class="flex shrink-0 items-center gap-2">
                <OrderStatusBadge :status="order.status" />
                <span class="font-medium text-slate-900">{{ fmt(order.total) }}</span>
              </div>
            </li>
            <li v-if="stats.recentOrders.length === 0" class="py-2 text-slate-400">No orders yet.</li>
          </ul>
        </div>

        <!-- Low stock -->
        <div class="card p-5">
          <h2 class="font-semibold text-slate-900">Low stock alerts</h2>
          <ul class="mt-3 divide-y divide-slate-100 text-sm">
            <li v-for="item in stats.lowStockItems" :key="item.productId" class="flex items-center justify-between gap-2 py-2" data-testid="low-stock-row">
              <div class="min-w-0">
                <p class="truncate font-medium text-slate-900">{{ item.name }}</p>
                <p class="text-xs text-slate-500">{{ item.sku }} · {{ item.categoryName }}</p>
              </div>
              <span class="badge shrink-0" :class="item.stock === 0 ? 'bg-rose-100 text-rose-700' : 'bg-amber-100 text-amber-800'">
                {{ item.stock === 0 ? 'Out' : `${item.stock} left` }}
              </span>
            </li>
            <li v-if="stats.lowStockItems.length === 0" class="py-2 text-slate-400">All products are sufficiently stocked.</li>
          </ul>
        </div>
      </div>
    </template>
  </div>
</template>

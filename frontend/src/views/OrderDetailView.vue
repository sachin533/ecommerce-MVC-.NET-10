<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { orderService } from '@/services/api'
import ProductImage from '@/components/ProductImage.vue'
import OrderStatusBadge from '@/components/OrderStatusBadge.vue'
import { ApiError } from '@/services/http'
import type { OrderDetail } from '@/types'

const route = useRoute()
const order = ref<OrderDetail | null>(null)
const loading = ref(true)
const notFound = ref(false)

const fmt = (value: number) => `₹${value.toLocaleString('en-IN')}`

onMounted(async () => {
  try {
    order.value = await orderService.getMine(Number(route.params.id))
  } catch (error) {
    if (error instanceof ApiError && (error.status === 404 || error.status === 403)) notFound.value = true
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="mx-auto max-w-4xl px-4 py-8 sm:px-6">
    <router-link :to="{ name: 'orders' }" class="text-sm font-semibold text-brand-600 hover:underline">← Back to orders</router-link>

    <div v-if="loading" class="mt-4 space-y-4">
      <div class="h-24 animate-pulse rounded-xl bg-slate-200" />
      <div class="h-64 animate-pulse rounded-xl bg-slate-200" />
    </div>

    <div v-else-if="notFound || !order" class="mt-4 rounded-xl border border-dashed border-slate-300 p-16 text-center">
      <p class="text-lg font-semibold text-slate-700">Order not found</p>
      <router-link :to="{ name: 'orders' }" class="btn-primary mt-4">Back to orders</router-link>
    </div>

    <template v-else>
      <div class="mt-4 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 class="text-2xl font-bold text-slate-900" data-testid="order-detail-title">{{ order.orderNumber }}</h1>
          <p class="text-sm text-slate-500">
            Placed {{ new Date(order.placedAt).toLocaleString('en-IN', { dateStyle: 'long', timeStyle: 'short' }) }}
          </p>
        </div>
        <OrderStatusBadge :status="order.status" />
      </div>

      <div class="mt-6 grid gap-4 sm:grid-cols-3">
        <div class="card p-4"><p class="text-xs text-slate-500">Items</p><p class="mt-1 text-lg font-bold text-slate-900">{{ order.itemCount }}</p></div>
        <div class="card p-4"><p class="text-xs text-slate-500">Shipping ({{ order.shippingMethod }})</p><p class="mt-1 text-lg font-bold text-slate-900">{{ order.shippingCost === 0 ? 'FREE' : fmt(order.shippingCost) }}</p></div>
        <div class="card p-4"><p class="text-xs text-slate-500">Total</p><p class="mt-1 text-lg font-bold text-slate-900" data-testid="detail-total">{{ fmt(order.total) }}</p></div>
      </div>

      <div class="card mt-6 p-5">
        <h2 class="font-semibold text-slate-900">Items</h2>
        <ul class="mt-3 divide-y divide-slate-100">
          <li v-for="item in order.items" :key="item.id" class="flex items-center gap-3 py-3" data-testid="order-item">
            <router-link v-if="item.slug" :to="{ name: 'product', params: { slug: item.slug } }" class="h-14 w-14 shrink-0 overflow-hidden rounded-lg bg-slate-100">
              <ProductImage :src="item.imageUrl ?? ''" :alt="item.productName" />
            </router-link>
            <div v-else class="h-14 w-14 shrink-0 overflow-hidden rounded-lg bg-slate-100">
              <ProductImage :src="item.imageUrl ?? ''" :alt="item.productName" />
            </div>
            <div class="min-w-0 flex-1">
              <p class="truncate font-medium text-slate-900">{{ item.productName }}</p>
              <p class="text-xs text-slate-500">
                {{ item.sku }}<template v-if="item.variantName"> · {{ item.variantName }}</template> · Qty {{ item.quantity }}
              </p>
            </div>
            <div class="text-right">
              <p class="font-medium text-slate-900">{{ fmt(item.lineTotal) }}</p>
              <p class="text-xs text-slate-400">{{ fmt(item.unitPrice) }} each</p>
            </div>
          </li>
        </ul>
        <dl class="mt-3 space-y-1 border-t border-slate-100 pt-3 text-sm">
          <div class="flex justify-between"><dt class="text-slate-500">Subtotal</dt><dd>{{ fmt(order.subtotal) }}</dd></div>
          <div class="flex justify-between"><dt class="text-slate-500">Shipping</dt><dd>{{ order.shippingCost === 0 ? 'FREE' : fmt(order.shippingCost) }}</dd></div>
          <div class="flex justify-between text-base font-bold"><dt>Total</dt><dd>{{ fmt(order.total) }}</dd></div>
        </dl>
      </div>

      <div class="mt-6 grid gap-4 sm:grid-cols-2">
        <div class="card p-5 text-sm">
          <h2 class="font-semibold text-slate-900">Shipping address</h2>
          <p class="mt-2 text-slate-700">{{ order.shipFullName }}</p>
          <p class="text-slate-600">{{ order.shipLine1 }}<template v-if="order.shipLine2">, {{ order.shipLine2 }}</template></p>
          <p class="text-slate-600">{{ order.shipCity }}, {{ order.shipState }} {{ order.shipPostalCode }}</p>
          <p class="text-slate-500">{{ order.shipPhone }}</p>
        </div>
        <div class="card p-5 text-sm">
          <h2 class="font-semibold text-slate-900">Payment</h2>
          <p class="mt-2 text-slate-700">{{ order.payment?.provider ?? 'mock' }}<template v-if="order.payment?.cardLast4"> ···· {{ order.payment.cardLast4 }}</template></p>
          <p class="text-slate-600">Status: {{ order.payment?.status ?? 'Pending' }}</p>
          <p v-if="order.payment?.failureReason" class="mt-1 text-xs text-rose-600">{{ order.payment.failureReason }}</p>
          <p v-if="order.payment?.transactionId" class="mt-1 text-xs text-slate-400">Txn {{ order.payment.transactionId }}</p>
          <p v-if="order.customerNote" class="mt-3 rounded bg-slate-50 p-2 text-xs text-slate-600">Note: {{ order.customerNote }}</p>
        </div>
      </div>
    </template>
  </div>
</template>

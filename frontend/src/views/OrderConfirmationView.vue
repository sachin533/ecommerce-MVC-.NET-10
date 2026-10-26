<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { orderService } from '@/services/api'
import ProductImage from '@/components/ProductImage.vue'
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
    if (error instanceof ApiError && error.status === 404) notFound.value = true
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="mx-auto max-w-3xl px-4 py-10 sm:px-6">
    <div v-if="loading" class="space-y-4">
      <div class="h-24 animate-pulse rounded-xl bg-slate-200" />
      <div class="h-64 animate-pulse rounded-xl bg-slate-200" />
    </div>

    <div v-else-if="notFound" class="rounded-xl border border-dashed border-slate-300 p-16 text-center">
      <p class="text-lg font-semibold text-slate-700">Order not found</p>
      <router-link :to="{ name: 'orders' }" class="btn-primary mt-4">View my orders</router-link>
    </div>

    <template v-else-if="order">
      <!-- Success hero -->
      <div class="card overflow-hidden text-center" data-testid="confirmation-hero">
        <div class="bg-emerald-50 px-6 py-8">
          <span class="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-emerald-500 text-white">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" class="h-8 w-8"><path d="m5 13 4 4L19 7" stroke-linecap="round" stroke-linejoin="round" /></svg>
          </span>
          <h1 class="mt-4 text-2xl font-bold text-slate-900" data-testid="confirmation-title">Order confirmed!</h1>
          <p class="mt-1 text-slate-600">
            Thanks {{ order.customerName?.split(' ')[0] ?? 'there' }} — order
            <span class="font-bold" data-testid="order-number">{{ order.orderNumber }}</span> is placed.
          </p>
          <p class="mt-1 text-sm text-slate-500">A confirmation has been recorded. Payment status:
            <span class="font-semibold" data-testid="payment-status">{{ order.payment?.status ?? 'Pending' }}</span>
          </p>
        </div>

        <div class="px-6 py-5 text-left">
          <dl class="grid grid-cols-2 gap-4 text-sm sm:grid-cols-4">
            <div><dt class="text-slate-500">Total</dt><dd class="font-bold text-slate-900">{{ fmt(order.total) }}</dd></div>
            <div><dt class="text-slate-500">Items</dt><dd class="font-bold text-slate-900">{{ order.itemCount }}</dd></div>
            <div><dt class="text-slate-500">Shipping</dt><dd class="font-bold capitalize text-slate-900">{{ order.shippingMethod }}</dd></div>
            <div><dt class="text-slate-500">Status</dt><dd class="font-bold text-slate-900">{{ order.status }}</dd></div>
          </dl>
        </div>
      </div>

      <!-- Items -->
      <div class="card mt-6 p-5">
        <h2 class="font-semibold text-slate-900">Items</h2>
        <ul class="mt-3 divide-y divide-slate-100">
          <li v-for="item in order.items" :key="item.id" class="flex items-center gap-3 py-3" data-testid="confirmation-item">
            <div class="h-14 w-14 shrink-0 overflow-hidden rounded-lg bg-slate-100">
              <ProductImage :src="item.imageUrl ?? ''" :alt="item.productName" />
            </div>
            <div class="min-w-0 flex-1">
              <p class="truncate font-medium text-slate-900">{{ item.productName }}</p>
              <p class="text-xs text-slate-500">Qty {{ item.quantity }}<template v-if="item.variantName"> · {{ item.variantName }}</template> · {{ item.sku }}</p>
            </div>
            <span class="font-medium text-slate-900">{{ fmt(item.lineTotal) }}</span>
          </li>
        </ul>
        <dl class="mt-3 space-y-1 border-t border-slate-100 pt-3 text-sm">
          <div class="flex justify-between"><dt class="text-slate-500">Subtotal</dt><dd>{{ fmt(order.subtotal) }}</dd></div>
          <div class="flex justify-between"><dt class="text-slate-500">Shipping</dt><dd>{{ order.shippingCost === 0 ? 'FREE' : fmt(order.shippingCost) }}</dd></div>
          <div class="flex justify-between text-base font-bold"><dt>Total</dt><dd>{{ fmt(order.total) }}</dd></div>
        </dl>
      </div>

      <!-- Shipping + payment -->
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
          <p class="mt-2 text-slate-700">{{ order.payment?.provider ?? 'mock' }}<template v-if="order.payment?.cardLast4"> ending in {{ order.payment.cardLast4 }}</template></p>
          <p class="text-slate-600">Status: {{ order.payment?.status ?? 'Pending' }}</p>
          <p v-if="order.payment?.transactionId" class="text-xs text-slate-500">Txn {{ order.payment.transactionId }}</p>
        </div>
      </div>

      <div class="mt-6 flex justify-center gap-3">
        <router-link :to="{ name: 'orders' }" class="btn-secondary">View all orders</router-link>
        <router-link :to="{ name: 'shop' }" class="btn-primary">Continue shopping</router-link>
      </div>
    </template>
  </div>
</template>

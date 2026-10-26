<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { adminService } from '@/services/api'
import OrderStatusBadge from '@/components/OrderStatusBadge.vue'
import ProductImage from '@/components/ProductImage.vue'
import { useToastStore } from '@/stores/toast'
import { ApiError } from '@/services/http'
import type { OrderDetail } from '@/types'

const route = useRoute()
const toasts = useToastStore()

const order = ref<OrderDetail | null>(null)
const loading = ref(true)
const updating = ref(false)

const fmt = (value: number) => `₹${value.toLocaleString('en-IN')}`

/** Allowed transitions mirror the server's OrderStatusTransitions rules. */
const transitions: Record<string, string[]> = {
  Pending: ['Paid', 'Processing', 'Cancelled'],
  Paid: ['Processing', 'Cancelled'],
  Processing: ['Shipped', 'Cancelled'],
  Shipped: ['Delivered'],
  Delivered: [],
  Cancelled: [],
}

const availableNext = computed(() => (order.value ? transitions[order.value.status] ?? [] : []))

async function load() {
  loading.value = true
  try {
    order.value = await adminService.order(Number(route.params.id))
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not load the order')
  } finally {
    loading.value = false
  }
}

async function setStatus(status: string) {
  if (!order.value) return
  if (status === 'Cancelled' && !confirm(`Cancel order ${order.value.orderNumber}? Stock will be returned to inventory.`)) return
  updating.value = true
  try {
    order.value = await adminService.updateOrderStatus(order.value.id, status)
    toasts.success(`Order marked ${status}`)
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not update the status')
  } finally {
    updating.value = false
  }
}

onMounted(load)
</script>

<template>
  <div>
    <router-link :to="{ name: 'admin-orders' }" class="text-sm font-semibold text-brand-600 hover:underline">← Back to orders</router-link>

    <div v-if="loading" class="mt-4 space-y-4">
      <div class="h-24 animate-pulse rounded-xl bg-slate-200" />
      <div class="h-64 animate-pulse rounded-xl bg-slate-200" />
    </div>

    <template v-else-if="order">
      <div class="mt-2 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 class="text-2xl font-bold text-slate-900" data-testid="admin-order-title">{{ order.orderNumber }}</h1>
          <p class="text-sm text-slate-500">
            {{ order.customerName }} ({{ order.customerEmail }}) ·
            {{ new Date(order.placedAt).toLocaleString('en-IN', { dateStyle: 'long', timeStyle: 'short' }) }}
          </p>
        </div>
        <OrderStatusBadge :status="order.status" />
      </div>

      <!-- Status actions -->
      <div class="card mt-4 p-4" data-testid="status-actions">
        <div class="flex flex-wrap items-center gap-2">
          <span class="text-sm font-semibold text-slate-700">Move to:</span>
          <button
            v-for="next in availableNext"
            :key="next"
            class="btn-secondary text-sm"
            :class="{ 'border-rose-300 text-rose-600 hover:bg-rose-50': next === 'Cancelled' }"
            :disabled="updating"
            :data-testid="`status-${next.toLowerCase()}`"
            @click="setStatus(next)"
          >
            {{ next }}
          </button>
          <span v-if="availableNext.length === 0" class="text-sm text-slate-400">No further transitions from “{{ order.status }}”.</span>
        </div>
        <p class="mt-2 text-xs text-slate-400">Cancelling an order returns its stock and refunds a successful payment (mock provider).</p>
      </div>

      <div class="mt-4 grid gap-4 lg:grid-cols-3">
        <!-- Items -->
        <div class="card p-5 lg:col-span-2">
          <h2 class="font-semibold text-slate-900">Items</h2>
          <ul class="mt-3 divide-y divide-slate-100 text-sm">
            <li v-for="item in order.items" :key="item.id" class="flex items-center gap-3 py-3" data-testid="admin-order-item">
              <div class="h-12 w-12 shrink-0 overflow-hidden rounded-lg bg-slate-100">
                <ProductImage :src="item.imageUrl ?? ''" :alt="item.productName" />
              </div>
              <div class="min-w-0 flex-1">
                <p class="truncate font-medium text-slate-900">{{ item.productName }}</p>
                <p class="text-xs text-slate-500">{{ item.sku }}<template v-if="item.variantName"> · {{ item.variantName }}</template></p>
              </div>
              <div class="text-right">
                <p class="font-medium text-slate-900">{{ fmt(item.lineTotal) }}</p>
                <p class="text-xs text-slate-400">{{ fmt(item.unitPrice) }} × {{ item.quantity }}</p>
              </div>
            </li>
          </ul>
          <dl class="mt-3 space-y-1 border-t border-slate-100 pt-3 text-sm">
            <div class="flex justify-between"><dt class="text-slate-500">Subtotal</dt><dd>{{ fmt(order.subtotal) }}</dd></div>
            <div class="flex justify-between"><dt class="text-slate-500">Shipping ({{ order.shippingMethod }})</dt><dd>{{ order.shippingCost === 0 ? 'FREE' : fmt(order.shippingCost) }}</dd></div>
            <div class="flex justify-between text-base font-bold"><dt>Total</dt><dd>{{ fmt(order.total) }}</dd></div>
          </dl>
        </div>

        <!-- Side details -->
        <div class="space-y-4">
          <div class="card p-5 text-sm">
            <h2 class="font-semibold text-slate-900">Ship to</h2>
            <p class="mt-2 text-slate-700">{{ order.shipFullName }}</p>
            <p class="text-slate-600">{{ order.shipLine1 }}<template v-if="order.shipLine2">, {{ order.shipLine2 }}</template></p>
            <p class="text-slate-600">{{ order.shipCity }}, {{ order.shipState }} {{ order.shipPostalCode }}</p>
            <p class="text-slate-500">{{ order.shipPhone }}</p>
          </div>
          <div class="card p-5 text-sm">
            <h2 class="font-semibold text-slate-900">Payment</h2>
            <p class="mt-2 text-slate-700">{{ order.payment?.provider ?? 'mock' }}<template v-if="order.payment?.cardLast4"> ···· {{ order.payment.cardLast4 }}</template></p>
            <p class="text-slate-600">Status: {{ order.payment?.status ?? 'Pending' }}</p>
            <p v-if="order.payment?.transactionId" class="text-xs text-slate-400">Txn {{ order.payment.transactionId }}</p>
            <p v-if="order.payment?.failureReason" class="mt-1 text-xs text-rose-600">{{ order.payment.failureReason }}</p>
          </div>
          <div v-if="order.customerNote" class="card p-5 text-sm">
            <h2 class="font-semibold text-slate-900">Customer note</h2>
            <p class="mt-2 text-slate-600">{{ order.customerNote }}</p>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

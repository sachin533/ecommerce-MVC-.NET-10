<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import ProductImage from '@/components/ProductImage.vue'
import PriceDisplay from '@/components/PriceDisplay.vue'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useToastStore } from '@/stores/toast'
import { ApiError } from '@/services/http'

const router = useRouter()
const auth = useAuthStore()
const cart = useCartStore()
const toasts = useToastStore()

const busyId = ref<number | null>(null)

onMounted(async () => {
  if (cart.isEmpty && !cart.cart) await cart.fetch()
})

async function changeQuantity(id: number, quantity: number) {
  if (quantity < 1) return
  busyId.value = id
  try {
    await cart.updateQuantity(id, quantity)
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not update quantity')
    await cart.fetch()
  } finally {
    busyId.value = null
  }
}

async function removeItem(id: number, name: string) {
  busyId.value = id
  try {
    await cart.remove(id)
    toasts.info(`Removed “${name}”`)
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not remove item')
  } finally {
    busyId.value = null
  }
}

async function clearCart() {
  try {
    await cart.clear()
    toasts.info('Cart cleared')
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not clear cart')
  }
}

function checkout() {
  if (!auth.isAuthenticated) {
    router.push({ name: 'login', query: { redirect: '/checkout' } })
    return
  }
  router.push({ name: 'checkout' })
}

// When the guest signs in mid-visit the cart store is replaced — nothing to do,
// but keep the view reactive to owner changes.
watch(
  () => cart.cart?.owner,
  () => {},
)
</script>

<template>
  <div class="mx-auto max-w-7xl px-4 py-8 sm:px-6">
    <div class="mb-6 flex items-center justify-between">
      <h1 class="text-2xl font-bold text-slate-900" data-testid="cart-title">Your cart</h1>
      <button v-if="cart.itemCount > 0" class="text-sm font-semibold text-rose-600 hover:underline" @click="clearCart">
        Clear cart
      </button>
    </div>

    <p v-if="cart.cart?.owner === 'guest'" class="mb-4 rounded-lg bg-sky-50 px-4 py-2 text-sm text-sky-700">
      You're shopping as a guest.
      <router-link :to="{ name: 'login' }" class="font-semibold underline">Sign in</router-link> to keep this cart across devices.
    </p>

    <div v-if="cart.isEmpty" class="rounded-xl border border-dashed border-slate-300 p-16 text-center" data-testid="empty-cart">
      <p class="text-lg font-semibold text-slate-700">Your cart is empty</p>
      <p class="mt-1 text-sm text-slate-500">Browse the catalog and add something you love.</p>
      <router-link :to="{ name: 'shop' }" class="btn-primary mt-4">Start shopping</router-link>
    </div>

    <div v-else class="grid gap-8 lg:grid-cols-[1fr_340px]">
      <!-- Items -->
      <ul class="space-y-4" data-testid="cart-items">
        <li v-for="item in cart.items" :key="item.id" class="card flex gap-4 p-4" data-testid="cart-line">
          <router-link :to="{ name: 'product', params: { slug: item.slug } }" class="h-24 w-24 shrink-0 overflow-hidden rounded-lg bg-slate-100">
            <ProductImage :src="item.imageUrl" :alt="item.name" />
          </router-link>

          <div class="flex flex-1 flex-col">
            <div class="flex items-start justify-between gap-2">
              <div>
                <router-link :to="{ name: 'product', params: { slug: item.slug } }" class="text-sm font-semibold text-slate-900 hover:text-brand-600">
                  {{ item.name }}
                </router-link>
                <p class="mt-0.5 text-xs text-slate-500">
                  {{ item.brand }}
                  <template v-if="item.variantName"> · {{ item.variantName }}</template>
                </p>
              </div>
              <button
                class="rounded p-1 text-slate-400 hover:text-rose-500 disabled:opacity-40"
                aria-label="Remove item"
                :disabled="busyId === item.id"
                :data-testid="`remove-${item.id}`"
                @click="removeItem(item.id, item.name)"
              >
                <svg viewBox="0 0 20 20" fill="currentColor" class="h-5 w-5"><path d="M6.3 4.9 4.9 6.3 8.6 10l-3.7 3.7 1.4 1.4L10 11.4l3.7 3.7 1.4-1.4L11.4 10l3.7-3.7-1.4-1.4L10 8.6 6.3 4.9Z" /></svg>
              </button>
            </div>

            <div class="mt-auto flex items-center justify-between gap-3 pt-2">
              <div class="flex items-center rounded-lg border border-slate-300">
                <button
                  class="px-2.5 py-1.5 text-slate-600 hover:bg-slate-50 disabled:opacity-40"
                  aria-label="Decrease quantity"
                  :disabled="item.quantity <= 1 || busyId === item.id"
                  @click="changeQuantity(item.id, item.quantity - 1)"
                >−</button>
                <span class="w-8 text-center text-sm font-semibold" :data-testid="`qty-${item.id}`">{{ item.quantity }}</span>
                <button
                  class="px-2.5 py-1.5 text-slate-600 hover:bg-slate-50 disabled:opacity-40"
                  aria-label="Increase quantity"
                  :disabled="item.quantity >= item.maxQuantity || busyId === item.id"
                  @click="changeQuantity(item.id, item.quantity + 1)"
                >+</button>
              </div>
              <PriceDisplay :price="item.unitPrice" :mrp="item.mrp" :discount-percent="item.discountPercent" />
            </div>

            <p v-if="!item.isAvailable" class="mt-1 text-xs font-medium text-rose-600">This item is no longer available.</p>
            <p v-else-if="item.quantity >= item.stock && item.stock <= 5" class="mt-1 text-xs font-medium text-amber-600">
              Only {{ item.stock }} in stock.
            </p>
          </div>
        </li>
      </ul>

      <!-- Summary -->
      <aside class="card h-fit p-5 lg:sticky lg:top-20" data-testid="order-summary">
        <h2 class="text-lg font-semibold text-slate-900">Order summary</h2>
        <dl class="mt-4 space-y-2 text-sm">
          <div class="flex justify-between"><dt class="text-slate-500">Subtotal ({{ cart.itemCount }} items)</dt><dd class="font-medium" data-testid="cart-subtotal">₹{{ cart.subtotal.toLocaleString('en-IN') }}</dd></div>
          <div class="flex justify-between"><dt class="text-slate-500">Estimated shipping</dt><dd class="font-medium">{{ cart.estimatedShipping === 0 ? 'FREE' : `₹${cart.estimatedShipping}` }}</dd></div>
          <div class="mt-2 flex justify-between border-t border-slate-100 pt-2 text-base font-bold"><dt>Total</dt><dd data-testid="cart-total">₹{{ cart.total.toLocaleString('en-IN') }}</dd></div>
        </dl>
        <p class="mt-2 text-xs text-slate-400">Final shipping is chosen at checkout (standard ₹49, free over ₹999 · express ₹199).</p>
        <button class="btn-primary mt-4 w-full py-3" data-testid="checkout-button" @click="checkout">
          Proceed to checkout
        </button>
        <router-link :to="{ name: 'shop' }" class="btn-secondary mt-2 w-full">Continue shopping</router-link>
      </aside>
    </div>
  </div>
</template>

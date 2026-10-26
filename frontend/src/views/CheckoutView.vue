<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { addressService, orderService } from '@/services/api'
import ProductImage from '@/components/ProductImage.vue'
import { useCartStore } from '@/stores/cart'
import { useToastStore } from '@/stores/toast'
import { ApiError } from '@/services/http'
import type { Address, CheckoutResponse } from '@/types'

const router = useRouter()
const cart = useCartStore()
const toasts = useToastStore()

const steps = ['Address', 'Shipping', 'Payment', 'Review'] as const
const step = ref<1 | 2 | 3 | 4>(1)

const addresses = ref<Address[]>([])
const loadingAddresses = ref(true)

const form = reactive({
  // Step 1 — address
  addressMode: 'saved' as 'saved' | 'new',
  addressId: null as number | null,
  newAddress: {
    fullName: '',
    phone: '',
    line1: '',
    line2: '',
    city: '',
    state: '',
    postalCode: '',
    country: 'India',
    isDefault: false,
  },
  // Step 2 — shipping
  shippingMethod: 'standard' as 'standard' | 'express',
  // Step 3 — payment
  cardName: '',
  cardNumber: '',
  cardExpiry: '',
  cardCvv: '',
  // Step 4 — review
  customerNote: '',
})

const submitting = ref(false)
const paymentError = ref('')
const failedOrderNumber = ref<string | null>(null)

const SHIPPING = { standard: 49, express: 199 } as const
const FREE_SHIPPING_THRESHOLD = 999

const shippingCost = computed(() => {
  if (form.shippingMethod === 'express') return SHIPPING.express
  return cart.subtotal >= FREE_SHIPPING_THRESHOLD ? 0 : SHIPPING.standard
})
const grandTotal = computed(() => cart.subtotal + shippingCost.value)

const selectedAddress = computed(
  () => addresses.value.find((a) => a.id === form.addressId) ?? null,
)

/** Last 4 digits only — the full PAN is never transmitted or stored. */
const cardLast4 = computed(() => form.cardNumber.replace(/\D/g, '').slice(-4))

const canContinueFromAddress = computed(() => {
  if (form.addressMode === 'saved') return form.addressId !== null
  const n = form.newAddress
  return Boolean(n.fullName && n.phone && n.line1 && n.city && n.state && n.postalCode)
})

const canContinueFromPayment = computed(() => {
  const digits = form.cardNumber.replace(/\D/g, '')
  return (
    form.cardName.trim().length >= 2 &&
    digits.length >= 12 &&
    /^\d{2}\/\d{2}$/.test(form.cardExpiry) &&
    /^\d{3,4}$/.test(form.cardCvv)
  )
})

function formatCardNumber(event: Event) {
  const input = event.target as HTMLInputElement
  const digits = input.value.replace(/\D/g, '').slice(0, 16)
  form.cardNumber = digits.replace(/(\d{4})(?=\d)/g, '$1 ')
}

function formatExpiry(event: Event) {
  const input = event.target as HTMLInputElement
  const digits = input.value.replace(/\D/g, '').slice(0, 4)
  form.cardExpiry = digits.length > 2 ? `${digits.slice(0, 2)}/${digits.slice(2)}` : digits
}

onMounted(async () => {
  if (cart.isEmpty) await cart.fetch()
  if (cart.isEmpty) {
    router.replace({ name: 'cart' })
    return
  }
  try {
    addresses.value = await addressService.list()
    const defaultAddress = addresses.value.find((a) => a.isDefault) ?? addresses.value[0]
    if (defaultAddress) {
      form.addressId = defaultAddress.id
    } else {
      form.addressMode = 'new'
    }
  } catch {
    form.addressMode = 'new'
  } finally {
    loadingAddresses.value = false
  }
})

async function saveNewAddress(): Promise<number | null> {
  if (form.addressMode !== 'new') return form.addressId
  const created = await addressService.create({ ...form.newAddress })
  addresses.value = [...addresses.value, created]
  return created.id
}

async function placeOrder() {
  submitting.value = true
  paymentError.value = ''
  failedOrderNumber.value = null
  try {
    const addressId = await saveNewAddress()
    const response: CheckoutResponse = await orderService.checkout({
      addressId,
      newAddress: null,
      shippingMethod: form.shippingMethod,
      paymentMethod: 'mock_card',
      cardLast4: cardLast4.value,
      customerNote: form.customerNote.trim() || null,
    })
    await cart.fetch() // server cleared the purchased items
    router.push({ name: 'order-confirmation', params: { id: String(response.order.id) } })
  } catch (error) {
    if (error instanceof ApiError && error.status === 402) {
      // Payment declined: the order was cancelled and stock restored server-side.
      paymentError.value = error.problem.providerReason || error.message
      failedOrderNumber.value = error.problem.orderNumber ?? null
      step.value = 3
      toasts.error('Payment was declined — your cart is untouched.')
    } else if (error instanceof ApiError) {
      paymentError.value = error.message
      toasts.error(error.message)
    } else {
      paymentError.value = 'Something went wrong. Please try again.'
      toasts.error('Something went wrong')
    }
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="mx-auto max-w-5xl px-4 py-8 sm:px-6">
    <h1 class="mb-6 text-2xl font-bold text-slate-900" data-testid="checkout-title">Checkout</h1>

    <!-- Stepper -->
    <ol class="mb-8 flex items-center gap-2" data-testid="checkout-stepper">
      <li v-for="(label, index) in steps" :key="label" class="flex flex-1 items-center gap-2">
        <span
          class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-sm font-bold"
          :class="step > index + 1 ? 'bg-emerald-500 text-white' : step === index + 1 ? 'bg-brand-600 text-white' : 'bg-slate-200 text-slate-500'"
          :data-testid="`step-${index + 1}`"
        >
          <svg v-if="step > index + 1" viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4"><path d="M16.7 5.3a1 1 0 0 1 0 1.4l-7.5 7.5a1 1 0 0 1-1.4 0L3.3 9.7a1 1 0 1 1 1.4-1.4l3.8 3.8 6.8-6.8a1 1 0 0 1 1.4 0Z" /></svg>
          <template v-else>{{ index + 1 }}</template>
        </span>
        <span class="text-sm font-medium" :class="step === index + 1 ? 'text-slate-900' : 'text-slate-400'">{{ label }}</span>
        <span v-if="index < steps.length - 1" class="h-px flex-1 bg-slate-200" />
      </li>
    </ol>

    <div class="grid gap-8 lg:grid-cols-[1fr_320px]">
      <section class="card p-6">
        <!-- Step 1: address -->
        <div v-if="step === 1" data-testid="step-address">
          <h2 class="text-lg font-semibold text-slate-900">Shipping address</h2>

          <div v-if="loadingAddresses" class="mt-4 space-y-2">
            <div v-for="i in 2" :key="i" class="h-16 animate-pulse rounded-lg bg-slate-200" />
          </div>

          <template v-else>
            <div v-if="addresses.length > 0" class="mt-4 space-y-2">
              <label
                v-for="address in addresses"
                :key="address.id"
                class="flex cursor-pointer items-start gap-3 rounded-lg border p-3 transition"
                :class="form.addressMode === 'saved' && form.addressId === address.id ? 'border-brand-500 bg-brand-50' : 'border-slate-200 hover:border-slate-300'"
                data-testid="address-option"
              >
                <input v-model="form.addressId" type="radio" :value="address.id" class="mt-1" @change="form.addressMode = 'saved'" />
                <span class="text-sm">
                  <span class="font-semibold text-slate-900">{{ address.fullName }}</span>
                  <span v-if="address.isDefault" class="badge ml-2 bg-brand-50 text-brand-700">Default</span>
                  <span class="block text-slate-600">{{ address.line1 }}<template v-if="address.line2">, {{ address.line2 }}</template></span>
                  <span class="block text-slate-600">{{ address.city }}, {{ address.state }} {{ address.postalCode }}</span>
                  <span class="block text-slate-500">{{ address.phone }}</span>
                </span>
              </label>
            </div>

            <button
              class="mt-3 text-sm font-semibold text-brand-600 hover:underline"
              data-testid="toggle-new-address"
              @click="form.addressMode = form.addressMode === 'new' ? 'saved' : 'new'; form.addressId = null"
            >
              {{ form.addressMode === 'new' ? '← Use a saved address' : '+ Ship to a new address' }}
            </button>

            <!-- New address form -->
            <div v-if="form.addressMode === 'new' || addresses.length === 0" class="mt-4 grid gap-4 sm:grid-cols-2" data-testid="new-address-form">
              <div class="sm:col-span-2">
                <label class="label" for="addr-name">Full name</label>
                <input id="addr-name" v-model="form.newAddress.fullName" class="input" autocomplete="name" required />
              </div>
              <div>
                <label class="label" for="addr-phone">Phone</label>
                <input id="addr-phone" v-model="form.newAddress.phone" class="input" autocomplete="tel" required />
              </div>
              <div>
                <label class="label" for="addr-postal">PIN code</label>
                <input id="addr-postal" v-model="form.newAddress.postalCode" class="input" autocomplete="postal-code" required />
              </div>
              <div class="sm:col-span-2">
                <label class="label" for="addr-line1">Address line 1</label>
                <input id="addr-line1" v-model="form.newAddress.line1" class="input" autocomplete="address-line1" required />
              </div>
              <div class="sm:col-span-2">
                <label class="label" for="addr-line2">Address line 2 (optional)</label>
                <input id="addr-line2" v-model="form.newAddress.line2" class="input" autocomplete="address-line2" />
              </div>
              <div>
                <label class="label" for="addr-city">City</label>
                <input id="addr-city" v-model="form.newAddress.city" class="input" autocomplete="address-level2" required />
              </div>
              <div>
                <label class="label" for="addr-state">State</label>
                <input id="addr-state" v-model="form.newAddress.state" class="input" autocomplete="address-level1" required />
              </div>
              <label class="flex items-center gap-2 text-sm text-slate-700 sm:col-span-2">
                <input v-model="form.newAddress.isDefault" type="checkbox" class="h-4 w-4 rounded border-slate-300 text-brand-600" />
                Set as default address
              </label>
            </div>
          </template>

          <div class="mt-6 flex justify-end">
            <button class="btn-primary" :disabled="!canContinueFromAddress" data-testid="next-shipping" @click="step = 2">
              Continue to shipping
            </button>
          </div>
        </div>

        <!-- Step 2: shipping -->
        <div v-else-if="step === 2" data-testid="step-shipping">
          <h2 class="text-lg font-semibold text-slate-900">Shipping method</h2>
          <div class="mt-4 space-y-3">
            <label
              class="flex cursor-pointer items-center justify-between rounded-lg border p-4 transition"
              :class="form.shippingMethod === 'standard' ? 'border-brand-500 bg-brand-50' : 'border-slate-200 hover:border-slate-300'"
              data-testid="shipping-standard"
            >
              <span class="flex items-center gap-3">
                <input v-model="form.shippingMethod" type="radio" value="standard" />
                <span>
                  <span class="block font-semibold text-slate-900">Standard delivery</span>
                  <span class="block text-sm text-slate-500">3–5 business days</span>
                </span>
              </span>
              <span class="font-bold text-slate-900">{{ cart.subtotal >= 999 ? 'FREE' : '₹49' }}</span>
            </label>
            <label
              class="flex cursor-pointer items-center justify-between rounded-lg border p-4 transition"
              :class="form.shippingMethod === 'express' ? 'border-brand-500 bg-brand-50' : 'border-slate-200 hover:border-slate-300'"
              data-testid="shipping-express"
            >
              <span class="flex items-center gap-3">
                <input v-model="form.shippingMethod" type="radio" value="express" />
                <span>
                  <span class="block font-semibold text-slate-900">Express delivery</span>
                  <span class="block text-sm text-slate-500">1–2 business days</span>
                </span>
              </span>
              <span class="font-bold text-slate-900">₹199</span>
            </label>
          </div>
          <div class="mt-6 flex justify-between">
            <button class="btn-secondary" @click="step = 1">Back</button>
            <button class="btn-primary" data-testid="next-payment" @click="step = 3">Continue to payment</button>
          </div>
        </div>

        <!-- Step 3: payment -->
        <div v-else-if="step === 3" data-testid="step-payment">
          <h2 class="text-lg font-semibold text-slate-900">Payment</h2>
          <p class="mt-1 rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-800" data-testid="mock-notice">
            Demo environment: a <strong>mock payment provider</strong> is used. Only the last 4 digits of your card are sent —
            the full card number and CVV never leave this page and are never stored.
          </p>

          <div v-if="paymentError" class="mt-4 rounded-lg border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700" data-testid="payment-error">
            <p class="font-semibold">Payment failed{{ failedOrderNumber ? ` (order ${failedOrderNumber})` : '' }}</p>
            <p class="mt-1">{{ paymentError }}</p>
            <p class="mt-1 text-xs">Your cart was preserved and any reserved stock was released.</p>
          </div>

          <div class="mt-4 grid gap-4 sm:grid-cols-2">
            <div class="sm:col-span-2">
              <label class="label" for="card-name">Name on card</label>
              <input id="card-name" v-model="form.cardName" class="input" autocomplete="cc-name" placeholder="Ada Lovelace" />
            </div>
            <div class="sm:col-span-2">
              <label class="label" for="card-number">Card number</label>
              <input
                id="card-number"
                :value="form.cardNumber"
                class="input"
                inputmode="numeric"
                autocomplete="cc-number"
                placeholder="4111 1111 1111 1111"
                data-testid="card-number"
                @input="formatCardNumber"
              />
            </div>
            <div>
              <label class="label" for="card-expiry">Expiry (MM/YY)</label>
              <input
                id="card-expiry"
                :value="form.cardExpiry"
                class="input"
                inputmode="numeric"
                autocomplete="cc-exp"
                placeholder="12/29"
                data-testid="card-expiry"
                @input="formatExpiry"
              />
            </div>
            <div>
              <label class="label" for="card-cvv">CVV</label>
              <input
                id="card-cvv"
                v-model="form.cardCvv"
                class="input"
                type="password"
                inputmode="numeric"
                autocomplete="cc-csc"
                placeholder="123"
                maxlength="4"
                data-testid="card-cvv"
              />
              <p class="mt-1 text-xs text-slate-400">Kept in memory only; never transmitted.</p>
            </div>
          </div>

          <p class="mt-3 text-xs text-slate-400">Tip: use a card ending in an odd digit to simulate a declined payment.</p>

          <div class="mt-6 flex justify-between">
            <button class="btn-secondary" @click="step = 2">Back</button>
            <button class="btn-primary" :disabled="!canContinueFromPayment" data-testid="next-review" @click="step = 4">
              Review order
            </button>
          </div>
        </div>

        <!-- Step 4: review -->
        <div v-else data-testid="step-review">
          <h2 class="text-lg font-semibold text-slate-900">Review your order</h2>

          <div class="mt-4 space-y-4 text-sm">
            <div class="rounded-lg bg-slate-50 p-4">
              <div class="flex items-center justify-between">
                <span class="font-semibold text-slate-700">Ship to</span>
                <button class="font-semibold text-brand-600 hover:underline" @click="step = 1">Change</button>
              </div>
              <template v-if="form.addressMode === 'saved' && selectedAddress">
                <p class="mt-1 text-slate-900">{{ selectedAddress.fullName }} · {{ selectedAddress.phone }}</p>
                <p class="text-slate-600">{{ selectedAddress.line1 }}<template v-if="selectedAddress.line2">, {{ selectedAddress.line2 }}</template>, {{ selectedAddress.city }}, {{ selectedAddress.state }} {{ selectedAddress.postalCode }}</p>
              </template>
              <template v-else>
                <p class="mt-1 text-slate-900">{{ form.newAddress.fullName }} · {{ form.newAddress.phone }}</p>
                <p class="text-slate-600">{{ form.newAddress.line1 }}<template v-if="form.newAddress.line2">, {{ form.newAddress.line2 }}</template>, {{ form.newAddress.city }}, {{ form.newAddress.state }} {{ form.newAddress.postalCode }}</p>
              </template>
            </div>

            <div class="rounded-lg bg-slate-50 p-4">
              <div class="flex items-center justify-between">
                <span class="font-semibold text-slate-700">Shipping</span>
                <button class="font-semibold text-brand-600 hover:underline" @click="step = 2">Change</button>
              </div>
              <p class="mt-1 text-slate-900 capitalize">{{ form.shippingMethod }} delivery · {{ shippingCost === 0 ? 'FREE' : `₹${shippingCost}` }}</p>
            </div>

            <div class="rounded-lg bg-slate-50 p-4">
              <div class="flex items-center justify-between">
                <span class="font-semibold text-slate-700">Payment</span>
                <button class="font-semibold text-brand-600 hover:underline" @click="step = 3">Change</button>
              </div>
              <p class="mt-1 text-slate-900">Mock card ending in {{ cardLast4 }}</p>
            </div>

            <div>
              <label class="label" for="order-note">Order note (optional)</label>
              <textarea id="order-note" v-model="form.customerNote" rows="2" class="input" maxlength="1000" placeholder="Delivery instructions…" data-testid="order-note" />
            </div>

            <ul class="divide-y divide-slate-100 rounded-lg border border-slate-100" data-testid="review-items">
              <li v-for="item in cart.items" :key="item.id" class="flex items-center gap-3 p-3">
                <div class="h-12 w-12 shrink-0 overflow-hidden rounded-md bg-slate-100">
                  <ProductImage :src="item.imageUrl" :alt="item.name" />
                </div>
                <div class="min-w-0 flex-1">
                  <p class="truncate font-medium text-slate-900">{{ item.name }}</p>
                  <p class="text-xs text-slate-500">Qty {{ item.quantity }}<template v-if="item.variantName"> · {{ item.variantName }}</template></p>
                </div>
                <span class="font-medium text-slate-900">₹{{ item.lineTotal.toLocaleString('en-IN') }}</span>
              </li>
            </ul>
          </div>

          <div class="mt-6 flex justify-between">
            <button class="btn-secondary" :disabled="submitting" @click="step = 3">Back</button>
            <button class="btn-primary px-8 py-3" :disabled="submitting" data-testid="place-order" @click="placeOrder">
              {{ submitting ? 'Processing payment…' : `Pay ₹${grandTotal.toLocaleString('en-IN')}` }}
            </button>
          </div>
        </div>
      </section>

      <!-- Summary sidebar -->
      <aside class="card h-fit p-5 lg:sticky lg:top-20">
        <h2 class="font-semibold text-slate-900">Summary</h2>
        <dl class="mt-3 space-y-2 text-sm">
          <div class="flex justify-between"><dt class="text-slate-500">Items ({{ cart.itemCount }})</dt><dd>₹{{ cart.subtotal.toLocaleString('en-IN') }}</dd></div>
          <div class="flex justify-between"><dt class="text-slate-500">Shipping</dt><dd>{{ shippingCost === 0 ? 'FREE' : `₹${shippingCost}` }}</dd></div>
          <div class="flex justify-between border-t border-slate-100 pt-2 text-base font-bold"><dt>Total</dt><dd data-testid="checkout-total">₹{{ grandTotal.toLocaleString('en-IN') }}</dd></div>
        </dl>
      </aside>
    </div>
  </div>
</template>

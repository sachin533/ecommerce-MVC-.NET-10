import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { cartService } from '@/services/api'
import type { Cart } from '@/types'

export const useCartStore = defineStore('cart', () => {
  const cart = ref<Cart | null>(null)
  const loading = ref(false)
  const lastAdded = ref<string | null>(null)

  const items = computed(() => cart.value?.items ?? [])
  const itemCount = computed(() => cart.value?.itemCount ?? 0)
  const subtotal = computed(() => cart.value?.subtotal ?? 0)
  const total = computed(() => cart.value?.total ?? 0)
  const estimatedShipping = computed(() => cart.value?.estimatedShipping ?? 0)
  const isEmpty = computed(() => items.value.length === 0)

  async function fetch() {
    loading.value = true
    try {
      cart.value = await cartService.get()
    } finally {
      loading.value = false
    }
  }

  async function add(productId: number, quantity = 1, variantId?: number | null) {
    cart.value = await cartService.addItem(productId, quantity, variantId)
    lastAdded.value = new Date().toISOString()
  }

  async function updateQuantity(id: number, quantity: number) {
    cart.value = await cartService.updateItem(id, quantity)
  }

  async function remove(id: number) {
    cart.value = await cartService.removeItem(id)
  }

  async function clear() {
    cart.value = await cartService.clear()
  }

  /** Called after sign-in merge completes. */
  function setCart(next: Cart) {
    cart.value = next
  }

  return { cart, loading, lastAdded, items, itemCount, subtotal, total, estimatedShipping, isEmpty, fetch, add, updateQuantity, remove, clear, setCart }
})

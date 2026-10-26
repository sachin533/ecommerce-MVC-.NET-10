import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { wishlistService } from '@/services/api'
import { useAuthStore } from './auth'
import type { ProductSummary } from '@/types'

export const useWishlistStore = defineStore('wishlist', () => {
  const items = ref<ProductSummary[]>([])
  const loaded = ref(false)

  const count = computed(() => items.value.length)

  function has(productId: number): boolean {
    return items.value.some((p) => p.id === productId)
  }

  async function fetch(force = false) {
    const auth = useAuthStore()
    if (!auth.isAuthenticated) {
      items.value = []
      loaded.value = true
      return
    }
    if (loaded.value && !force) return
    items.value = await wishlistService.get()
    loaded.value = true
  }

  async function toggle(product: ProductSummary) {
    if (has(product.id)) {
      items.value = await wishlistService.remove(product.id)
    } else {
      items.value = await wishlistService.add(product.id)
    }
  }

  async function remove(productId: number) {
    items.value = await wishlistService.remove(productId)
  }

  function reset() {
    items.value = []
    loaded.value = false
  }

  return { items, loaded, count, has, fetch, toggle, remove, reset }
})

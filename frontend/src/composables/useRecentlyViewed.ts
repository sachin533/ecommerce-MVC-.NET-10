import { computed, ref } from 'vue'
import type { ProductSummary } from '@/types'

/**
 * Recently viewed products, persisted per-browser in localStorage.
 * Stores summary snapshots so the section renders instantly offline of the API.
 */
const STORAGE_KEY = 'quickshop.recently-viewed.v1'
const MAX_ITEMS = 12

const items = ref<ProductSummary[]>(load())

function load(): ProductSummary[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return []
    const parsed = JSON.parse(raw)
    return Array.isArray(parsed) ? parsed.slice(0, MAX_ITEMS) : []
  } catch {
    return []
  }
}

function persist() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(items.value))
  } catch {
    /* private mode / quota — non-fatal */
  }
}

export function useRecentlyViewed() {
  /** Record a product view (moves it to the front, dedupes by id). */
  function record(product: ProductSummary) {
    const next = [product, ...items.value.filter((p) => p.id !== product.id)].slice(0, MAX_ITEMS)
    items.value = next
    persist()
  }

  /** Most recent first, excluding an optional slug (the current product). */
  function recent(count: number, excludeSlug?: string): ProductSummary[] {
    return items.value.filter((p) => p.slug !== excludeSlug).slice(0, count)
  }

  const recentProducts = computed(() => items.value)

  function clear() {
    items.value = []
    persist()
  }

  return { record, recent, recentProducts, clear }
}

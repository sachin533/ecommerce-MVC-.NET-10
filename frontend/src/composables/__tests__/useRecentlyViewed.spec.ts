import { describe, expect, it } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useRecentlyViewed } from '@/composables/useRecentlyViewed'
import { makeProduct } from '@/test/fixtures'

describe('useRecentlyViewed', () => {
  it('records products newest-first and dedupes by id', () => {
    setActivePinia(createPinia())
    const { record, recent, clear } = useRecentlyViewed()
    clear()

    record(makeProduct({ id: 1, slug: 'a' }))
    record(makeProduct({ id: 2, slug: 'b' }))
    record(makeProduct({ id: 1, slug: 'a' })) // moves back to front

    const items = recent(10)
    expect(items.map((p) => p.id)).toEqual([1, 2])
    clear()
  })

  it('excludes a slug (the current product) and persists to localStorage', () => {
    setActivePinia(createPinia())
    const { record, recent, clear } = useRecentlyViewed()
    clear()

    record(makeProduct({ id: 5, slug: 'current' }))
    record(makeProduct({ id: 6, slug: 'other' }))

    expect(recent(4, 'current').map((p) => p.slug)).toEqual(['other'])

    // A fresh load from localStorage returns the same entries.
    const raw = localStorage.getItem('quickshop.recently-viewed.v1')
    expect(raw).toContain('current')
    clear()
    expect(localStorage.getItem('quickshop.recently-viewed.v1')).toBe('[]')
  })
})

import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useToastStore } from '@/stores/toast'

describe('toast store', () => {
  let store: ReturnType<typeof useToastStore>

  beforeEach(() => {
    setActivePinia(createPinia())
    vi.useFakeTimers()
    store = useToastStore()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('pushes toasts of every kind', () => {
    store.success('done')
    store.error('boom')
    store.info('fyi')

    expect(store.toasts.map((t) => t.kind)).toEqual(['success', 'error', 'info'])
    expect(store.toasts.map((t) => t.message)).toEqual(['done', 'boom', 'fyi'])
    expect(store.toasts.every((t) => t.id > 0)).toBe(true)
  })

  it('auto-dismisses success toasts after the default 4s TTL', () => {
    store.success('done')
    expect(store.toasts).toHaveLength(1)

    vi.advanceTimersByTime(3999)
    expect(store.toasts).toHaveLength(1)

    vi.advanceTimersByTime(1)
    expect(store.toasts).toHaveLength(0)
  })

  it('keeps error toasts for the longer 6s TTL', () => {
    store.error('boom')

    vi.advanceTimersByTime(4000)
    expect(store.toasts).toHaveLength(1)

    vi.advanceTimersByTime(2000)
    expect(store.toasts).toHaveLength(0)
  })

  it('never auto-dismisses a toast pushed with ttl 0', () => {
    store.push('info', 'sticky', 0)

    vi.advanceTimersByTime(60_000)
    expect(store.toasts).toHaveLength(1)
  })

  it('dismisses immediately by id and leaves the others alone', () => {
    store.success('first')
    store.error('second')
    const firstId = store.toasts[0].id

    store.dismiss(firstId)

    expect(store.toasts).toHaveLength(1)
    expect(store.toasts[0].message).toBe('second')
    // The surviving toast's own timer still works.
    vi.advanceTimersByTime(6000)
    expect(store.toasts).toHaveLength(0)
  })
})

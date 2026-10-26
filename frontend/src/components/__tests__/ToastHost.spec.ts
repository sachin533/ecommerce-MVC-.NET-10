import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import ToastHost from '@/components/ToastHost.vue'
import { useToastStore } from '@/stores/toast'

describe('ToastHost', () => {
  let pinia: ReturnType<typeof createPinia>
  let store: ReturnType<typeof useToastStore>

  beforeEach(() => {
    pinia = createPinia()
    setActivePinia(pinia)
    store = useToastStore()
  })

  it('renders nothing but the host container when there are no toasts', () => {
    const wrapper = mount(ToastHost, { global: { plugins: [pinia] } })
    expect(wrapper.find('[data-testid="toast-host"]').exists()).toBe(true)
    expect(wrapper.findAll('[data-testid^="toast-"]').filter((w) => w.attributes('data-testid') !== 'toast-host')).toHaveLength(0)
  })

  it('renders each toast kind with its message', () => {
    store.success('Added to cart')
    store.error('Payment failed')
    store.info('Heads up')

    const wrapper = mount(ToastHost, { global: { plugins: [pinia] } })

    expect(wrapper.get('[data-testid="toast-success"]').text()).toContain('Added to cart')
    expect(wrapper.get('[data-testid="toast-error"]').text()).toContain('Payment failed')
    expect(wrapper.get('[data-testid="toast-info"]').text()).toContain('Heads up')
  })

  it('is announced politely to screen readers', () => {
    const wrapper = mount(ToastHost, { global: { plugins: [pinia] } })
    expect(wrapper.get('[data-testid="toast-host"]').attributes('aria-live')).toBe('polite')
  })

  it('removes a toast when its dismiss button is clicked', async () => {
    store.success('Saved')
    const wrapper = mount(ToastHost, { global: { plugins: [pinia] } })
    expect(wrapper.findAll('[data-testid="toast-success"]')).toHaveLength(1)

    await wrapper.get('[aria-label="Dismiss"]').trigger('click')

    // Store updates synchronously...
    expect(store.toasts).toHaveLength(0)
    // ...and the element leaves the DOM once the exit transition finishes.
    await vi.waitFor(() => {
      expect(wrapper.find('[data-testid="toast-success"]').exists()).toBe(false)
    })
  })
})

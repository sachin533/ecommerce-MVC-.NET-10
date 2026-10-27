import { mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import HeroCarousel from '@/components/HeroCarousel.vue'

const pushMock = vi.hoisted(() => vi.fn())
vi.mock('vue-router', () => ({ useRouter: () => ({ push: pushMock }) }))

function matchMediaStub(reduced: boolean) {
  window.matchMedia = vi.fn().mockImplementation((query: string) => ({
    matches: reduced && query.includes('prefers-reduced-motion'),
    media: query,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
  }))
}

describe('HeroCarousel', () => {
  it('renders the first slide with shop-now and indicator controls', () => {
    setActivePinia(createPinia())
    matchMediaStub(true)
    const wrapper = mount(HeroCarousel, { attachTo: document.body })

    expect(wrapper.find('[data-testid="hero-carousel"]').exists()).toBe(true)
    expect(wrapper.get('[data-testid="hero-shop-now"]').text()).toBe('Shop Now')
    expect(wrapper.findAll('[data-testid="hero-slide"]').length).toBe(3)
    expect(wrapper.findAll('[role="tab"]').length).toBe(3)
    wrapper.unmount()
  })

  it('advances slides through the next control and indicator clicks', async () => {
    setActivePinia(createPinia())
    matchMediaStub(true)
    const wrapper = mount(HeroCarousel, { attachTo: document.body })

    await wrapper.get('[data-testid="hero-next"]').trigger('click')
    const live = wrapper.find('p[aria-live="polite"]')
    expect(live.text()).toContain('Slide 2 of 3')

    await wrapper.findAll('[role="tab"]')[2].trigger('click')
    await wrapper.vm.$nextTick()
    expect(wrapper.find('p[aria-live="polite"]').text()).toContain('Slide 3 of 3')

    // Wraps around back to slide 1.
    await wrapper.get('[data-testid="hero-next"]').trigger('click')
    expect(wrapper.find('p[aria-live="polite"]').text()).toContain('Slide 1 of 3')
    wrapper.unmount()
  })

  it('sends users to the product page from Shop Now', async () => {
    setActivePinia(createPinia())
    matchMediaStub(true)
    const wrapper = mount(HeroCarousel, { attachTo: document.body })

    await wrapper.get('[data-testid="hero-shop-now"]').trigger('click')
    expect(pushMock).toHaveBeenCalledWith({
      name: 'product',
      params: { slug: 'apple-macbook-air-m3' },
    })
    wrapper.unmount()
  })
})

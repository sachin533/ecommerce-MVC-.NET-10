import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import PromoTicker from '@/components/PromoTicker.vue'

describe('PromoTicker', () => {
  setActivePinia(createPinia())

  it('renders every promotional message twice for the seamless loop', () => {
    const wrapper = mount(PromoTicker)
    const text = wrapper.text()

    expect(text).toContain('FESTIVE SALE — UP TO 50% OFF')
    expect(text).toContain('FREE DELIVERY ON ORDERS OVER ₹999')
    expect(text).toContain('SECURE CHECKOUT')

    // Duplicated track: each message appears exactly twice.
    const occurrences = text.split('FESTIVE SALE').length - 1
    expect(occurrences).toBe(2)
  })

  it('exposes the marquee testid and region semantics', () => {
    const wrapper = mount(PromoTicker)
    expect(wrapper.get('[data-testid="promo-ticker"]').attributes('role')).toBe('region')
    // Second track is hidden from assistive tech.
    const hidden = wrapper.findAll('[aria-hidden="true"]').length
    expect(hidden).toBeGreaterThan(0)
  })
})

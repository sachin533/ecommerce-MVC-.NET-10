import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import PriceDisplay from '@/components/PriceDisplay.vue'

describe('PriceDisplay', () => {
  it('formats the sale price as Indian rupees', () => {
    const wrapper = mount(PriceDisplay, { props: { price: 4999 } })
    expect(wrapper.get('[data-testid="sale-price"]').text()).toContain('4,999')
    expect(wrapper.get('[data-testid="sale-price"]').text()).toContain('₹')
  })

  it('groups large amounts (lakhs) with the en-IN convention', () => {
    const wrapper = mount(PriceDisplay, { props: { price: 129999 } })
    expect(wrapper.get('[data-testid="sale-price"]').text()).toContain('1,29,999')
  })

  it('shows the MRP struck through only when it is above the sale price', async () => {
    const wrapper = mount(PriceDisplay, { props: { price: 4999, mrp: 5999 } })
    expect(wrapper.find('.line-through').exists()).toBe(true)
    expect(wrapper.find('.line-through').text()).toContain('5,999')

    // MRP equal to the sale price is not a discount — hide it.
    await wrapper.setProps({ price: 5999, mrp: 5999 })
    expect(wrapper.find('.line-through').exists()).toBe(false)
  })

  it('hides the MRP entirely when it is not provided', () => {
    const wrapper = mount(PriceDisplay, { props: { price: 4999 } })
    expect(wrapper.find('.line-through').exists()).toBe(false)
  })

  it('renders a rounded discount badge', () => {
    const wrapper = mount(PriceDisplay, {
      props: { price: 899, mrp: 1000, discountPercent: 10.4 },
    })
    expect(wrapper.get('[data-testid="discount-badge"]').text()).toBe('10% off')
  })

  it('rounds up fractional discounts', () => {
    const wrapper = mount(PriceDisplay, {
      props: { price: 449, mrp: 599, discountPercent: 25.04 },
    })
    expect(wrapper.get('[data-testid="discount-badge"]').text()).toBe('25% off')
  })

  it('omits the discount badge when there is no discount', () => {
    const wrapper = mount(PriceDisplay, { props: { price: 1000, discountPercent: 0 } })
    expect(wrapper.find('[data-testid="discount-badge"]').exists()).toBe(false)
  })

  it('uses the larger type size when asked', () => {
    const wrapper = mount(PriceDisplay, { props: { price: 100, size: 'lg' } })
    expect(wrapper.get('[data-testid="sale-price"]').classes()).toContain('text-2xl')
  })
})

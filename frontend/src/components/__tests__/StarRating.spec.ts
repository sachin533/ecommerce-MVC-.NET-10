import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import StarRating from '@/components/StarRating.vue'

describe('StarRating', () => {
  it('exposes an accessible label with the rating', () => {
    const wrapper = mount(StarRating, { props: { rating: 4.3 } })
    expect(wrapper.get('[data-testid="rating"]').attributes('aria-label')).toBe('Rated 4.3 out of 5')
  })

  it('always shows the rating to one decimal place', () => {
    const wrapper = mount(StarRating, { props: { rating: 4 } })
    expect(wrapper.text()).toContain('4.0')
  })

  it('shows the review count when provided', () => {
    const wrapper = mount(StarRating, { props: { rating: 4.5, count: 128 } })
    expect(wrapper.text()).toContain('(128)')
  })

  it('omits the count when it is not provided', () => {
    const wrapper = mount(StarRating, { props: { rating: 4.5 } })
    expect(wrapper.text()).not.toContain('(')
  })

  it('fills the stars proportionally to the rating', () => {
    const wrapper = mount(StarRating, { props: { rating: 4 } })
    const fill = wrapper.find('[data-testid="rating"] > div > div:nth-child(2)')
    expect(fill.attributes('style')).toContain('width: 80%')
  })

  it('renders five empty and five filled star glyphs', () => {
    const wrapper = mount(StarRating, { props: { rating: 3.2 } })
    expect(wrapper.findAll('svg')).toHaveLength(10)
  })
})

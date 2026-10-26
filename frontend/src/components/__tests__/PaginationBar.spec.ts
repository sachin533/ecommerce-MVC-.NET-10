import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import PaginationBar from '@/components/PaginationBar.vue'

function bar(page: number, totalPages: number, totalCount = totalPages * 20) {
  return mount(PaginationBar, { props: { page, totalPages, totalCount } })
}

describe('PaginationBar', () => {
  it('renders nothing when there is a single page', () => {
    const wrapper = bar(1, 1, 8)
    expect(wrapper.find('nav').exists()).toBe(false)
    expect(wrapper.html()).toBe('<!--v-if-->')
  })

  it('disables Prev on the first page and Next on the last', () => {
    const first = bar(1, 5)
    expect(first.get('[data-testid="pagination-prev"]').attributes('disabled')).toBeDefined()
    expect(first.get('[data-testid="pagination-next"]').attributes('disabled')).toBeUndefined()

    const last = bar(5, 5)
    expect(last.get('[data-testid="pagination-prev"]').attributes('disabled')).toBeUndefined()
    expect(last.get('[data-testid="pagination-next"]').attributes('disabled')).toBeDefined()
  })

  it('emits change with the neighbouring page', async () => {
    const wrapper = bar(3, 6)
    await wrapper.get('[data-testid="pagination-prev"]').trigger('click')
    await wrapper.get('[data-testid="pagination-next"]').trigger('click')
    expect(wrapper.emitted('change')).toEqual([[2], [4]])
  })

  it('emits change when a page number is clicked', async () => {
    const wrapper = bar(3, 6)
    await wrapper.get('[data-testid="pagination-page-5"]').trigger('click')
    expect(wrapper.emitted('change')).toEqual([[5]])
  })

  it('shows a window of +/- 2 pages around the current page', () => {
    const wrapper = bar(5, 10)
    for (const p of [3, 4, 5, 6, 7]) {
      expect(wrapper.find(`[data-testid="pagination-page-${p}"]`).exists()).toBe(true)
    }
    expect(wrapper.find('[data-testid="pagination-page-8"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="pagination-page-2"]').exists()).toBe(false)
  })

  it('clamps the window at the edges', () => {
    const start = bar(1, 10)
    expect(start.findAll('[data-testid^="pagination-page-"]').map((b) => b.text())).toEqual([
      '1',
      '2',
      '3',
    ])

    const end = bar(10, 10)
    expect(end.findAll('[data-testid^="pagination-page-"]').map((b) => b.text())).toEqual([
      '8',
      '9',
      '10',
    ])
  })

  it('marks the current page as active', () => {
    const wrapper = bar(4, 8)
    expect(wrapper.get('[data-testid="pagination-page-4"]').classes()).toContain('bg-brand-600')
    expect(wrapper.get('[data-testid="pagination-page-3"]').classes()).not.toContain('bg-brand-600')
  })
})

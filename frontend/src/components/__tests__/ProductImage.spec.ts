import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import ProductImage from '@/components/ProductImage.vue'

describe('ProductImage', () => {
  it('renders the image when a source is given', () => {
    const wrapper = mount(ProductImage, {
      props: { src: 'https://cdn.example.com/p1.jpg', alt: 'A product' },
    })
    const img = wrapper.get('img')
    expect(img.attributes('src')).toBe('https://cdn.example.com/p1.jpg')
    expect(img.attributes('alt')).toBe('A product')
    expect(wrapper.find('[data-testid="image-fallback"]').exists()).toBe(false)
  })

  it('falls back to the placeholder when the source is empty', () => {
    const wrapper = mount(ProductImage, { props: { src: '', alt: 'Missing image' } })
    const fallback = wrapper.get('[data-testid="image-fallback"]')
    expect(wrapper.find('img').exists()).toBe(false)
    expect(fallback.attributes('aria-label')).toBe('Missing image')
    expect(fallback.attributes('role')).toBe('img')
  })

  it('switches to the placeholder when the image fails to load', async () => {
    const wrapper = mount(ProductImage, {
      props: { src: 'https://cdn.example.com/broken.jpg', alt: 'Broken' },
    })
    await wrapper.get('img').trigger('error')
    expect(wrapper.find('img').exists()).toBe(false)
    expect(wrapper.find('[data-testid="image-fallback"]').exists()).toBe(true)
  })

  it('recovers when the source changes after a failure', async () => {
    const wrapper = mount(ProductImage, {
      props: { src: 'https://cdn.example.com/broken.jpg', alt: 'Broken' },
    })
    await wrapper.get('img').trigger('error')
    expect(wrapper.find('[data-testid="image-fallback"]').exists()).toBe(true)

    await wrapper.setProps({ src: 'https://cdn.example.com/good.jpg' })
    const img = wrapper.get('img')
    expect(img.attributes('src')).toBe('https://cdn.example.com/good.jpg')
  })

  it('applies the caller-supplied image classes', () => {
    const wrapper = mount(ProductImage, {
      props: { src: 'https://cdn.example.com/p1.jpg', alt: 'A', imgClass: 'h-24 w-24' },
    })
    expect(wrapper.get('img').classes()).toContain('h-24')
  })
})

import { describe, expect, it, vi } from 'vitest'
import { measureStickyOffset, scrollHunkIntoView } from './scroll-hunk'

describe('scrollHunkIntoView', () => {
  it('scrolls the container to center the target element', () => {
    const scrollTo = vi.fn()
    const container = {
      clientHeight: 400,
      scrollTop: 0,
      getBoundingClientRect: () => ({ top: 100 }),
      scrollTo,
    } as unknown as HTMLElement

    const element = {
      offsetHeight: 20,
      getBoundingClientRect: () => ({ top: 500 }),
    } as unknown as HTMLElement

    scrollHunkIntoView(container, element)

    expect(scrollTo).toHaveBeenCalledWith({
      top: 210,
      behavior: 'smooth',
    })
  })

  it('accounts for sticky header offset', () => {
    const scrollTo = vi.fn()
    const container = {
      clientHeight: 400,
      scrollTop: 0,
      getBoundingClientRect: () => ({ top: 100 }),
      scrollTo,
    } as unknown as HTMLElement

    const element = {
      offsetHeight: 20,
      getBoundingClientRect: () => ({ top: 500 }),
    } as unknown as HTMLElement

    scrollHunkIntoView(container, element, 32)

    expect(scrollTo).toHaveBeenCalledWith({
      top: 194,
      behavior: 'smooth',
    })
  })
})

describe('measureStickyOffset', () => {
  it('returns sticky header height when present', () => {
    const header = document.createElement('div')
    Object.defineProperty(header, 'offsetHeight', { value: 28 })

    const container = document.createElement('div')
    header.setAttribute('data-diff-sticky-header', '')
    container.appendChild(header)

    expect(measureStickyOffset(container)).toBe(28)
  })

  it('returns 0 when sticky header is absent', () => {
    const container = document.createElement('div')
    expect(measureStickyOffset(container)).toBe(0)
  })
})
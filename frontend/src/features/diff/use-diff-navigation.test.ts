import { act, renderHook } from '@testing-library/react'
import { createRef, type RefObject } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useDiffNavigation } from './use-diff-navigation'

describe('useDiffNavigation', () => {
  beforeEach(() => {
    Element.prototype.scrollIntoView = vi.fn()
  })

  it('0 hunks → navigation disabled', () => {
    const { result } = renderHook(() => useDiffNavigation([], true))
    expect(result.current.totalHunks).toBe(0)
    expect(result.current.canGoNext).toBe(false)
    expect(result.current.canGoPrevious).toBe(false)
  })

  it('advances and retreats within bounds', () => {
    const hunks = [0, 5, 10]
    const { result } = renderHook(() => useDiffNavigation(hunks, true))

    expect(result.current.activeIndex).toBe(0)
    expect(result.current.canGoPrevious).toBe(false)
    expect(result.current.canGoNext).toBe(true)

    act(() => result.current.goToNext())
    expect(result.current.activeIndex).toBe(1)
    expect(result.current.canGoPrevious).toBe(true)

    act(() => result.current.goToNext())
    expect(result.current.activeIndex).toBe(2)
    expect(result.current.canGoNext).toBe(false)

    act(() => result.current.goToNext())
    expect(result.current.activeIndex).toBe(2)

    act(() => result.current.goToPrevious())
    expect(result.current.activeIndex).toBe(1)
  })

  it('resets index when changeHunks changes', () => {
    const { result, rerender } = renderHook(
      ({ hunks }) => useDiffNavigation(hunks, true),
      { initialProps: { hunks: [0, 3] } },
    )

    act(() => result.current.goToNext())
    expect(result.current.activeIndex).toBe(1)

    rerender({ hunks: [1, 7, 12] })
    expect(result.current.activeIndex).toBe(0)
    expect(result.current.totalHunks).toBe(3)
  })

  it('focusActive scrolls via the scroll container', () => {
    const scrollTo = vi.fn()
    const container = document.createElement('div')
    Object.defineProperty(container, 'clientHeight', { value: 400 })
    Object.defineProperty(container, 'scrollTop', { value: 0, writable: true })
    container.scrollTo = scrollTo
    container.getBoundingClientRect = () => ({ top: 100 }) as DOMRect

    const hunk = document.createElement('div')
    Object.defineProperty(hunk, 'offsetHeight', { value: 20 })
    hunk.getBoundingClientRect = () => ({ top: 500 }) as DOMRect

    const scrollContainerRef = createRef<HTMLDivElement>()
    ;(scrollContainerRef as RefObject<HTMLDivElement>).current = container

    const hunks = [4]
    const { result } = renderHook(() => useDiffNavigation(hunks, true, scrollContainerRef))

    act(() => result.current.registerHunkRef(0, hunk))
    scrollTo.mockClear()
    act(() => result.current.focusActive())

    expect(result.current.activeIndex).toBe(0)
    expect(scrollTo).toHaveBeenCalledWith({
      top: 210,
      behavior: 'smooth',
    })
  })
})
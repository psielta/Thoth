import { useCallback, useEffect, useRef, useState, type RefObject } from 'react'
import { measureStickyOffset, scrollHunkIntoView } from './scroll-hunk'

export function useDiffNavigation(
  changeHunks: number[],
  enabled: boolean,
  scrollContainerRef?: RefObject<HTMLElement | null>,
) {
  const changeHunksKey = changeHunks.join(',')
  const [prevHunksKey, setPrevHunksKey] = useState(changeHunksKey)
  const [activeIndex, setActiveIndex] = useState(0)
  const hunkRefs = useRef<Map<number, HTMLElement>>(new Map())

  if (changeHunksKey !== prevHunksKey) {
    setPrevHunksKey(changeHunksKey)
    setActiveIndex(0)
  }

  const totalHunks = changeHunks.length
  const canGoNext = enabled && activeIndex < totalHunks - 1
  const canGoPrevious = enabled && activeIndex > 0

  const registerHunkRef = useCallback((hunkIndex: number, el: HTMLElement | null) => {
    if (el) {
      hunkRefs.current.set(hunkIndex, el)
    } else {
      hunkRefs.current.delete(hunkIndex)
    }
  }, [])

  const scrollToHunk = useCallback(
    (index: number) => {
      const el = hunkRefs.current.get(index)
      if (!el) return

      const container = scrollContainerRef?.current
      if (container) {
        const stickyOffset = measureStickyOffset(container)
        scrollHunkIntoView(container, el, stickyOffset)
        return
      }

      el.scrollIntoView?.({ behavior: 'smooth', block: 'center' })
    },
    [scrollContainerRef],
  )

  useEffect(() => {
    if (!enabled || totalHunks === 0) return
    scrollToHunk(activeIndex)
  }, [activeIndex, enabled, totalHunks, scrollToHunk])

  const goToNext = useCallback(() => {
    setActiveIndex((i) => Math.min(i + 1, totalHunks - 1))
  }, [totalHunks])

  const goToPrevious = useCallback(() => {
    setActiveIndex((i) => Math.max(i - 1, 0))
  }, [])

  const focusActive = useCallback(() => {
    scrollToHunk(activeIndex)
  }, [activeIndex, scrollToHunk])

  return {
    activeIndex,
    totalHunks,
    canGoNext,
    canGoPrevious,
    goToNext,
    goToPrevious,
    focusActive,
    registerHunkRef,
  }
}
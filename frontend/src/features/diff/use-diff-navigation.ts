import { useCallback, useEffect, useRef, useState } from 'react'

export function useDiffNavigation(changeHunks: number[], enabled: boolean) {
  const changeHunksKey = changeHunks.join(',')
  const [prevHunksKey, setPrevHunksKey] = useState(changeHunksKey)
  const [activeIndex, setActiveIndex] = useState(0)
  const hunkRefs = useRef<Map<number, HTMLElement>>(new Map())

  if (changeHunksKey !== prevHunksKey) {
    setPrevHunksKey(changeHunksKey)
    setActiveIndex(0)
  }

  useEffect(() => {
    hunkRefs.current.clear()
  }, [changeHunksKey])

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

  useEffect(() => {
    if (!enabled || totalHunks === 0) return
    const el = hunkRefs.current.get(activeIndex)
    el?.scrollIntoView?.({ behavior: 'smooth', block: 'center' })
  }, [activeIndex, enabled, totalHunks])

  const goToNext = useCallback(() => {
    setActiveIndex((i) => Math.min(i + 1, totalHunks - 1))
  }, [totalHunks])

  const goToPrevious = useCallback(() => {
    setActiveIndex((i) => Math.max(i - 1, 0))
  }, [])

  return {
    activeIndex,
    totalHunks,
    canGoNext,
    canGoPrevious,
    goToNext,
    goToPrevious,
    registerHunkRef,
  }
}
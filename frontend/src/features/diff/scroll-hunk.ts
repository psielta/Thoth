export function scrollHunkIntoView(
  container: HTMLElement,
  element: HTMLElement,
  stickyOffset = 0,
) {
  const containerRect = container.getBoundingClientRect()
  const elementRect = element.getBoundingClientRect()
  const elementTop = elementRect.top - containerRect.top + container.scrollTop
  const viewportHeight = container.clientHeight - stickyOffset
  const targetScrollTop = elementTop - stickyOffset - (viewportHeight - element.offsetHeight) / 2

  container.scrollTo({
    top: Math.max(0, targetScrollTop),
    behavior: 'smooth',
  })
}

export function measureStickyOffset(container: HTMLElement): number {
  const sticky = container.querySelector<HTMLElement>('[data-diff-sticky-header]')
  return sticky?.offsetHeight ?? 0
}
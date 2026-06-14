import * as matchers from '@testing-library/jest-dom/matchers'
import { expect } from 'vitest'

expect.extend(matchers)

Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => undefined,
    removeListener: () => undefined,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
    dispatchEvent: () => false,
  }),
})

Object.defineProperty(document, 'queryCommandSupported', {
  writable: true,
  value: () => false,
})

Object.defineProperty(window.navigator, 'clipboard', {
  configurable: true,
  writable: true,
  value: {
    readText: () => Promise.resolve(''),
    writeText: () => Promise.resolve(),
    write: () => Promise.resolve(),
  },
})

if (typeof window.ClipboardItem === 'undefined') {
  class ClipboardItemMock {
    readonly data: Record<string, Blob | Promise<Blob>>

    constructor(data: Record<string, Blob | Promise<Blob>>) {
      this.data = data
    }
  }

  Object.defineProperty(window, 'ClipboardItem', {
    configurable: true,
    writable: true,
    value: ClipboardItemMock,
  })
}

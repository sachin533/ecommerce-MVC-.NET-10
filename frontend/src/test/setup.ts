/**
 * Vitest setup: jsdom in this project exposes `localStorage` as an empty
 * object without methods. Provide a minimal in-memory Storage implementation
 * so stores/composables that persist state can be tested for real.
 */
class MemoryStorage implements Storage {
  private map = new Map<string, string>()

  get length() {
    return this.map.size
  }
  key(index: number): string | null {
    return [...this.map.keys()][index] ?? null
  }
  getItem(key: string): string | null {
    return this.map.has(key) ? this.map.get(key)! : null
  }
  setItem(key: string, value: string): void {
    this.map.set(key, String(value))
  }
  removeItem(key: string): void {
    this.map.delete(key)
  }
  clear(): void {
    this.map.clear()
  }
}

const target = globalThis as { localStorage?: Storage }
if (typeof target.localStorage?.getItem !== 'function') {
  target.localStorage = new MemoryStorage()
}

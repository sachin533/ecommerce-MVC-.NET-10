import { defineStore } from 'pinia'
import { ref } from 'vue'

export interface Toast {
  id: number
  kind: 'success' | 'error' | 'info'
  message: string
}

let nextId = 1

export const useToastStore = defineStore('toast', () => {
  const toasts = ref<Toast[]>([])

  function push(kind: Toast['kind'], message: string, ttlMs = 4000) {
    const id = nextId++
    toasts.value.push({ id, kind, message })
    if (ttlMs > 0) {
      setTimeout(() => dismiss(id), ttlMs)
    }
  }

  function success(message: string) {
    push('success', message)
  }

  function error(message: string, ttlMs = 6000) {
    push('error', message, ttlMs)
  }

  function info(message: string) {
    push('info', message)
  }

  function dismiss(id: number) {
    toasts.value = toasts.value.filter((t) => t.id !== id)
  }

  return { toasts, push, success, error, info, dismiss }
})

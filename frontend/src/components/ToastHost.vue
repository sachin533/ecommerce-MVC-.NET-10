<script setup lang="ts">
import { useToastStore } from '@/stores/toast'

const toasts = useToastStore()
</script>

<template>
  <div class="pointer-events-none fixed bottom-4 right-4 z-50 flex w-full max-w-sm flex-col gap-2" data-testid="toast-host" aria-live="polite">
    <transition-group name="toast">
      <div
        v-for="toast in toasts.toasts"
        :key="toast.id"
        class="pointer-events-auto flex items-start gap-3 rounded-xl border p-3 shadow-lg"
        :class="{
          'border-emerald-200 bg-emerald-50 text-emerald-800': toast.kind === 'success',
          'border-rose-200 bg-rose-50 text-rose-800': toast.kind === 'error',
          'border-sky-200 bg-sky-50 text-sky-800': toast.kind === 'info',
        }"
        :data-testid="`toast-${toast.kind}`"
      >
        <p class="flex-1 text-sm font-medium">{{ toast.message }}</p>
        <button class="shrink-0 opacity-60 hover:opacity-100" aria-label="Dismiss" @click="toasts.dismiss(toast.id)">
          <svg viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4"><path d="M6.3 4.9 4.9 6.3 8.6 10l-3.7 3.7 1.4 1.4L10 11.4l3.7 3.7 1.4-1.4L11.4 10l3.7-3.7-1.4-1.4L10 8.6 6.3 4.9Z" /></svg>
        </button>
      </div>
    </transition-group>
  </div>
</template>

<style scoped>
.toast-enter-active,
.toast-leave-active {
  transition: all 0.25s ease;
}
.toast-enter-from,
.toast-leave-to {
  opacity: 0;
  transform: translateY(8px);
}
</style>

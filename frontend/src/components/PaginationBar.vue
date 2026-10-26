<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  page: number
  totalPages: number
  totalCount: number
}>()

const emit = defineEmits<{ change: [page: number] }>()

const pages = computed<number[]>(() => {
  const result: number[] = []
  const start = Math.max(1, props.page - 2)
  const end = Math.min(props.totalPages, props.page + 2)
  for (let i = start; i <= end; i++) result.push(i)
  return result
})
</script>

<template>
  <nav v-if="totalPages > 1" class="flex items-center justify-between gap-2 pt-6" aria-label="Pagination">
    <button
      class="btn-secondary"
      :disabled="page <= 1"
      data-testid="pagination-prev"
      @click="emit('change', page - 1)"
    >
      ← Prev
    </button>

    <div class="flex items-center gap-1">
      <button
        v-for="p in pages"
        :key="p"
        class="h-9 min-w-9 rounded-lg px-2 text-sm font-medium"
        :class="p === page ? 'bg-brand-600 text-white' : 'text-slate-600 hover:bg-slate-100'"
        :aria-current="p === page ? 'page' : undefined"
        :data-testid="`pagination-page-${p}`"
        @click="emit('change', p)"
      >
        {{ p }}
      </button>
    </div>

    <button
      class="btn-secondary"
      :disabled="page >= totalPages"
      data-testid="pagination-next"
      @click="emit('change', page + 1)"
    >
      Next →
    </button>
  </nav>
</template>

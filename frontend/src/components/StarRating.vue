<script setup lang="ts">
defineProps<{
  rating: number
  count?: number
}>()
</script>

<template>
  <div class="flex items-center gap-1" :aria-label="`Rated ${rating} out of 5`" data-testid="rating">
    <div class="relative flex">
      <div class="flex text-slate-300">
        <svg v-for="i in 5" :key="`o${i}`" viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4"><path d="M10 1.5l2.6 5.3 5.9.9-4.2 4.1 1 5.8L10 14.9l-5.3 2.7 1-5.8L1.5 7.7l5.9-.9L10 1.5Z" /></svg>
      </div>
      <div class="absolute inset-0 flex overflow-hidden text-amber-400" :style="{ width: `${(rating / 5) * 100}%` }">
        <svg v-for="i in 5" :key="`f${i}`" viewBox="0 0 20 20" fill="currentColor" class="h-4 w-4 shrink-0"><path d="M10 1.5l2.6 5.3 5.9.9-4.2 4.1 1 5.8L10 14.9l-5.3 2.7 1-5.8L1.5 7.7l5.9-.9L10 1.5Z" /></svg>
      </div>
    </div>
    <span class="text-xs font-medium text-slate-600">{{ rating.toFixed(1) }}</span>
    <span v-if="count != null" class="text-xs text-slate-400">({{ count }})</span>
  </div>
</template>

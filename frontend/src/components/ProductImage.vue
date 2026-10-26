<script setup lang="ts">
import { ref, watch } from 'vue'

const props = defineProps<{
  src: string
  alt: string
  /** css class for the <img> */
  imgClass?: string
}>()

const failed = ref(false)

// Reset when the source changes (e.g. next product in a carousel).
watch(
  () => props.src,
  () => {
    failed.value = false
  },
)
</script>

<template>
  <img
    v-if="src && !failed"
    :src="src"
    :alt="alt"
    loading="lazy"
    :class="imgClass ?? 'h-full w-full object-cover'"
    @error="failed = true"
  />
  <!-- Local SVG placeholder when the image URL is missing or fails to load. -->
  <div
    v-else
    class="flex h-full w-full items-center justify-center bg-gradient-to-br from-slate-100 to-slate-200 text-slate-400"
    role="img"
    :aria-label="alt"
    data-testid="image-fallback"
  >
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="h-12 w-12">
      <rect x="3" y="3" width="18" height="18" rx="2" />
      <circle cx="8.5" cy="8.5" r="1.5" />
      <path d="m21 15-5-5L5 21" stroke-linecap="round" stroke-linejoin="round" />
    </svg>
  </div>
</template>

<script setup lang="ts">
/**
 * Full-width black promotional ticker with a seamless white-text marquee.
 * - Duplicated content track for a gap-free loop (translateX -50%).
 * - Pauses on hover/focus; respects prefers-reduced-motion (see main.css).
 */
const messages = [
  { text: 'FESTIVE SALE — UP TO 50% OFF', icon: 'M12 3l2.5 5 5.5.8-4 3.9.9 5.5L12 15.5 7.1 18.2l.9-5.5-4-3.9L9.5 8 12 3z' },
  { text: 'FREE DELIVERY ON ORDERS OVER ₹999', icon: 'M3 7h11v8H3zM14 10h4l3 3v2h-7zM6.5 18a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3zM17.5 18a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3z' },
  { text: 'EXTRA DISCOUNTS ON SELECTED PRODUCTS', icon: 'M20.6 13.4L13.4 20.6a2 2 0 0 1-2.8 0L3 13V3h10l7.6 7.6a2 2 0 0 1 0 2.8zM7.5 7.5h.01' },
  { text: 'NEW DEALS EVERY DAY', icon: 'M8 2v3M16 2v3M3.5 9h17M5 5h14a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2z' },
  { text: 'EASY 7-DAY RETURNS', icon: 'M3 12a9 9 0 1 0 3-6.7L3 8M3 3v5h5' },
  { text: 'SECURE CHECKOUT', icon: 'M12 2l8 4v6c0 5-3.5 8.5-8 10-4.5-1.5-8-5-8-10V6l8-4zM9 12l2 2 4-4' },
]
</script>

<template>
  <div
    class="qs-marquee-wrap overflow-hidden bg-black text-white"
    role="region"
    aria-label="Store promotions"
    data-testid="promo-ticker"
  >
    <div class="qs-marquee flex w-max items-center gap-10 py-2 pl-10 text-xs font-semibold tracking-wide uppercase sm:text-sm">
      <!-- Two identical tracks make the -50% translation seamless -->
      <template v-for="track in 2" :key="track">
        <span
          v-for="message in messages"
          :key="`${track}-${message.text}`"
          class="flex shrink-0 items-center gap-2 whitespace-nowrap"
          :aria-hidden="track === 2"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" class="h-3.5 w-3.5 text-amber-300 sm:h-4 sm:w-4" aria-hidden="true">
            <path :d="message.icon" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
          {{ message.text }}
          <span class="ml-8 text-white/30" aria-hidden="true">•</span>
        </span>
      </template>
    </div>
  </div>
</template>

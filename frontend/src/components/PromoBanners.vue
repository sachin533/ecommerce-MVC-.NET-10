<script setup lang="ts">
import { useRouter } from 'vue-router'

/**
 * Three promotional banners that deep-link into real categories.
 * Product photography uses object-contain so nothing is stretched;
 * the gradient backdrop supplies the lifestyle feel.
 */
const router = useRouter()

const banners = [
  {
    title: 'OnePlus 12',
    subtitle: 'Snapdragon 8 Gen 3 · Hasselblad camera',
    badge: 'From ₹64,999',
    image: '/images/products/oneplus-12/1.png',
    imageAlt: 'OnePlus 12 smartphone',
    bg: 'from-emerald-950 via-emerald-900 to-slate-900',
    to: { name: 'category', params: { slug: 'mobiles' } },
    testid: 'promo-banner-mobiles',
  },
  {
    title: 'Big-screen TVs',
    subtitle: 'Neo QLED & OLED 4K panels',
    badge: 'Up to 40% off',
    image: '/images/products/samsung-neo-qled-55/1.jpg',
    imageAlt: 'Samsung Neo QLED 4K television',
    bg: 'from-blue-950 via-blue-900 to-slate-900',
    to: { name: 'category', params: { slug: 'tvs' } },
    testid: 'promo-banner-tvs',
  },
  {
    title: 'Studio sound',
    subtitle: 'Sony WH-1000XM5 headphones',
    badge: 'Best seller',
    image: '/images/products/sony-wh-1000xm5/1.jpg',
    imageAlt: 'Sony WH-1000XM5 headphones',
    bg: 'from-violet-950 via-violet-900 to-slate-900',
    to: { name: 'category', params: { slug: 'headphones-audio' } },
    testid: 'promo-banner-audio',
  },
]
</script>

<template>
  <section aria-label="Promotional offers" data-testid="promo-banners">
    <div class="grid gap-4 md:grid-cols-3">
      <button
        v-for="banner in banners"
        :key="banner.title"
        class="group relative flex h-44 flex-col justify-between overflow-hidden rounded-2xl bg-gradient-to-br p-5 text-left text-white transition hover:-translate-y-0.5 hover:shadow-xl sm:h-48"
        :class="banner.bg"
        :data-testid="banner.testid"
        @click="router.push(banner.to)"
      >
        <div class="relative z-10 max-w-[60%]">
          <span class="inline-flex rounded-full bg-white/15 px-2.5 py-1 text-[11px] font-semibold backdrop-blur">{{ banner.badge }}</span>
          <h3 class="mt-3 text-xl font-bold sm:text-2xl">{{ banner.title }}</h3>
          <p class="mt-1 text-xs text-white/75 sm:text-sm">{{ banner.subtitle }}</p>
          <span class="mt-3 inline-flex items-center gap-1 text-xs font-bold text-amber-300 sm:text-sm">
            Shop now
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" class="h-3.5 w-3.5 transition group-hover:translate-x-0.5"><path d="M5 12h14M13 6l6 6-6 6" stroke-linecap="round" stroke-linejoin="round" /></svg>
          </span>
        </div>
        <img
          :src="banner.image"
          :alt="banner.imageAlt"
          class="pointer-events-none absolute -right-4 bottom-0 h-[92%] w-auto max-w-[55%] object-contain object-bottom opacity-95 drop-shadow-2xl transition group-hover:scale-105"
          loading="lazy"
          width="420"
          height="320"
        />
      </button>
    </div>
  </section>
</template>

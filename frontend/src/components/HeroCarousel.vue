<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

/**
 * Hero banner carousel: real product photography on gradient backdrops,
 * prev/next controls, clickable indicators, cross-fade transition,
 * auto-advance that pauses on hover/focus and stops for reduced-motion users.
 */
const router = useRouter()

interface Slide {
  eyebrow: string
  title: string
  description: string
  image: string
  imageAlt: string
  /** Gradient behind the product photo. */
  bg: string
  cta: { label: string; to: Record<string, unknown>; testid?: string }
  secondary: { label: string; to: Record<string, unknown> }
}

const slides: Slide[] = [
  {
    eyebrow: 'New · M3 chip',
    title: 'MacBook Air',
    description: 'Supercharged by the M3 chip in a thin, fanless design — up to 18 hours of battery and a brilliant Liquid Retina display.',
    image: '/images/products/apple-macbook-air-m3/1.jpg',
    imageAlt: 'Apple MacBook Air 13-inch with M3 chip',
    bg: 'from-slate-900 via-slate-800 to-slate-700',
    cta: { label: 'Shop Now', to: { name: 'product', params: { slug: 'apple-macbook-air-m3' } }, testid: 'hero-shop-now' },
    secondary: { label: 'Explore Laptops', to: { name: 'category', params: { slug: 'laptops' } } },
  },
  {
    eyebrow: 'Flagship phones',
    title: 'Galaxy S24 Ultra',
    description: 'Titanium frame, 200MP camera and Galaxy AI built in. Trade in your old phone for extra savings.',
    image: '/images/products/samsung-galaxy-s24-ultra/1.jpg',
    imageAlt: 'Samsung Galaxy S24 Ultra in titanium grey',
    bg: 'from-indigo-950 via-indigo-900 to-slate-800',
    cta: { label: 'Shop Now', to: { name: 'product', params: { slug: 'samsung-galaxy-s24-ultra' } } },
    secondary: { label: 'Explore Deals', to: { name: 'deals' } },
  },
  {
    eyebrow: 'Immersive audio',
    title: 'Sony WH-1000XM5',
    description: 'Industry-leading noise cancellation, crystal-clear calls and 30-hour battery in the lightest XM design yet.',
    image: '/images/products/sony-wh-1000xm5/1.jpg',
    imageAlt: 'Sony WH-1000XM5 wireless noise-cancelling headphones in black',
    bg: 'from-rose-950 via-rose-900 to-slate-900',
    cta: { label: 'Shop Now', to: { name: 'product', params: { slug: 'sony-wh-1000xm5' } } },
    secondary: { label: 'New Arrivals', to: { name: 'new-arrivals' } },
  },
]

const index = ref(0)
const paused = ref(false)
const current = computed(() => slides[index.value])

function go(next: number) {
  index.value = (next + slides.length) % slides.length
}

let timer: number | undefined
onMounted(() => {
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches
  if (reduced) return // no auto-advance for reduced-motion users
  timer = window.setInterval(() => {
    if (!paused.value) go(index.value + 1)
  }, 6000)
})
onBeforeUnmount(() => window.clearInterval(timer))
</script>

<template>
  <section
    class="relative overflow-hidden"
    aria-roledescription="carousel"
    aria-label="Featured promotions"
    data-testid="hero-carousel"
    @mouseenter="paused = true"
    @mouseleave="paused = false"
    @focusin="paused = true"
    @focusout="paused = false"
  >
    <!-- Slides -->
    <div class="relative min-h-[26rem] sm:min-h-[30rem] lg:min-h-[34rem]">
      <div
        v-for="(slide, i) in slides"
        v-show="i === index"
        :key="slide.title"
        class="absolute inset-0 bg-gradient-to-br text-white opacity-0 transition-opacity duration-700"
        :class="[slide.bg, i === index ? 'qs-slide-enter opacity-100' : '']"
        :aria-hidden="i !== index"
        data-testid="hero-slide"
      >
        <div class="mx-auto grid h-full max-w-7xl items-center gap-6 px-4 py-10 sm:px-6 lg:grid-cols-2 lg:py-14">
          <div class="relative z-10 max-w-xl">
            <p class="text-xs font-bold uppercase tracking-[0.2em] text-amber-300">{{ slide.eyebrow }}</p>
            <h1 class="mt-3 text-4xl font-extrabold leading-tight sm:text-5xl">{{ slide.title }}</h1>
            <p class="mt-4 text-sm leading-relaxed text-white/80 sm:text-base">{{ slide.description }}</p>
            <div class="mt-7 flex flex-wrap gap-3">
              <button
                class="rounded-lg bg-white px-6 py-3 text-sm font-bold text-slate-900 transition hover:bg-amber-300 hover:text-slate-900"
                :data-testid="slide.cta.testid"
                @click="router.push(slide.cta.to)"
              >
                {{ slide.cta.label }}
              </button>
              <button
                class="rounded-lg border border-white/40 px-6 py-3 text-sm font-semibold text-white transition hover:bg-white/10"
                data-testid="hero-secondary-cta"
                @click="router.push(slide.secondary.to)"
              >
                {{ slide.secondary.label }}
              </button>
            </div>
          </div>
          <!-- Product photo: contained, never stretched -->
          <div class="pointer-events-none relative hidden h-full items-center justify-center lg:flex">
            <img
              :src="slide.image"
              :alt="slide.imageAlt"
              class="max-h-[24rem] w-auto max-w-full object-contain drop-shadow-2xl xl:max-h-[28rem]"
              width="720"
              height="540"
              :loading="i === 0 ? 'eager' : 'lazy'"
              :fetchpriority="i === 0 ? 'high' : 'auto'"
            />
          </div>
        </div>
      </div>
    </div>

    <!-- Prev / next -->
    <button
      class="absolute left-3 top-1/2 z-20 -translate-y-1/2 rounded-full bg-white/15 p-2.5 text-white backdrop-blur transition hover:bg-white/30"
      aria-label="Previous slide"
      data-testid="hero-prev"
      @click="go(index - 1)"
    >
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" class="h-5 w-5"><path d="M15 5l-7 7 7 7" stroke-linecap="round" stroke-linejoin="round" /></svg>
    </button>
    <button
      class="absolute right-3 top-1/2 z-20 -translate-y-1/2 rounded-full bg-white/15 p-2.5 text-white backdrop-blur transition hover:bg-white/30"
      aria-label="Next slide"
      data-testid="hero-next"
      @click="go(index + 1)"
    >
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" class="h-5 w-5"><path d="M9 5l7 7-7 7" stroke-linecap="round" stroke-linejoin="round" /></svg>
    </button>

    <!-- Indicators -->
    <div class="absolute bottom-4 left-1/2 z-20 flex -translate-x-1/2 gap-2" role="tablist" aria-label="Slides">
      <button
        v-for="(slide, i) in slides"
        :key="`dot-${slide.title}`"
        class="h-2.5 rounded-full transition-all"
        :class="i === index ? 'w-7 bg-white' : 'w-2.5 bg-white/40 hover:bg-white/70'"
        :aria-label="`Go to slide ${i + 1}: ${slide.title}`"
        :aria-selected="i === index"
        role="tab"
        :data-testid="`hero-dot-${i}`"
        @click="go(i)"
      />
    </div>

    <!-- Current slide name for screen readers -->
    <p class="sr-only" aria-live="polite">Slide {{ index + 1 }} of {{ slides.length }}: {{ current.title }}</p>
  </section>
</template>

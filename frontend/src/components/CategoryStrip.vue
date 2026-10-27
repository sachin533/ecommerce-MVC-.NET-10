<script setup lang="ts">
import CategoryIcon from '@/components/CategoryIcon.vue'
import type { Category } from '@/types'

/** Circular category tiles — horizontal scroll on mobile, wrap on desktop. */
defineProps<{ categories: Category[] }>()
</script>

<template>
  <section aria-label="Shop by category" data-testid="category-strip">
    <div class="flex snap-x gap-4 overflow-x-auto pb-2 md:flex-wrap md:justify-center md:overflow-visible md:pb-0">
      <router-link
        v-for="category in categories"
        :key="category.slug"
        :to="{ name: 'category', params: { slug: category.slug } }"
        class="group flex w-24 shrink-0 snap-center flex-col items-center gap-2 text-center md:w-28"
        :data-testid="`category-${category.slug}`"
      >
        <span
          class="flex h-16 w-16 items-center justify-center rounded-full border border-slate-200 bg-white text-slate-700 shadow-sm transition group-hover:-translate-y-1 group-hover:border-brand-300 group-hover:text-brand-600 group-hover:shadow-md md:h-20 md:w-20"
        >
          <CategoryIcon :slug="category.slug" :label="category.name" />
        </span>
        <span class="text-xs font-semibold leading-tight text-slate-700 group-hover:text-brand-700">{{ category.name }}</span>
        <span class="text-[11px] text-slate-400">{{ category.productCount }} items</span>
      </router-link>
    </div>
  </section>
</template>

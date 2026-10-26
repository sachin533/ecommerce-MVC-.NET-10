<script setup lang="ts">
const props = defineProps<{
  price: number
  mrp?: number
  discountPercent?: number
  /** sm for dense lists, lg for the product page */
  size?: 'sm' | 'lg'
}>()

const fmt = (value: number) =>
  new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(value)

const showMrp = () => props.mrp != null && props.mrp > props.price
</script>

<template>
  <div class="flex flex-wrap items-baseline gap-2">
    <span :class="size === 'lg' ? 'text-2xl' : 'text-base'" class="font-bold text-slate-900" data-testid="sale-price">
      {{ fmt(price) }}
    </span>
    <span v-if="showMrp()" :class="size === 'lg' ? 'text-base' : 'text-xs'" class="text-slate-400 line-through">
      {{ fmt(mrp!) }}
    </span>
    <span
      v-if="discountPercent && discountPercent > 0"
      class="badge bg-emerald-100 text-emerald-700"
      data-testid="discount-badge"
    >
      {{ Math.round(discountPercent) }}% off
    </span>
  </div>
</template>

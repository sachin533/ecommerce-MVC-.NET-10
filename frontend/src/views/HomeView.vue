<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { productService, categoryService } from '@/services/api'
import ProductCard from '@/components/ProductCard.vue'
import ProductImage from '@/components/ProductImage.vue'
import type { Category, ProductSummary } from '@/types'

const featured = ref<ProductSummary[]>([])
const newest = ref<ProductSummary[]>([])
const categories = ref<Category[]>([])
const loading = ref(true)

onMounted(async () => {
  try {
    const [featuredRes, newestRes, categoriesRes] = await Promise.all([
      productService.featured(),
      productService.list({ sort: 'newest', pageSize: 8 }),
      categoryService.list(),
    ])
    featured.value = featuredRes
    newest.value = newestRes.items
    categories.value = categoriesRes.filter((c) => c.isActive).slice(0, 8)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div>
    <!-- Hero -->
    <section class="relative overflow-hidden bg-gradient-to-br from-brand-700 via-brand-600 to-sky-500 text-white">
      <div class="mx-auto grid max-w-7xl items-center gap-10 px-4 py-16 sm:px-6 lg:grid-cols-2 lg:py-24">
        <div>
          <span class="badge bg-white/15 text-white">Festive sale · up to 40% off</span>
          <h1 class="mt-4 text-4xl font-extrabold leading-tight tracking-tight sm:text-5xl">
            Everything you love,<br />delivered fast.
          </h1>
          <p class="mt-4 max-w-lg text-lg text-white/85">
            Laptops, fashion, home essentials and more — 170+ products, honest prices in ₹, and a checkout that takes under a minute.
          </p>
          <div class="mt-8 flex flex-wrap gap-3">
            <router-link :to="{ name: 'shop' }" class="btn bg-white px-6 py-3 text-brand-700 hover:bg-brand-50" data-testid="hero-shop-now">
              Shop now
            </router-link>
            <router-link :to="{ name: 'shop', query: { sort: 'price-asc' } }" class="btn border border-white/40 px-6 py-3 text-white hover:bg-white/10">
              Deals under ₹999
            </router-link>
          </div>
          <dl class="mt-10 grid max-w-md grid-cols-3 gap-6 border-t border-white/20 pt-6 text-sm">
            <div><dt class="text-white/70">Free shipping</dt><dd class="font-bold">over ₹999</dd></div>
            <div><dt class="text-white/70">Easy returns</dt><dd class="font-bold">7-day window</dd></div>
            <div><dt class="text-white/70">Secure mock pay</dt><dd class="font-bold">no card stored</dd></div>
          </dl>
        </div>
        <div class="relative hidden lg:block">
          <div class="grid grid-cols-2 gap-4">
            <ProductImage src="https://cdn.dummyjson.com/product-images/mobiles/apple-iphone-13-pro/1.webp" alt="iPhone" img-class="h-64 w-full rounded-2xl object-cover shadow-xl" />
            <ProductImage src="https://cdn.dummyjson.com/product-images/laptops/asus-f512ja-as74/1.webp" alt="Laptop" img-class="mt-8 h-64 w-full rounded-2xl object-cover shadow-xl" />
            <ProductImage src="https://cdn.dummyjson.com/product-images/mens-shoes/nike-air-max/1.webp" alt="Shoes" img-class="-mt-8 h-64 w-full rounded-2xl object-cover shadow-xl" />
            <ProductImage src="https://cdn.dummyjson.com/product-images/womens-dresses/beautiful-quran-and-rosary/1.webp" alt="Fashion" img-class="h-64 w-full rounded-2xl object-cover shadow-xl" />
          </div>
        </div>
      </div>
    </section>

    <div class="mx-auto max-w-7xl px-4 sm:px-6">
      <!-- Categories -->
      <section class="py-12">
        <div class="mb-6 flex items-end justify-between">
          <h2 class="text-2xl font-bold text-slate-900">Shop by category</h2>
          <router-link :to="{ name: 'shop' }" class="text-sm font-semibold text-brand-600 hover:text-brand-700">View all →</router-link>
        </div>
        <div v-if="loading" class="grid grid-cols-2 gap-4 sm:grid-cols-4 lg:grid-cols-8">
          <div v-for="i in 8" :key="i" class="h-24 animate-pulse rounded-xl bg-slate-200" />
        </div>
        <div v-else class="grid grid-cols-2 gap-4 sm:grid-cols-4 lg:grid-cols-8">
          <router-link
            v-for="category in categories"
            :key="category.id"
            :to="{ name: 'category', params: { slug: category.slug } }"
            class="card flex h-24 flex-col items-center justify-center p-3 text-center transition hover:-translate-y-0.5 hover:border-brand-300 hover:shadow-md"
            data-testid="category-tile"
          >
            <span class="text-sm font-semibold text-slate-800">{{ category.name }}</span>
            <span class="mt-1 text-xs text-slate-400">{{ category.productCount }} items</span>
          </router-link>
        </div>
      </section>

      <!-- Featured -->
      <section class="py-6">
        <div class="mb-6 flex items-end justify-between">
          <div>
            <h2 class="text-2xl font-bold text-slate-900">Featured picks</h2>
            <p class="text-sm text-slate-500">Hand-picked bestsellers our customers love.</p>
          </div>
        </div>
        <div v-if="loading" class="grid grid-cols-2 gap-4 md:grid-cols-4">
          <div v-for="i in 4" :key="i" class="h-96 animate-pulse rounded-xl bg-slate-200" />
        </div>
        <div v-else class="grid grid-cols-2 gap-4 md:grid-cols-4">
          <ProductCard v-for="product in featured" :key="product.id" :product="product" />
        </div>
      </section>

      <!-- Value props -->
      <section class="grid gap-4 py-10 sm:grid-cols-3">
        <div class="card flex items-start gap-3 p-5">
          <span class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-brand-50 text-brand-600">🚚</span>
          <div><h3 class="font-semibold text-slate-900">Fast delivery</h3><p class="text-sm text-slate-500">Standard ₹49 (free over ₹999) or express ₹199.</p></div>
        </div>
        <div class="card flex items-start gap-3 p-5">
          <span class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-brand-50 text-brand-600">🔒</span>
          <div><h3 class="font-semibold text-slate-900">Safe checkout</h3><p class="text-sm text-slate-500">Card numbers are never sent to or stored on our servers.</p></div>
        </div>
        <div class="card flex items-start gap-3 p-5">
          <span class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-brand-50 text-brand-600">↩️</span>
          <div><h3 class="font-semibold text-slate-900">Easy returns</h3><p class="text-sm text-slate-500">7-day return window on unopened items.</p></div>
        </div>
      </section>

      <!-- New arrivals -->
      <section class="py-6">
        <div class="mb-6 flex items-end justify-between">
          <h2 class="text-2xl font-bold text-slate-900">New arrivals</h2>
          <router-link :to="{ name: 'shop', query: { sort: 'newest' } }" class="text-sm font-semibold text-brand-600 hover:text-brand-700">See more →</router-link>
        </div>
        <div v-if="loading" class="grid grid-cols-2 gap-4 md:grid-cols-4">
          <div v-for="i in 4" :key="i" class="h-96 animate-pulse rounded-xl bg-slate-200" />
        </div>
        <div v-else class="grid grid-cols-2 gap-4 md:grid-cols-4">
          <ProductCard v-for="product in newest" :key="product.id" :product="product" />
        </div>
      </section>
    </div>
  </div>
</template>

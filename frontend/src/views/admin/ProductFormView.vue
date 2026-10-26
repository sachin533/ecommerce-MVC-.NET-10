<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { adminService } from '@/services/api'
import ProductImage from '@/components/ProductImage.vue'
import { useToastStore } from '@/stores/toast'
import { ApiError } from '@/services/http'
import type { Category } from '@/types'

const route = useRoute()
const router = useRouter()
const toasts = useToastStore()

const isEdit = computed(() => route.name === 'admin-product-edit')
const productId = computed(() => Number(route.params.id))

const categories = ref<Category[]>([])
const loading = ref(true)
const saving = ref(false)
const errors = ref<Record<string, string[]>>({})

const form = reactive({
  name: '',
  brand: '',
  description: '',
  price: 0,
  discountPercent: 0,
  stock: 0,
  rating: 4.5,
  ratingCount: 0,
  categoryId: 0,
  imageUrl: '',
  images: [] as string[],
  isFeatured: false,
  isActive: true,
  sku: '' as string | null,
})

const newImage = ref('')

onMounted(async () => {
  try {
    categories.value = await adminService.categories()
    if (isEdit.value) {
      const product = await adminService.product(productId.value)
      Object.assign(form, {
        name: product.name,
        brand: product.brand,
        description: product.description,
        price: product.price,
        discountPercent: product.discountPercent,
        stock: product.stock,
        rating: product.rating,
        ratingCount: product.ratingCount,
        categoryId: product.categoryId,
        imageUrl: product.imageUrl,
        images: product.images.map((i) => i.url).filter((url) => url !== product.imageUrl),
        isFeatured: product.isFeatured,
        isActive: product.isActive,
        sku: product.sku,
      })
    } else if (categories.value.length > 0) {
      form.categoryId = categories.value[0].id
    }
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not load form')
  } finally {
    loading.value = false
  }
})

function addImage() {
  const url = newImage.value.trim()
  if (url && !form.images.includes(url)) {
    form.images.push(url)
    newImage.value = ''
  }
}

function removeImage(index: number) {
  form.images.splice(index, 1)
}

async function save() {
  saving.value = true
  errors.value = {}
  try {
    const payload = {
      ...form,
      sku: form.sku || null,
      images: [...form.images],
    }
    if (isEdit.value) {
      await adminService.updateProduct(productId.value, payload)
      toasts.success('Product updated')
    } else {
      await adminService.createProduct(payload)
      toasts.success('Product created')
    }
    router.push({ name: 'admin-products' })
  } catch (error) {
    if (error instanceof ApiError) {
      errors.value = error.fieldErrors
      toasts.error(error.message)
    } else {
      toasts.error('Could not save the product')
    }
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="max-w-3xl">
    <router-link :to="{ name: 'admin-products' }" class="text-sm font-semibold text-brand-600 hover:underline">← Back to products</router-link>
    <h1 class="mt-2 text-2xl font-bold text-slate-900" data-testid="product-form-title">
      {{ isEdit ? 'Edit product' : 'New product' }}
    </h1>

    <div v-if="loading" class="mt-6 h-96 animate-pulse rounded-xl bg-slate-200" />

    <form v-else class="card mt-6 space-y-5 p-6" @submit.prevent="save" data-testid="product-form">
      <div class="grid gap-4 sm:grid-cols-2">
        <div class="sm:col-span-2">
          <label class="label" for="p-name">Name</label>
          <input id="p-name" v-model="form.name" class="input" :class="{ 'input-error': errors.Name }" required data-testid="product-name-input" />
          <p v-if="errors.Name" class="field-error">{{ errors.Name[0] }}</p>
        </div>

        <div>
          <label class="label" for="p-brand">Brand</label>
          <input id="p-brand" v-model="form.brand" class="input" :class="{ 'input-error': errors.Brand }" required />
          <p v-if="errors.Brand" class="field-error">{{ errors.Brand[0] }}</p>
        </div>

        <div>
          <label class="label" for="p-category">Category</label>
          <select id="p-category" v-model="form.categoryId" class="input" :class="{ 'input-error': errors.CategoryId }" required data-testid="product-category">
            <option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</option>
          </select>
          <p v-if="errors.CategoryId" class="field-error">{{ errors.CategoryId[0] }}</p>
        </div>

        <div>
          <label class="label" for="p-price">MRP (₹)</label>
          <input id="p-price" v-model.number="form.price" type="number" min="0" step="1" class="input" :class="{ 'input-error': errors.Price }" required data-testid="product-price" />
          <p v-if="errors.Price" class="field-error">{{ errors.Price[0] }}</p>
        </div>

        <div>
          <label class="label" for="p-discount">Discount (%)</label>
          <input id="p-discount" v-model.number="form.discountPercent" type="number" min="0" max="90" step="1" class="input" :class="{ 'input-error': errors.DiscountPercent }" required />
          <p v-if="errors.DiscountPercent" class="field-error">{{ errors.DiscountPercent[0] }}</p>
        </div>

        <div>
          <label class="label" for="p-stock">Stock</label>
          <input id="p-stock" v-model.number="form.stock" type="number" min="0" class="input" :class="{ 'input-error': errors.Stock }" required data-testid="product-stock" />
          <p v-if="errors.Stock" class="field-error">{{ errors.Stock[0] }}</p>
        </div>

        <div>
          <label class="label" for="p-sku">SKU (optional — auto-generated)</label>
          <input id="p-sku" v-model="form.sku" class="input font-mono text-sm" />
        </div>

        <div class="sm:col-span-2">
          <label class="label" for="p-image">Primary image URL</label>
          <input id="p-image" v-model="form.imageUrl" class="input" :class="{ 'input-error': errors.ImageUrl }" required placeholder="https://…" data-testid="product-image" />
          <p v-if="errors.ImageUrl" class="field-error">{{ errors.ImageUrl[0] }}</p>
          <div class="mt-2 h-24 w-24 overflow-hidden rounded-lg border border-slate-200 bg-slate-100">
            <ProductImage :src="form.imageUrl" :alt="form.name || 'Preview'" />
          </div>
        </div>

        <div class="sm:col-span-2">
          <label class="label">Additional gallery images</label>
          <div class="flex gap-2">
            <input v-model="newImage" type="url" class="input" placeholder="https://…" @keyup.enter.prevent="addImage" />
            <button type="button" class="btn-secondary shrink-0" @click="addImage">Add</button>
          </div>
          <ul v-if="form.images.length" class="mt-2 flex flex-wrap gap-2">
            <li v-for="(url, index) in form.images" :key="url" class="flex items-center gap-1 rounded-lg border border-slate-200 bg-slate-50 py-1 pl-2 pr-1 text-xs">
              <span class="max-w-48 truncate">{{ url }}</span>
              <button type="button" class="rounded p-0.5 text-slate-400 hover:text-rose-500" aria-label="Remove image" @click="removeImage(index)">✕</button>
            </li>
          </ul>
        </div>

        <div class="sm:col-span-2">
          <label class="label" for="p-description">Description</label>
          <textarea id="p-description" v-model="form.description" rows="5" class="input" :class="{ 'input-error': errors.Description }" required data-testid="product-description-input" />
          <p v-if="errors.Description" class="field-error">{{ errors.Description[0] }}</p>
        </div>

        <div>
          <label class="label" for="p-rating">Seed rating (demo data)</label>
          <input id="p-rating" v-model.number="form.rating" type="number" min="0" max="5" step="0.1" class="input" />
        </div>

        <div>
          <label class="label" for="p-rating-count">Rating count (demo data)</label>
          <input id="p-rating-count" v-model.number="form.ratingCount" type="number" min="0" class="input" />
        </div>

        <label class="flex cursor-pointer items-center gap-2 text-sm text-slate-700">
          <input v-model="form.isFeatured" type="checkbox" class="h-4 w-4 rounded border-slate-300 text-brand-600" data-testid="product-featured" />
          Featured on home page
        </label>

        <label class="flex cursor-pointer items-center gap-2 text-sm text-slate-700">
          <input v-model="form.isActive" type="checkbox" class="h-4 w-4 rounded border-slate-300 text-brand-600" data-testid="product-active" />
          Active (visible in store)
        </label>
      </div>

      <div class="flex gap-2 border-t border-slate-100 pt-4">
        <button type="submit" class="btn-primary" :disabled="saving" data-testid="product-save">
          {{ saving ? 'Saving…' : isEdit ? 'Save changes' : 'Create product' }}
        </button>
        <router-link :to="{ name: 'admin-products' }" class="btn-secondary">Cancel</router-link>
      </div>
    </form>
  </div>
</template>

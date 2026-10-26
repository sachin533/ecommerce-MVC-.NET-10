<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { adminService } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import { ApiError } from '@/services/http'
import type { Category } from '@/types'

const toasts = useToastStore()

const categories = ref<Category[]>([])
const loading = ref(true)
const editing = ref(false)
const saving = ref(false)
const errors = ref<Record<string, string[]>>({})
const editingId = ref<number | null>(null)

const blank = { name: '', slug: '', description: '', imageUrl: '', isActive: true }
const form = reactive({ ...blank })

async function load() {
  loading.value = true
  try {
    categories.value = await adminService.categories()
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not load categories')
  } finally {
    loading.value = false
  }
}

function startCreate() {
  editingId.value = null
  Object.assign(form, blank)
  errors.value = {}
  editing.value = true
}

function startEdit(category: Category) {
  editingId.value = category.id
  Object.assign(form, {
    name: category.name,
    slug: category.slug,
    description: category.description ?? '',
    imageUrl: category.imageUrl ?? '',
    isActive: category.isActive,
  })
  errors.value = {}
  editing.value = true
}

async function save() {
  saving.value = true
  errors.value = {}
  try {
    const payload = {
      name: form.name,
      slug: form.slug || undefined,
      description: form.description || null,
      imageUrl: form.imageUrl || null,
      isActive: form.isActive,
    }
    if (editingId.value) {
      await adminService.updateCategory(editingId.value, payload)
      toasts.success('Category updated')
    } else {
      await adminService.createCategory(payload)
      toasts.success('Category created')
    }
    editing.value = false
    await load()
  } catch (error) {
    if (error instanceof ApiError) {
      errors.value = error.fieldErrors
      toasts.error(error.message)
    } else {
      toasts.error('Could not save the category')
    }
  } finally {
    saving.value = false
  }
}

async function remove(category: Category) {
  if (!confirm(`Delete category “${category.name}”?`)) return
  try {
    await adminService.deleteCategory(category.id)
    toasts.success('Category deleted')
    await load()
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not delete the category')
  }
}

onMounted(load)
</script>

<template>
  <div>
    <div class="mb-6 flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-slate-900" data-testid="admin-categories-title">Categories</h1>
        <p class="text-sm text-slate-500">{{ categories.length }} categories</p>
      </div>
      <button class="btn-primary" data-testid="new-category" @click="startCreate">+ Add category</button>
    </div>

    <form v-if="editing" class="card mb-6 p-5" data-testid="category-form" @submit.prevent="save">
      <h2 class="font-semibold text-slate-900">{{ editingId ? 'Edit category' : 'New category' }}</h2>
      <div class="mt-4 grid gap-4 sm:grid-cols-2">
        <div>
          <label class="label" for="c-name">Name</label>
          <input id="c-name" v-model="form.name" class="input" :class="{ 'input-error': errors.Name }" required data-testid="category-name" />
          <p v-if="errors.Name" class="field-error">{{ errors.Name[0] }}</p>
        </div>
        <div>
          <label class="label" for="c-slug">Slug (optional)</label>
          <input id="c-slug" v-model="form.slug" class="input" :class="{ 'input-error': errors.Slug }" placeholder="auto from name" />
          <p v-if="errors.Slug" class="field-error">{{ errors.Slug[0] }}</p>
        </div>
        <div class="sm:col-span-2">
          <label class="label" for="c-description">Description (optional)</label>
          <textarea id="c-description" v-model="form.description" rows="2" class="input" />
        </div>
        <div class="sm:col-span-2">
          <label class="label" for="c-image">Image URL (optional)</label>
          <input id="c-image" v-model="form.imageUrl" class="input" placeholder="https://…" />
        </div>
        <label class="flex items-center gap-2 text-sm text-slate-700">
          <input v-model="form.isActive" type="checkbox" class="h-4 w-4 rounded border-slate-300 text-brand-600" />
          Active
        </label>
      </div>
      <div class="mt-4 flex gap-2">
        <button class="btn-primary" :disabled="saving" data-testid="category-save">{{ saving ? 'Saving…' : 'Save' }}</button>
        <button type="button" class="btn-secondary" @click="editing = false">Cancel</button>
      </div>
    </form>

    <div v-if="loading" class="space-y-2">
      <div v-for="i in 6" :key="i" class="h-14 animate-pulse rounded-lg bg-slate-200" />
    </div>

    <div v-else class="card overflow-x-auto">
      <table class="w-full min-w-[560px] text-left text-sm">
        <thead class="border-b border-slate-200 bg-slate-50 text-xs uppercase text-slate-500">
          <tr>
            <th class="px-4 py-3">Name</th>
            <th class="px-4 py-3">Slug</th>
            <th class="px-4 py-3">Products</th>
            <th class="px-4 py-3">Status</th>
            <th class="px-4 py-3 text-right">Actions</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100" data-testid="category-table">
          <tr v-for="category in categories" :key="category.id" data-testid="category-row">
            <td class="px-4 py-3 font-medium text-slate-900">{{ category.name }}</td>
            <td class="px-4 py-3 font-mono text-xs text-slate-500">{{ category.slug }}</td>
            <td class="px-4 py-3 text-slate-600">{{ category.productCount }}</td>
            <td class="px-4 py-3">
              <span class="badge" :class="category.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'">
                {{ category.isActive ? 'Active' : 'Hidden' }}
              </span>
            </td>
            <td class="px-4 py-3 text-right">
              <div class="flex justify-end gap-2">
                <button class="text-xs font-semibold text-brand-600 hover:underline" :data-testid="`edit-category-${category.id}`" @click="startEdit(category)">Edit</button>
                <button class="text-xs font-semibold text-rose-600 hover:underline" :data-testid="`delete-category-${category.id}`" @click="remove(category)">Delete</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

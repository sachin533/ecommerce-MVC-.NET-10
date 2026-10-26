<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { addressService } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import { ApiError } from '@/services/http'
import type { Address } from '@/types'

const toasts = useToastStore()

const addresses = ref<Address[]>([])
const loading = ref(true)
const editing = ref(false)
const saving = ref(false)
const errors = ref<Record<string, string[]>>({})
const editingId = ref<number | null>(null)

const blank = {
  fullName: '',
  phone: '',
  line1: '',
  line2: '',
  city: '',
  state: '',
  postalCode: '',
  country: 'India',
  isDefault: false,
}
const form = reactive({ ...blank })

async function load() {
  loading.value = true
  try {
    addresses.value = await addressService.list()
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

function startEdit(address: Address) {
  editingId.value = address.id
  Object.assign(form, {
    fullName: address.fullName,
    phone: address.phone,
    line1: address.line1,
    line2: address.line2 ?? '',
    city: address.city,
    state: address.state,
    postalCode: address.postalCode,
    country: address.country,
    isDefault: address.isDefault,
  })
  errors.value = {}
  editing.value = true
}

async function save() {
  saving.value = true
  errors.value = {}
  try {
    if (editingId.value) {
      await addressService.update(editingId.value, { ...form })
      toasts.success('Address updated')
    } else {
      await addressService.create({ ...form })
      toasts.success('Address added')
    }
    editing.value = false
    await load()
  } catch (error) {
    if (error instanceof ApiError) {
      errors.value = error.fieldErrors
      toasts.error(error.message)
    } else {
      toasts.error('Could not save the address')
    }
  } finally {
    saving.value = false
  }
}

async function makeDefault(address: Address) {
  try {
    await addressService.setDefault(address.id)
    toasts.success('Default address updated')
    await load()
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not update default')
  }
}

async function remove(address: Address) {
  try {
    await addressService.remove(address.id)
    toasts.info('Address removed')
    await load()
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not remove address')
  }
}

onMounted(load)
</script>

<template>
  <div class="mx-auto max-w-3xl px-4 py-8 sm:px-6">
    <div class="mb-6 flex items-center justify-between">
      <h1 class="text-2xl font-bold text-slate-900" data-testid="addresses-title">Addresses</h1>
      <button class="btn-primary" data-testid="add-address" @click="startCreate">+ Add address</button>
    </div>

    <!-- Editor -->
    <form v-if="editing" class="card mb-6 p-5" data-testid="address-form" @submit.prevent="save">
      <h2 class="font-semibold text-slate-900">{{ editingId ? 'Edit address' : 'New address' }}</h2>
      <div class="mt-4 grid gap-4 sm:grid-cols-2">
        <div class="sm:col-span-2">
          <label class="label" for="a-name">Full name</label>
          <input id="a-name" v-model="form.fullName" class="input" :class="{ 'input-error': errors.FullName }" required />
          <p v-if="errors.FullName" class="field-error">{{ errors.FullName[0] }}</p>
        </div>
        <div>
          <label class="label" for="a-phone">Phone</label>
          <input id="a-phone" v-model="form.phone" class="input" :class="{ 'input-error': errors.Phone }" required />
          <p v-if="errors.Phone" class="field-error">{{ errors.Phone[0] }}</p>
        </div>
        <div>
          <label class="label" for="a-postal">PIN code</label>
          <input id="a-postal" v-model="form.postalCode" class="input" :class="{ 'input-error': errors.PostalCode }" required />
          <p v-if="errors.PostalCode" class="field-error">{{ errors.PostalCode[0] }}</p>
        </div>
        <div class="sm:col-span-2">
          <label class="label" for="a-line1">Address line 1</label>
          <input id="a-line1" v-model="form.line1" class="input" :class="{ 'input-error': errors.Line1 }" required />
          <p v-if="errors.Line1" class="field-error">{{ errors.Line1[0] }}</p>
        </div>
        <div class="sm:col-span-2">
          <label class="label" for="a-line2">Address line 2 (optional)</label>
          <input id="a-line2" v-model="form.line2" class="input" />
        </div>
        <div>
          <label class="label" for="a-city">City</label>
          <input id="a-city" v-model="form.city" class="input" :class="{ 'input-error': errors.City }" required />
          <p v-if="errors.City" class="field-error">{{ errors.City[0] }}</p>
        </div>
        <div>
          <label class="label" for="a-state">State</label>
          <input id="a-state" v-model="form.state" class="input" :class="{ 'input-error': errors.State }" required />
          <p v-if="errors.State" class="field-error">{{ errors.State[0] }}</p>
        </div>
        <label class="flex items-center gap-2 text-sm text-slate-700 sm:col-span-2">
          <input v-model="form.isDefault" type="checkbox" class="h-4 w-4 rounded border-slate-300 text-brand-600" />
          Set as default
        </label>
      </div>
      <div class="mt-4 flex gap-2">
        <button class="btn-primary" :disabled="saving" data-testid="save-address">{{ saving ? 'Saving…' : 'Save address' }}</button>
        <button type="button" class="btn-secondary" @click="editing = false">Cancel</button>
      </div>
    </form>

    <!-- List -->
    <div v-if="loading" class="space-y-3">
      <div v-for="i in 2" :key="i" class="h-28 animate-pulse rounded-xl bg-slate-200" />
    </div>

    <div v-else-if="addresses.length === 0 && !editing" class="rounded-xl border border-dashed border-slate-300 p-16 text-center" data-testid="empty-addresses">
      <p class="text-lg font-semibold text-slate-700">No saved addresses</p>
      <p class="mt-1 text-sm text-slate-500">Add one to speed through checkout.</p>
      <button class="btn-primary mt-4" @click="startCreate">Add address</button>
    </div>

    <ul v-else class="space-y-3" data-testid="address-list">
      <li v-for="address in addresses" :key="address.id" class="card p-4" data-testid="address-row">
        <div class="flex items-start justify-between gap-3">
          <div class="text-sm">
            <p class="font-semibold text-slate-900">
              {{ address.fullName }}
              <span v-if="address.isDefault" class="badge ml-2 bg-brand-50 text-brand-700">Default</span>
            </p>
            <p class="text-slate-600">{{ address.line1 }}<template v-if="address.line2">, {{ address.line2 }}</template></p>
            <p class="text-slate-600">{{ address.city }}, {{ address.state }} {{ address.postalCode }}</p>
            <p class="text-slate-500">{{ address.phone }}</p>
          </div>
          <div class="flex shrink-0 gap-1">
            <button v-if="!address.isDefault" class="text-xs font-semibold text-brand-600 hover:underline" @click="makeDefault(address)">Make default</button>
            <button class="text-xs font-semibold text-slate-600 hover:underline" @click="startEdit(address)">Edit</button>
            <button class="text-xs font-semibold text-rose-600 hover:underline" :data-testid="`delete-address-${address.id}`" @click="remove(address)">Delete</button>
          </div>
        </div>
      </li>
    </ul>
  </div>
</template>

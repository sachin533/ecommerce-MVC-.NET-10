<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { adminService } from '@/services/api'
import PaginationBar from '@/components/PaginationBar.vue'
import { useToastStore } from '@/stores/toast'
import { ApiError } from '@/services/http'
import type { AdminCustomer, PagedResult } from '@/types'

const toasts = useToastStore()

const results = ref<PagedResult<AdminCustomer> | null>(null)
const loading = ref(true)
const search = ref('')
const page = ref(1)

async function load() {
  loading.value = true
  try {
    results.value = await adminService.customers({ search: search.value || undefined, page: page.value, pageSize: 20 })
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Could not load customers')
  } finally {
    loading.value = false
  }
}

watch(page, load)
onMounted(load)
</script>

<template>
  <div>
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-slate-900" data-testid="admin-customers-title">Customers</h1>
      <p class="text-sm text-slate-500">{{ results?.totalCount ?? '…' }} registered accounts</p>
    </div>

    <div class="card mb-4 p-3">
      <input v-model="search" type="search" placeholder="Search email or name…" class="input max-w-md" data-testid="customer-search" @input="page = 1; load()" />
    </div>

    <div v-if="loading" class="space-y-2">
      <div v-for="i in 6" :key="i" class="h-14 animate-pulse rounded-lg bg-slate-200" />
    </div>

    <div v-else-if="results" class="card overflow-x-auto" data-testid="customer-table">
      <table class="w-full min-w-[680px] text-left text-sm">
        <thead class="border-b border-slate-200 bg-slate-50 text-xs uppercase text-slate-500">
          <tr>
            <th class="px-4 py-3">Customer</th>
            <th class="px-4 py-3">Phone</th>
            <th class="px-4 py-3">Joined</th>
            <th class="px-4 py-3">Orders</th>
            <th class="px-4 py-3">Total spent</th>
            <th class="px-4 py-3">Roles</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100">
          <tr v-for="customer in results.items" :key="customer.id" data-testid="customer-row">
            <td class="px-4 py-3">
              <p class="font-medium text-slate-900">{{ customer.fullName }}</p>
              <p class="text-xs text-slate-500">{{ customer.email }}</p>
            </td>
            <td class="px-4 py-3 text-slate-600">{{ customer.phoneNumber ?? '—' }}</td>
            <td class="px-4 py-3 text-slate-600">{{ new Date(customer.createdAt).toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' }) }}</td>
            <td class="px-4 py-3 text-slate-600">{{ customer.orderCount }}</td>
            <td class="px-4 py-3 font-medium text-slate-900">₹{{ Math.round(customer.totalSpent).toLocaleString('en-IN') }}</td>
            <td class="px-4 py-3">
              <span v-for="role in customer.roles" :key="role" class="badge mr-1 bg-brand-50 text-brand-700">{{ role }}</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <PaginationBar
      v-if="results"
      :page="results.page"
      :total-pages="results.totalPages"
      :total-count="results.totalCount"
      @change="page = $event"
    />
  </div>
</template>

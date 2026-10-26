<script setup lang="ts">
defineProps<{ status: string }>()

const styles: Record<string, string> = {
  Pending: 'bg-amber-100 text-amber-800',
  Paid: 'bg-sky-100 text-sky-800',
  Processing: 'bg-indigo-100 text-indigo-800',
  Shipped: 'bg-violet-100 text-violet-800',
  Delivered: 'bg-emerald-100 text-emerald-800',
  Cancelled: 'bg-rose-100 text-rose-800',
}
</script>

<template>
  <span class="badge" :class="styles[status] ?? 'bg-slate-100 text-slate-700'" :data-testid="`status-${status.toLowerCase()}`">
    {{ status }}
  </span>
</template>

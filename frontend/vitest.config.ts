import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vitest/config'

// Vitest reuses the app's Vite plugins/aliases so `@/...` imports and <script setup>
// SFCs compile in tests exactly as they do in the real build.
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  test: {
    environment: 'jsdom',
    include: ['src/**/*.{test,spec}.ts'],
    // Tests import from 'vitest' explicitly — no globals required.
    globals: false,
  },
})

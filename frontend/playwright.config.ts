import { defineConfig, devices } from '@playwright/test'

/**
 * End-to-end suite for QuickShop.
 *
 * Requirements (see README):
 *  - SQL Server with the dev database seeded (`QuickShopDb`)
 *  - Backend on http://localhost:5080  (started automatically if not already up)
 *  - Frontend dev server on http://localhost:5173 (started automatically if not already up)
 *
 * Run with: npm run test:e2e
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: [['list']],
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    actionTimeout: 15_000,
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  webServer: [
    {
      // Reuse a running dev API; start one when absent.
      command: 'dotnet run --project backend/src/QuickShop.Api',
      url: 'http://localhost:5080/api/health',
      reuseExistingServer: true,
      timeout: 180_000,
      cwd: '..',
    },
    {
      command: 'npm run dev',
      url: 'http://localhost:5173',
      reuseExistingServer: true,
      timeout: 60_000,
    },
  ],
})

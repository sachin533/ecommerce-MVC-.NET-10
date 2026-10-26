# QuickShop

A modern full-stack eCommerce platform: a **Vue 3 + TypeScript** single-page storefront and admin console, backed by an **ASP.NET Core Minimal API** on .NET 10 with EF Core 10 (SQL Server), JWT/cookie authentication, and a clearly-labelled mock payment provider.

- **Frontend:** Vue 3 (Composition API) · Vite · Vue Router · Pinia · Tailwind CSS v4 · Axios
- **Backend:** ASP.NET Core Minimal API (`MapGroup` + DI) · EF Core 10 (code-first migrations) · ASP.NET Core Identity · JWT bearer · Swagger
- **Tests:** xUnit (unit + integration) · Vitest + Vue Test Utils · Playwright (end-to-end)

---

## Prerequisites

| Tool | Notes |
|------|-------|
| .NET 10 SDK | Backend, tests and EF CLI |
| Node.js 20+ | Frontend build and test tooling |
| SQL Server | `.\SQLEXPRESS` works out of the box; any SQL Server instance via the connection string |

## Configuration and secrets

Secrets are **never committed**. The JWT signing key is read from user-secrets in Development; the API fails fast at startup with setup instructions if it is missing.

```bash
cd backend/src/QuickShop.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "<a long random string of at least 32 characters>"
```

Connection string (defaults to `.\SQLEXPRESS` with integrated auth) is set in `appsettings.json` and can be overridden per environment or with the standard `ConnectionStrings__Default` environment variable.

## Database: migrate and seed

Migrations and seeding run automatically at startup in the Development environment (`Data:ApplyMigrations` / `Data:Seed` are both `true` in `appsettings.Development.json`). To run them by hand:

```bash
cd backend
dotnet tool restore                       # restores the local dotnet-ef tool
dotnet ef database update \
  --project src/QuickShop.Api --startup-project src/QuickShop.Api
```

Seeding is idempotent — safe to re-run. The dev database (`QuickShopDb`) is seeded with **186 products across 14 categories**, 1 admin + 3 customer accounts, and 17 sample orders.

**Dev accounts**

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@quickshop.dev` | `Admin@12345` |
| Customer | `rahul.sharma@demo.dev` | `Demo@12345` |
| Customer | `priya.verma@demo.dev` | `Demo@12345` |
| Customer | `arjun.iyer@demo.dev` | `Demo@12345` |

## Running the app

**Backend** — http://localhost:5080 (Swagger UI enabled in Development)

```bash
cd backend/src/QuickShop.Api
dotnet run
```

**Frontend** — http://localhost:5173 (dev server proxies `/api` to the backend)

```bash
cd frontend
npm install
npm run dev
```

Health check: `GET http://localhost:5080/api/health`.

## Testing

```bash
# Backend — xUnit unit + integration tests
cd backend
dotnet test

# Frontend — Vitest component/store tests
cd frontend
npm test

# Frontend — Playwright end-to-end (starts API + dev server if not already running)
cd frontend
npx playwright install chromium   # first run only
npm run test:e2e
```

The Playwright config reuses an already-running API (port 5080) and dev server (port 5173); otherwise it starts them. Integration and e2e suites seed a dedicated test database (`QuickShopDb_Test`) and never touch the dev data.

## How the important pieces work

**Authentication.** One JWT bearer scheme serves both the `Authorization: Bearer` header and an HttpOnly `quickshop_auth` cookie. Cookie-authenticated unsafe requests must echo the readable `quickshop_xsrf` cookie in an `X-XSRF-TOKEN` header (double-submit CSRF). "Remember me" makes the cookie persistent; otherwise it is a session cookie. `POST /api/auth/refresh` slides the token expiry and rotates the XSRF value. On expiry or logout the frontend clears local state and routes the user to sign in.

**Carts.** No server sessions. Carts are rows in the database. Guests use an opaque client-generated cart id sent as `X-Cart-Id`; the API validates ownership on every request. On sign-in/registration the guest cart is folded into the user cart. Prices, totals, roles and user ids are always derived server-side — the client never dictates them.

**Checkout and payments.** Multi-step on the client (address → shipping → payment → review). The order transaction runs inside EF's execution strategy so a transient failure retries the whole unit. The mock payment provider declines any card whose final digit is odd; on decline the API returns **402** with `orderNumber` and `providerReason`, the order is marked `Cancelled`, stock is restocked, and the customer's cart is left untouched. **Card numbers and CVVs are never stored** — only the last four digits reach the server, and the CVV never leaves the browser.

**Passwords (dev).** In Development there is no SMTP, so the forgot-password response includes a `devResetToken`; the token is also written to `logs/dev-emails/` and the console. Tokens are single-use and expiring. The UI surfaces a development-only "reset link" so the flow can be completed in the browser.

## Project layout

```
backend/
  src/QuickShop.Api/
    Domain/ Data/ DTOs/ Services/ Endpoints/ Security/ Common/ Program.cs
  tests/QuickShop.Tests/          # xUnit unit + integration
frontend/
  src/
    components/ views/ layouts/ router/ stores/ services/ types/ composables/ assets/
  e2e/                            # Playwright specs
```

## Verification checklist

- Seeded catalog: **186 products**, **14 categories** (unique SKUs/names, INR pricing).
- Backend tests: **92 passed / 0 failed**.
- Frontend unit tests: **67 passed / 0 failed**.
- Playwright e2e: **16 passed / 0 failed** (register/login, browsing, cart, checkout success + decline, order history, admin authz).
- Live 402 decline smoke: odd-digit card → HTTP 402 with `orderNumber`/`providerReason`, order `Cancelled`, stock restored, cart preserved.

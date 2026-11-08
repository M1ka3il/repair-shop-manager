# repair-shop-web

Angular 22 frontend for the Repair Shop API — standalone components, signals,
lazy-loaded routes, no UI library.

## Run

Against the API running locally on `:8080` (see the root README, "Local development
without Docker"):

```bash
npm ci
npm start          # http://localhost:4200, /api is proxied to :8080 (proxy.conf.json)
```

Or the whole stack with `docker compose up --build` from the repository root:
UI on <http://localhost:4200>, Swagger on <http://localhost:8080>.

## The API contract

The TypeScript types are **generated**, never hand-written:

```
backend OpenAPI ──snapshot──▶ openapi/repair-shop-api.json ──npm run api:types──▶ src/app/core/api/schema.gen.ts
```

Two guards keep both halves honest:

| Guard | Fails when |
|---|---|
| `OpenApiContractTests` (backend integration test) | the API changed but the snapshot was not refreshed |
| CI step "Generated API types match the OpenAPI snapshot" | the snapshot changed but the types were not regenerated |

After an intended API change:

```bash
UPDATE_OPENAPI_SNAPSHOT=1 dotnet test tests/RepairShop.IntegrationTests --filter OpenApi   # from the repo root
npm run api:types
```

Components import friendly names from `core/api/models.ts`, not from the generated file.

## Structure

```
src/app/
├── core/
│   ├── api/       generated schema, model aliases, typed HttpClient wrappers
│   ├── auth/      session, bearer-token interceptor, route guards
│   ├── http/      ApiError + interceptor: every failure arrives as one type
│   └── status/    status labels and colours (display only — rules stay in the backend)
├── features/
│   ├── board/         start page: orders by status, status changes
│   ├── order-detail/  lifecycle, description, line items, totals, delete
│   ├── new-order/     find or register the vehicle (and customer), then open the order
│   ├── customers/     master data: customers and their vehicles, selection in the URL
│   ├── login/         sign-in form
│   └── report/        revenue per customer: period, totals, stacked bars, table, CSV
├── shared/
│   ├── forms/     German decimal input, customer/vehicle form controls, validation messages
│   └── notice/    success/error feedback after an action
├── app.ts / app.html  shell with navigation
└── app.routes.ts      route table
```

## Sign-in

- `core/auth/`: `AuthService` (session in `sessionStorage`: closing the browser signs out),
  `authInterceptor` (adds the bearer token to `/api/` calls; a 401 signs out and
  returns to the login with `returnUrl`), `authGuard` / `roleGuard('Office')`.
- After login only same-app paths are followed (`/x` yes, `//host` no — no open redirect).
- The role shapes the UI (menu, buttons); the API enforces every rule itself.

## Look and feel

- **Light and dark mode** from one set of colour tokens in `src/styles.css`; no
  component hard-codes a colour. Default follows the OS (`prefers-color-scheme`);
  the toggle in the top bar cycles System → Hell → Dunkel and stores the choice
  in `localStorage`. A small script in `index.html` applies it before Angular
  starts, so dark mode never flashes white.
- Text pairs checked for WCAG ≥ 4.5:1 in both modes; the two chart series are
  validated per mode (lightness band, colour-blind separation, contrast).
- **Inter is self-hosted** (`@fontsource-variable/inter`, OFL), not loaded from
  Google Fonts: in Germany, embedding Google Fonts transfers visitors' IP
  addresses to Google and has led to cease-and-desist letters (e.g. LG München,
  2022).

## Scripts

| Script | Purpose |
|---|---|
| `npm start` | Dev server with API proxy |
| `npm test` | Unit tests (Vitest, single run) |
| `npm run build` | Production build into `dist/` |
| `npm run api:types` | Regenerate API types from the snapshot |
| `npm run format` / `format:check` | Prettier |

## Note on `overrides`

`openapi-typescript` 7.x declares `typescript@^5` as a peer, Angular 22 ships
TypeScript 6. `package.json` points the tool at the project's TypeScript via
`overrides`; generation works and is checked in CI. Remove the override once
`openapi-typescript` supports TypeScript 6.

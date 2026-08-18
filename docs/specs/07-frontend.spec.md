# Spec 07: Vue 3 + Element Plus Web Frontend

## Objective
Build the bookstore's single web client in `src/Web/frontend` (Vue 3, Vite, TypeScript, Element Plus, Pinia, Vue Router) and enable it to call the YARP gateway (`http://localhost:8080`) from its own origin (`http://localhost:3000`) by adding CORS to the gateway (Option A). The SPA provides catalog browsing/search, cart management, checkout, order tracking, login, and admin book management, using strictly the Spec 05 public route map. All client HTTP traffic targets the gateway; upstream service ports are never called directly.

## Requirements

### 1. API Gateway CORS (Option A — supersedes Spec 05 Req 5's "no custom middleware")
- **Why**: the SPA origin `http://localhost:3000` differs from the gateway `http://localhost:8080`, so every API call is cross-origin. YARP does NOT enable CORS by default and does not auto-match preflight `OPTIONS` unless configured (per the [YARP CORS documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/cors)); without this change, `PUT`/`DELETE`/`PATCH` + JSON + `Authorization` all trigger a preflight that the gateway answers with `404`, so the SPA cannot work at all.
- Modify **only** `src/Gateways/ApiGateway/Program.cs`:
  - Register a default CORS policy that allows only the SPA origin, placed after `AddServiceDefaults()`:
    ```csharp
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()));
    ```
  - Apply it **before** `app.MapReverseProxy()`:
    ```csharp
    app.UseCors();
    app.MapReverseProxy();
    ```
  - Use `WithOrigins(...)`, NOT `AllowAnyOrigin()` — the origin stays explicit and the policy stays credentials-compatible. If the SPA is later served from another origin, add that origin to `WithOrigins`.
- Behavior: the CORS middleware answers preflight `OPTIONS` with `204` + `Access-Control-Allow-*` before the proxy runs, and stamps `Access-Control-Allow-Origin` onto real cross-origin responses.
- **Gateway preflight test** (`tests/ApiGateway.Tests`): a `WebApplicationFactory<Program>` test asserting `OPTIONS /catalog/api/books` with `Origin: http://localhost:3000` + `Access-Control-Request-Method: GET` returns `204` and `Access-Control-Allow-Origin: http://localhost:3000`; and that a `GET` with that `Origin` echoes the ACAO header.

### 2. Frontend Application (`src/Web/frontend`)
- **Framework**: Vue 3 (Composition API `<script setup>`).
- **Build tool & language**: Vite 6+, TypeScript (strict); `vue-tsc --noEmit` must pass.
- **UI library**: Element Plus + `@element-plus/icons-vue`.
- **State**: Pinia (`authStore`, `cartStore`).
- **Routing**: Vue Router 4 (History Mode).
- **HTTP**: Axios with request/response interceptors.
- Commit `package-lock.json` so CI/`docker build` can use `npm ci`.

### 3. HTTP Client & Error Handling (`src/api/client.ts`)
- `baseURL = import.meta.env.VITE_API_GATEWAY_URL || 'http://localhost:8080'`.
- Request interceptor: attach `Authorization: Bearer <token>` when `authStore` holds a token.
- Response interceptor:
  - `400` → `ElMessage.error` (payload detail if present).
  - `401` → clear `authStore`, redirect to `/login`.
  - `409` → `ElNotification.warning` ("Insufficient stock for one or more items.").
  - network error → friendly message.

### 4. Authentication Contract (authoritative — matches Identity.Api)
- **Login** — `POST /identity/connect/token` with `Content-Type: application/x-www-form-urlencoded` body:
  ```
  grant_type=password&client_id=bookstore-app&username={username}&password={password}
  ```
  - `bookstore-app` is a **public** client (no secret) registered in the identity DB seeder; password grant and `profile`/`email`/`roles` scopes are enabled. (Optionally append `scope=profile email roles`.)
  - `200` → `{ access_token, token_type: "Bearer", expires_in, scope, ... }`; store `access_token`.
  - `400` with `{ error: "invalid_grant", error_description: "Invalid username or password." }` → show error, do not redirect.
- **User info** — `GET /identity/connect/userinfo`, header `Authorization: Bearer <access_token>`:
  - `200` → `{ sub, name, email, role }`; `role` is `"Customer"` or `"Admin"`.
  - `401` → token invalid/expired → clear auth, redirect to login.
- **`isAdmin`** = `role === "Admin"`. The JWT is signed and readable (not encrypted) and carries the `role` claim as well, so admin gating may read from the token or from `userinfo`.
- **Seed accounts** (identity seeding, for manual QA): `alice` / `P@ssw0rd!` (Customer), `admin` / `Admin@123` (Admin).
- **Note**: today only `/identity/connect/userinfo` is `[Authorize]`d; the catalog/order endpoints have no server-side authorization, so admin actions are currently gated at the UI layer by `isAdmin`. Keep the UI gate; do not treat it as a security boundary.

### 5. State Stores (Pinia)
- **`authStore`**: `token`, `user`, `isAdmin` getter; login/logout actions.
- **`cartStore`**: auto-generated UUID `sessionId` persisted to `localStorage`; `lines`, `totalAmount`, `itemCount` (badge) getters; add/update/remove/clear actions.

### 6. Authoritative Data Contracts
Field names are camelCase JSON (ASP.NET Core defaults). These shapes are authoritative per Spec 03/04 — type them exactly; do not add/remove fields.

**Book** (catalog):
| Field | Type | Notes |
|---|---|---|
| id | number | server-assigned |
| title | string | required on create/update |
| author | string | may be empty |
| isbn | string | required on create/update |
| price | number | >= 0 |
| stockQuantity | number | >= 0 |
| category | string \| null | optional |
| createdAt / updatedAt | string (ISO-8601) | server-set |
| isDeleted | boolean | always false in responses (soft-deleted books are hidden) |

**Catalog endpoints** (gateway `/catalog/api/books`):
| Method & Path | Request body / query | Success | Error |
|---|---|---|---|
| `GET /catalog/api/books` | `?keyword=&category=&minPrice=&maxPrice=&page=1&pageSize=20` (pageSize clamped 1–100) | `200 { items: Book[], totalCount, page, pageSize }` | — |
| `GET /catalog/api/books/{id}` | — | `200 Book` | `404` |
| `POST /catalog/api/books` | `{ title, author, isbn, price, stockQuantity, category }` | `201 Book` | `400` (title/isbn empty, price<0, stockQuantity<0) |
| `PUT /catalog/api/books/{id}` | same as create | `200 Book` | `400`, `404` |
| `DELETE /catalog/api/books/{id}` | — | `204` | `404` |
| `PATCH /catalog/api/books/{id}/stock` | `{ delta }` | `200 Book` | `400` (would go below zero), `404` |

**Cart** (order): `CartItem` = `{ id, sessionId, bookId, title, unitPrice, quantity }`.
| Method & Path | Request body / query | Success | Error |
|---|---|---|---|
| `GET /order/api/cart` | `?sessionId={id}` | `200 { items: CartItem[], totalAmount }` | — |
| `POST /order/api/cart/items` | `{ sessionId, bookId, title, unitPrice, quantity }` | `200 CartItem` (increments quantity if the book already exists in the cart) | `400` (empty sessionId, quantity < 1) |
| `PATCH /order/api/cart/items/{bookId}` | `?sessionId={id}` + body `{ quantity }` | `200 CartItem` | `400`, `404` |
| `DELETE /order/api/cart/items/{bookId}` | `?sessionId={id}` | `204` | `404` |
| `DELETE /order/api/cart` | `?sessionId={id}` | `204` | — |

**Order** (order): `OrderStatus` is serialized as its **integer enum value** — `Placed=0, Paid=1, Processing=2, Shipped=3, Completed=4, Cancelled=5` (Order.Api does not register `JsonStringEnumConverter`; verified against the running stack). `OrderItem` = `{ id, orderId, bookId, title, unitPrice, quantity, lineTotal }`. `OrderResponse` (detail/checkout) = `{ id, customerName, totalAmount, status, createdAt, updatedAt, items: OrderItem[] }`. The list endpoint returns bare `Order` entities — `{ id, customerName, totalAmount, status, createdAt, updatedAt }` — with **no nested items**.
| Method & Path | Request body / query | Success | Error |
|---|---|---|---|
| `POST /order/api/orders/checkout` | `{ sessionId, customerName }` | `201 OrderResponse` | `400` (empty cart), `409` (stock deduction failed; no order created) |
| `GET /order/api/orders` | `?page=1&pageSize=20` | `200 { items: Order[], totalCount, page, pageSize }` | — |
| `GET /order/api/orders/{id}` | — | `200 OrderResponse` | `404` |
| `POST /order/api/orders/{id}/status` | `{ status }` (integer enum value, e.g. `1` = Paid) | `200 Order` | `400` (invalid transition), `404` |
| `POST /order/api/orders/{id}/cancel` | — | `200 Order` | `400`, `404` |

**Cart snapshot note**: adding to cart requires the client to supply `title` and `unitPrice` (Spec 04 snapshots them at add-time). Fetch `GET /catalog/api/books/{id}` before adding so the cart shows the current title/price.

### 7. Pages & Routes (Vue Router, History Mode)
| Path | Page | Access |
|---|---|---|
| `/login` | Login form (password grant) | public |
| `/profile` | My Account — shows `name`, `email`, `role` from `GET /identity/connect/userinfo`; logout | logged in |
| `/` | Book list with keyword/category/price filter + pagination, "Add to cart" | public |
| `/books/:id` | Book detail + add to cart | public |
| `/cart` | Cart lines, quantity update/remove, checkout button | public |
| `/orders` | Order list + status timeline | public (view own history) |
| `/orders/:id` | Order detail with items | public |
| `/admin/books` | Book create/edit/delete + stock adjust | `isAdmin` only |
- Non-admin navigating to `/admin/*` → redirect to `/`.

### 8. Containerization
- **Dockerfile** (multi-stage, `src/Web/frontend/Dockerfile`):
  - Build stage: `node:22-alpine`; `WORKDIR /app`; `COPY package*.json ./` → `RUN npm ci`; copy the rest → `RUN npm run build` (which runs `vue-tsc --noEmit` + `vite build`).
  - Runtime stage: `nginx:1.27-alpine`; copy `dist/` into the nginx html root; `EXPOSE 80`.
  - **History-mode fallback is mandatory**: include an nginx config with `try_files $uri $uri/ /index.html;` so deep links like `/admin/books` do not 404 on refresh.
  - `baseURL` default `http://localhost:8080` works as-is (Option A). Allow override via `VITE_API_GATEWAY_URL` build arg.
- **docker-compose.yaml** — add a `frontend` service:
  - `build: { context: src/Web/frontend }`; `image: ${IMAGE_REGISTRY:-local}/bookstore-frontend:${IMAGE_TAG:-latest}`.
  - `ports: "3000:80"`.
  - `depends_on: api-gateway: { condition: service_healthy }`.
  - Optional `environment: VITE_API_GATEWAY_URL: http://localhost:8080` for explicit override.

### 9. Out of Scope — User Management & Permission Management
- **User management** (registration, user list, create/update/delete users) and **permission management** (role CRUD, assigning roles to users, per-route authorization) are NOT part of this spec.
- Reason: Identity.Api exposes no such endpoints today — only `/connect/token` (login) and `/connect/userinfo` (profile). The in-memory user store seeds `alice` (Customer) and `admin` (Admin) and supports credential verification only; there is no backend contract for the SPA to call.
- The only permission primitive available is the `role` claim (`"Customer"` | `"Admin"`) returned by userinfo and present in the JWT; the SPA uses it solely for the `isAdmin` UI gate (Req 4 / Req 7).
- If these features are required later, a new backend spec (e.g. "Spec 08: Identity admin — user CRUD + role assignment REST endpoints") must be written and implemented BEFORE the frontend can consume them.

## Rules
- Scope Isolation (Option A): ONLY create/modify files under `src/Web/frontend`, plus `src/Gateways/ApiGateway/Program.cs` (CORS wiring only), `tests/ApiGateway.Tests` (preflight test), and `docker-compose.yaml`. Do NOT touch Identity.Api / Catalog.Api / Order.Api, their `appsettings*.json`, `.sln`, existing test code, or specs 01–06.
- All client HTTP traffic MUST target the gateway (`http://localhost:8080`, or the `VITE_API_GATEWAY_URL` override); never call upstream service ports directly.
- No credentials in source: `client_id` is a public constant, but passwords are only ever user input.
- Run `npm install` / `npm ci` then `npm run build` and `npx vue-tsc --noEmit` to verify; run `docker compose config` after adding the `frontend` service.
- Gateway preflight verify (after `docker compose up`): `curl -i -X OPTIONS http://localhost:8080/catalog/api/books -H "Origin: http://localhost:3000" -H "Access-Control-Request-Method: GET"` → expect `204` + `Access-Control-Allow-Origin: http://localhost:3000`.

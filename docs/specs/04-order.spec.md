# Spec 04: Order Service & Checkout

## Objective
Implement the Order service in `src/Services/Order.Api`: a shopping cart, a checkout flow that deducts stock via Catalog.Api's gRPC `DeductStock`, and an order state machine, backed by EF Core PostgreSQL.

## Requirements
1. **Database Context**:
   - Create `OrderDbContext` in `Order.Api` using EF Core.
   - Reuse the config-driven provider switch so tests can run with `Database:Provider=sqlite`.
   - Define entities (field lists are authoritative; do not add others):
     - `CartItem`: `Id`, `SessionId`, `BookId`, `Title`, `UnitPrice` (decimal), `Quantity` (int).
     - `Order`: `Id`, `CustomerName`, `TotalAmount` (decimal), `Status` (`OrderStatus`), `CreatedAt`, `UpdatedAt`.
     - `OrderItem`: `Id`, `OrderId`, `BookId`, `Title`, `UnitPrice` (decimal), `Quantity` (int), `LineTotal` (decimal).
   - Define `OrderStatus` enum: `Placed`, `Paid`, `Processing`, `Shipped`, `Completed`, `Cancelled`.
   - Ensure schema on startup (`EnsureCreated`); no seed data required.
2. **Cart Endpoints** (`/api/cart`) — the cart is keyed by `SessionId`:
   - `GET /api/cart?sessionId={id}` — cart with its lines and the computed `totalAmount`.
   - `POST /api/cart/items` — body `{ sessionId, bookId, title, unitPrice, quantity }`; adds a line, or increments `Quantity` when the book is already in the cart. `quantity >= 1`; otherwise `400`.
   - `PATCH /api/cart/items/{bookId}?sessionId={id}` — body `{ quantity }`; sets `Quantity` (`>= 1`); `404` if the line is missing.
   - `DELETE /api/cart/items/{bookId}?sessionId={id}` — removes the line; `404` if missing.
   - `DELETE /api/cart?sessionId={id}` — clears the cart.
3. **Checkout** (`POST /api/orders/checkout`):
   - Body `{ sessionId, customerName }`.
   - Empty cart → `400`.
   - For each cart line, call Catalog.Api's gRPC `DeductStock(book_id, quantity)`:
     - Any failure → abort, no order created, return `409`.
     - Compensating restock of already-deducted lines is out of scope (Catalog has no restock RPC yet).
   - On success: create an `Order` (`Status = Placed`) with `OrderItem` snapshots (title/price/line totals), `TotalAmount` = sum of `LineTotal`, clear the cart, return `201` with the order.
   - Wrap the gRPC client behind `IStockDeductionService` so checkout logic is testable with a stub.
4. **Order State Machine**:
   - `GET /api/orders?page=&pageSize=` — paginated list.
   - `GET /api/orders/{id}` — order with its items; `404` if missing.
   - `POST /api/orders/{id}/status` — body `{ status }`; allowed transitions only, otherwise `400`:
     - `Placed → Paid`, `Placed → Cancelled`
     - `Paid → Processing`, `Paid → Cancelled`
     - `Processing → Shipped`
     - `Shipped → Completed`
   - `POST /api/orders/{id}/cancel` — shorthand for `→ Cancelled` from `Placed`/`Paid`; otherwise `400`.
5. **Testing**:
   - Create xUnit integration tests in `tests/Order.Api.Tests` using `WebApplicationFactory<Program>` against SQLite.
   - Substitute `IStockDeductionService` with a configurable stub.
   - Cover the cart: add (new + increment) / update / remove / clear / totals.
   - Cover checkout: success (order created, correct items and total, cart cleared), empty cart → `400`, deduction failure → `409` (no order created).
   - Cover the state machine: valid transitions, invalid transition → `400`, cancel on a non-cancellable status → `400`.

## Rules
- Scope Isolation: ONLY modify files inside `src/Services/Order.Api` and `tests/Order.Api.Tests`.
- The `DeductStock` contract belongs to Catalog.Api; Order.Api consumes it via gRPC as-is (no changes under `src/Services/Catalog.Api`).
- Run `dotnet test` at the end to verify.

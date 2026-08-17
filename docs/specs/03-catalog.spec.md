# Spec 03: Catalog Service & Inventory Management

## Objective
Implement the Catalog service in `src/Services/Catalog.Api`: book CRUD over REST and a gRPC `DeductStock` endpoint for order checkout, backed by EF Core PostgreSQL.

## Requirements
1. **Database Context**:
   - Create `CatalogDbContext` in `Catalog.Api` using EF Core.
   - Configure the PostgreSQL connection and reuse the config-driven provider switch so tests can run with `Database:Provider=sqlite`.
   - Define a `Book` entity: `Id`, `Title`, `Author`, `ISBN`, `Price`, `StockQuantity`, `CreatedAt`, `UpdatedAt`.
   - Seed a small set of books idempotently on startup (e.g. via `EnsureCreated` + seed data).
2. **REST Endpoints (Catalog Management)**:
   - `GET /api/books` — paginated list with optional `keyword` / `category` / `minPrice` / `maxPrice` filters.
   - `GET /api/books/{id}` — single book; return `404` if missing.
   - `POST /api/books` — create; validate non-empty `Title`/`ISBN`, `Price >= 0`, `StockQuantity >= 0`.
   - `PUT /api/books/{id}` — update; return `404` if missing.
   - `DELETE /api/books/{id}` — soft delete (set `IsDeleted`); hidden from list/get.
   - `PATCH /api/books/{id}/stock` — adjust `StockQuantity` (delta, may not go below zero).
3. **gRPC Stock Deduction (`DeductStock`)**:
   - Add gRPC server support to `Catalog.Api` (`Grpc.AspNetCore` + protobuf codegen).
   - Define `catalog.proto` declaring `CatalogService` with `rpc DeductStock(DeductStockRequest) returns (DeductStockResponse)`.
   - `DeductStockRequest`: `book_id` (int32), `quantity` (int32).
   - `DeductStockResponse`: `success` (bool), `remaining_stock` (int32).
   - Deduction must be atomic (transaction / concurrency guard) so concurrent requests cannot oversell.
   - Return `success=false` when stock is insufficient or the book is missing/soft-deleted.
4. **Testing**:
   - Create xUnit integration tests in `tests/Catalog.Api.Tests` using `WebApplicationFactory<Program>` against SQLite.
   - Cover: create / list / get / update / delete / stock-adjust endpoints.
   - Cover `DeductStock`: success path, insufficient-stock failure, and no-oversell under concurrent deductions.

## Rules
- Scope Isolation: ONLY modify files inside `src/Services/Catalog.Api` and `tests/Catalog.Api.Tests`.
- The `DeductStock` gRPC contract lives in `Catalog.Api`; consumption by `Order.Api` is deferred to a later spec.
- Run `dotnet test` at the end to verify.

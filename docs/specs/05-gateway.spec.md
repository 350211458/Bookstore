# Spec 05: API Gateway (YARP Reverse Proxy)

## Objective
Make the YARP gateway in `src/Gateways/ApiGateway` the single public entry point that routes HTTP traffic to the Identity, Catalog, and Order services, so each service's native routes (`/connect/*`, `/api/books*`, `/api/cart*`, `/api/orders*`) are reachable through a per-service base URL. Internal gRPC (`DeductStock`) is intentionally NOT exposed through the gateway.

## Requirements
1. **Reverse Proxy Configuration** (`src/Gateways/ApiGateway/appsettings.json`):
   - Keep the `ReverseProxy` config section: three `Routes` (`identity`, `catalog`, `order`) and three `Clusters` (`identity-api` → `http://identity-api`, `catalog-api` → `http://catalog-api`, `order-api` → `http://order-api`).
   - Cluster destination addresses are resolved through Service Discovery at runtime (AppHost registers the projects as `identity-api`, `catalog-api`, `order-api`).
   - Each route MUST strip its own prefix before forwarding — `"Transforms": [ { "PathRemovePrefix": "/identity" } ]`, `"/catalog"`, `"/order"` respectively. Without this transform the current `/xxx/{**catch-all}` match forwards the prefixed path (e.g. `/catalog/api/books`) to the upstream, which 404s against its native `[Route("api/books")]`; the prefix stripping is what actually "connects" the three services through the gateway.

2. **Public Route Map** (authoritative — upstream native paths are unchanged; the gateway only strips the per-service prefix):
   | Public path (gateway) | Upstream cluster | Upstream native path | Method(s) |
   |---|---|---|---|
   | `/identity/connect/token` | identity-api | `/connect/token` | POST |
   | `/identity/connect/authorize` | identity-api | `/connect/authorize` | GET, POST |
   | `/identity/connect/userinfo` | identity-api | `/connect/userinfo` | GET |
   | `/catalog/api/books` | catalog-api | `/api/books` | GET, POST |
   | `/catalog/api/books/{id}` | catalog-api | `/api/books/{id}` | GET, PUT, DELETE |
   | `/catalog/api/books/{id}/stock` | catalog-api | `/api/books/{id}/stock` | PATCH |
   | `/order/api/cart` (with `?sessionId=`) | order-api | `/api/cart` | GET, DELETE |
   | `/order/api/cart/items` | order-api | `/api/cart/items` | POST |
   | `/order/api/cart/items/{bookId}` | order-api | `/api/cart/items/{bookId}` | PATCH, DELETE |
   | `/order/api/orders` | order-api | `/api/orders` | GET |
   | `/order/api/orders/checkout` | order-api | `/api/orders/checkout` | POST |
   | `/order/api/orders/{id}` | order-api | `/api/orders/{id}` | GET |
   | `/order/api/orders/{id}/status` | order-api | `/api/orders/{id}/status` | POST |
   | `/order/api/orders/{id}/cancel` | order-api | `/api/orders/{id}/cancel` | POST |

3. **Passthrough Semantics**:
   - The gateway performs path-based reverse proxying ONLY: HTTP method, query string, request headers and body are forwarded unchanged; the response status code and body are returned unchanged. No payload transformation, no header injection.
   - A path matching none of the three routes returns gateway `404`.

4. **gRPC Is Not Exposed**:
   - `DeductStock` remains an internal service-to-service call (Order.Api → Catalog.Api, gRPC over the AppHost service endpoints). Do NOT add gRPC/HTTP2 routes to the gateway; the gateway proxies HTTP/1.1 JSON only.

5. **Program Wiring** (`src/Gateways/ApiGateway/Program.cs`):
   - Keep the existing wiring: `AddServiceDefaults()` → `AddReverseProxy().LoadFromConfig(...)` → `MapDefaultEndpoints()` → `MapReverseProxy()`.
   - The gateway is configuration-driven: no controllers or custom middleware beyond the proxy.

6. **Testing**:
   - Create xUnit integration tests in `tests/ApiGateway.Tests` using `WebApplicationFactory<Program>`.
   - Cluster destinations are config-driven, so override `ReverseProxy:Clusters:*:Destinations:*:Address` via `ConfigureAppConfiguration` to point at local in-test stub HTTP upstreams that record the path they receive.
   - Cover:
     - each route family (identity / catalog / order) forwards to its configured cluster;
     - the service prefix is stripped — the stub upstream receives the native path (e.g. `/api/books`, not `/catalog/api/books`);
     - HTTP method and query string pass through unchanged;
     - an unmatched path returns `404`.

## Rules
- Scope Isolation: ONLY modify files inside `src/Gateways/ApiGateway` and `tests/ApiGateway.Tests`.
- Do NOT change the route prefixes of Identity.Api / Catalog.Api / Order.Api — the gateway adapts to the existing native routes via prefix stripping.
- Run `dotnet test` at the end to verify.

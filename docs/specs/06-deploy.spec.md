# Spec 06: Docker Deployment & CI/CD (DevOps)

## Objective
Containerize the four runnable projects (`api-gateway`, `identity-api`, `catalog-api`, `order-api`), stand up the full stack (PostgreSQL + four services) with Docker Compose reusing the same Service Discovery names the gateway already relies on, and add GitHub Actions CI (build + all four test suites) and CD (build/push container images to GHCR + SSH deploy), so every change is automatically built, tested, and deployable as containers.

## Requirements
1. **Dockerfiles** — one multi-stage Dockerfile per runnable project:
   - `src/Gateways/ApiGateway/Dockerfile`
   - `src/Services/Identity.Api/Dockerfile`
   - `src/Services/Catalog.Api/Dockerfile`
   - `src/Services/Order.Api/Dockerfile`
   - Build stage: `mcr.microsoft.com/dotnet/sdk:10.0` — `dotnet publish -c Release -o /app/publish`; each builds its own csproj (the `ServiceDefaults` project reference is restored automatically).
   - Runtime stage: `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` (busybox `wget` ships with Alpine and is used by the compose healthchecks — no `apt-get`/`curl` needed). Copy published output, `ENV ASPNETCORE_HTTP_PORTS=8080`, `EXPOSE 8080`, `ENTRYPOINT ["dotnet", "<assembly>.dll"]`.

2. **Docker Compose** (`docker-compose.yaml` at repo root):
   - Compose service names MUST equal the Service Discovery names exactly (`identity-api`, `catalog-api`, `order-api`, `api-gateway`); Docker's embedded DNS then resolves the gateway's cluster destinations (`http://identity-api`) the same way AppHost does. This name match is what makes the containerized stack behave like the Aspire run — do not rename the services.
   - | compose service | image | ports | environment |
     |---|---|---|---|
     | `postgres` | `postgres:17-alpine` | `5432:5432` | `POSTGRES_USER=postgres`, `POSTGRES_PASSWORD`, `POSTGRES_DB=bookstore` |
     | `identity-api` | built from `src/Services/Identity.Api/Dockerfile` | — | `ConnectionStrings__IdentityDb=Host=postgres;Port=5432;Database=bookstore_identity;Username=postgres;Password=${POSTGRES_PASSWORD}` |
     | `catalog-api` | built from `src/Services/Catalog.Api/Dockerfile` | — | `ConnectionStrings__CatalogDb=...bookstore_catalog...` (same form) |
     | `order-api` | built from `src/Services/Order.Api/Dockerfile` | — | `ConnectionStrings__OrderDb=...bookstore_order...` (same form); `Catalog__GrpcEndpoint=http://catalog-api:8080` |
     | `api-gateway` | built from `src/Gateways/ApiGateway/Dockerfile` | `8080:8080` | cluster addresses resolved via Docker DNS, no override |
   - DB env names match the connection-string keys each `Program.cs` reads (`IdentityDb` / `CatalogDb` / `OrderDb`); the `Database:Provider` switch is left unset so the compose stack runs PostgreSQL.
   - `Catalog__GrpcEndpoint=http://catalog-api:8080` is the config key Order.Api's `IStockDeductionService` requires (spec 04); gRPC works over cleartext HTTP/2 (h2c) on the same Kestrel port, as already proven by the Catalog gRPC integration tests.
   - Healthchecks: services run with `ASPNETCORE_ENVIRONMENT=Development` (the same environment Aspire uses locally) because ServiceDefaults exposes `/health` only in Development; each service healthcheck is `wget -q -O /dev/null http://localhost:8080/health` and `postgres` uses `pg_isready -U postgres`. `depends_on: postgres: condition: service_healthy`; `api-gateway` `depends_on` the three APIs with `condition: service_healthy` so it only starts once upstreams answer `/health`.
   - Data persistence: named volume `postgres_data` mounted at `/var/lib/postgresql/data`.
   - `api-gateway` publishes `8080:8080` — the single public entry point exposing spec 05's `/identity/*`, `/catalog/*`, `/order/*` route map.

3. **GitHub Actions — CI** (`.github/workflows/ci.yml`):
   - Triggers: `push` to `main`, `pull_request` to `main`.
   - Steps: `actions/checkout@v4` → `actions/setup-dotnet@v4` with `dotnet-version: 10.0.x` → `dotnet build DotNet10Bookstore.sln -c Release` → run each test suite. Because the four test projects are intentionally NOT part of the solution (Scope Isolation precedent, specs 02–05), CI must invoke each test csproj explicitly:
     - `dotnet test tests/Identity.Api.Tests/Identity.Api.Tests.csproj -c Release`
     - `dotnet test tests/Catalog.Api.Tests/Catalog.Api.Tests.csproj -c Release`
     - `dotnet test tests/Order.Api.Tests/Order.Api.Tests.csproj -c Release`
     - `dotnet test tests/ApiGateway.Tests/ApiGateway.Tests.csproj -c Release`
   - A failed build or any failed suite fails the workflow.

4. **GitHub Actions — CD** (`.github/workflows/cd.yml`):
   - Trigger: `push` to `main` (after CI passes).
   - Build & push: `docker/login-action@v3` to GHCR using the default `GITHUB_TOKEN` with `packages: write`, then build-push each of the four images tagged `ghcr.io/${{ github.repository }}/bookstore-{identity-api,catalog-api,order-api,api-gateway}:latest` and `:${{ github.sha }}`.
   - Deploy: `appleboy/ssh-action` to the production host — `docker login ghcr` (token from secret), then `docker compose --env-file .env.production pull && docker compose --env-file .env.production up -d`.
   - `.env.production` is NOT committed; it is created on the host from GitHub secrets.

5. **DevOps**:
   - GitHub Actions secrets (referenced in the workflow files, values injected at runtime):
     - `POSTGRES_PASSWORD` — the DB password substituted into `ConnectionStrings__*Db`.
     - `DEPLOY_HOST`, `DEPLOY_USER`, `DEPLOY_SSH_KEY` — SSH target for the CD deploy job.
     - `GHCR_TOKEN` (or the default `GITHUB_TOKEN` with `packages: write`) for image pushes.
   - Commit `.env.example` at root (non-secret template: `POSTGRES_PASSWORD=...`, `IMAGE_TAG=...`); `.env.production` lives only on the deploy host.
   - Observability hook: every service already enables the OTLP exporter when `OTEL_EXPORTER_OTLP_ENDPOINT` is set (ServiceDefaults) — document that a collector endpoint can be injected as a compose env var with no code changes; adding a collector service is out of scope for this spec.
   - Backups: `postgres_data` is the only durable state; document `docker compose exec -T postgres pg_dump -U postgres bookstore_catalog` (and `bookstore_identity`, `bookstore_order`) to a backup location. Restore is out of scope.

6. **Verification**:
   - `docker compose config` — validates compose syntax and variable substitution.
   - `docker compose build` — builds all four images from the Dockerfiles.
   - `dotnet build DotNet10Bookstore.sln` — still 0 warnings / 0 errors (no code changes).
   - Manual (Docker Desktop running): `docker compose up -d`, then exercise the gateway — `POST http://localhost:8080/identity/connect/token`, `GET http://localhost:8080/catalog/api/books`, `GET http://localhost:8080/order/api/cart?sessionId=test` — each must match the spec 05 route map.

## Rules
- Scope Isolation: ONLY create/modify files under `.github/workflows/`, the four `Dockerfile`s under `src/**`, `docker-compose.yaml` at root, and `.env.example`. Do NOT modify any `Program.cs`, `appsettings*.json`, the gateway `ReverseProxy` section, the solution file, existing specs, or test code.
- Keep `src/AppHost` (Aspire) as the interactive development orchestrator; Docker Compose is the containerized deployment path. Both MUST agree on service names (`identity-api`, `catalog-api`, `order-api`, `api-gateway`) so the gateway's cluster destinations resolve identically.
- Run `dotnet build` and `docker compose config` / `docker compose build` at the end to verify.

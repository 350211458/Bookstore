# Spec 01: System Scaffolding & Orchestration

## Objective
Initialize the .NET 10 Solution with .NET Aspire, YARP Gateway, and core empty service shells.

## Required Components
1. **Solution File**: `DotNet10Bookstore.sln` at root.
2. **AppHost**: `src/AppHost` (.NET Aspire Orchestrator).
3. **ServiceDefaults**: `src/ServiceDefaults` (Shared OpenTelemetry and resilience).
4. **API Gateway**: `src/Gateways/ApiGateway` (ASP.NET Core with YARP Reverse Proxy).
5. **Microservice Shells** (Web API projects):
   - `src/Services/Identity.Api`
   - `src/Services/Catalog.Api`
   - `src/Services/Order.Api`

## Rules
- All projects must be added to `DotNet10Bookstore.sln`.
- `AppHost` must reference `Identity.Api`, `Catalog.Api`, `Order.Api`, and `ApiGateway`.
- Execute `dotnet build` at the end to verify.
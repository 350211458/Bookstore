# Spec 02: Identity Service & OIDC (OpenIddict)

## Objective
Implement an OIDC Authorization Server in `src/Services/Identity.Api` using OpenIddict and EF Core PostgreSQL.

## Requirements
1. **Database Context**:
   - Create `IdentityDbContext` in `Identity.Api` using EF Core.
   - Configure PostgreSQL connection for OpenIddict storage.
2. **OpenIddict Configuration**:
   - Enable Authorization Code, Password, and Client Credentials flows.
   - Register endpoints: `/connect/token`, `/connect/authorize`, `/connect/userinfo`.
   - Add development signing and encryption certificates.
3. **Endpoints & Claims**:
   - Implement `/connect/token` to validate user credentials and issue JWT tokens.
   - Embed `sub`, `email`, and `role` (`Customer`, `Admin`) into JWT Claims.
4. **Testing**:
   - Create xUnit integration test in `tests/Identity.Api.Tests` to verify fetching JWT token via `/connect/token`.

## Rules
- Scope Isolation: ONLY modify files inside `src/Services/Identity.Api` and `tests/Identity.Api.Tests`.
- Run `dotnet test` at the end to verify.
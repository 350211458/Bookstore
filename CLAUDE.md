# .NET 10 Microservices Bookstore - Claude Code Action Guidelines

## 1. Project Overview & Tech Stack
This is a production-grade microservices system for an Online Bookstore built on **.NET 10**, leveraging **Vibe Coding & AI Engineering Paradigms**.

- **Orchestration & Service Discovery**: .NET Aspire (`src/AppHost`)
- **API Gateway**: YARP (`src/Gateways/ApiGateway`)
- **Identity & Auth**: OpenIddict / OIDC Server (`src/Services/Identity.Api`)
- **Core Microservices**:
  - `src/Services/Catalog.Api` (Catalog management & gRPC `DeductStock`)
  - `src/Services/Order.Api` (Cart, Checkout & Order State Machine)
- **Database & Cache**: Entity Framework Core 10 (PostgreSQL) + .NET 10 HybridCache
- **Inter-Service Communication**: gRPC (Synchronous) + Refit HTTP Client

---

## 2. Directory & Component Structure
```text
DotNet10Bookstore/
├── CLAUDE.md                    # Global AI action rules (This file)
├── .mcp.json                    # MCP tools server configuration (Postgres, Git, etc.)
├── .claude/                     # Claude Code Custom Skills & Rules
│   ├── commands/                # Custom slash commands (e.g. /check-arch, /gen-tests)
│   └── rules/                   # Fine-grained architectural constraints
├── docs/
│   ├── architecture.md          # Global architecture design
│   └── specs/                   # [SDD] Specification-Driven Development specs
│       ├── identity.spec.md
│       ├── catalog.spec.md
│       └── order.spec.md
├── evals/                       # [Eval Agent & Harness] AI Code Evaluation Suite
│   ├── harness/                 # Context loading and agent environment mock
│   └── run-eval.py              # Automated testing & compliance evaluation script
├── src/
│   ├── AppHost/                 # .NET Aspire Orchestrator
│   ├── ServiceDefaults/         # Shared OpenTelemetry & resiliency extensions
│   ├── Gateways/ApiGateway/     # YARP Reverse Proxy
│   └── Services/                # Isolated Microservices
└── tests/                       # Integration & Unit test suites per service
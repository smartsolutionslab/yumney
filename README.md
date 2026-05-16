# Yumney

**From URL to cook-ready recipe in seconds -- no ads, no blabla.**

Yumney is a Progressive Web App that extracts recipes from any URL using LLM, generates shopping lists, provides step-by-step cooking instructions, and manages your personal recipe collection.

## Tech Stack

| Area | Technology |
|------|-----------|
| Backend | ASP.NET Core 10, Minimal APIs, Clean Architecture |
| Frontend | Angular 19+, Micro-Frontends (Native Federation), Nx Monorepo |
| Database | PostgreSQL 16 (EF Core, Code-First) |
| LLM | Microsoft Semantic Kernel (OpenAI, Anthropic, Azure OpenAI, Ollama) |
| Auth | Keycloak (OIDC, JWT Bearer) via .NET Aspire |
| API Gateway | YARP Reverse Proxy |
| Orchestration | .NET Aspire |
| Validation | FluentValidation + Guard System |
| Event Bus | In-process (swappable to RabbitMQ/MassTransit) |
| Logging | Serilog (structured) |
| API Docs | Scalar (OpenAPI) |
| i18n | Transloco (DE + EN) |
| State | Angular Signals |
| Testing | xUnit, FluentAssertions, NSubstitute, Vitest, Playwright |
| Mutation Testing | Stryker |
| Hosting | Azure (App Service, PostgreSQL Flex, Key Vault) |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- [Yarn](https://yarnpkg.com/)
- [Docker](https://docker.com/) (required -- Aspire runs PostgreSQL, Redis, Keycloak, Ollama, Mailpit via containers)

### Backend

```bash
# Run with Aspire (starts all infrastructure services via Docker)
cd src/Yumney.AppHost
dotnet run
```

Aspire automatically handles:
- PostgreSQL + PgAdmin
- Redis
- Keycloak (with realm import)
- Ollama (LLM provider)
- Mailpit (email testing)
- Database migrations
- API + Gateway startup

### Frontend

```bash
cd client
yarn install
yarn nx serve shell
```

### Development URLs

| Service | URL |
|---------|-----|
| Angular Dev Server | http://localhost:4200 |
| Aspire Dashboard | https://localhost:17298 |
| Scalar API Docs | https://localhost:{api-port}/scalar |
| Keycloak Admin | http://localhost:8080 |
| Mailpit UI | http://localhost:8025 |
| PgAdmin | http://localhost:{pgadmin-port} |

> Ports for Aspire-managed services (API, Gateway, PgAdmin) are assigned dynamically -- check the Aspire Dashboard for exact URLs.

### Running Tests

```bash
# Backend -- all tests
dotnet test

# Backend -- specific project
dotnet test tests/Yumney.Recipes.Domain.Tests

# Frontend -- all tests
cd client && yarn nx run-many -t test

# Frontend -- specific app
cd client && yarn nx test recipes
```

### Connecting Claude / ChatGPT / other MCP clients

Yumney exposes its toolset as an [MCP](https://modelcontextprotocol.io/) server. Setup for Claude.ai (custom connector) and Claude Desktop (`mcp-remote` bridge) lives in [docs/mcp/claude-setup.md](docs/mcp/claude-setup.md).

## Project Structure

```
yumney/
├── src/
│   ├── Yumney.AppHost/                 # .NET Aspire orchestration
│   ├── Yumney.ServiceDefaults/         # Shared Aspire defaults
│   ├── Yumney.Gateway/                 # YARP reverse proxy (CORS, routing)
│   ├── Yumney.Api/                     # API Host (DI, Middleware, Auth)
│   ├── Yumney.MigrationRunner/         # Database migration orchestration
│   ├── Yumney.Shared/                  # Cross-cutting: Guards, Result, Entity
│   │
│   ├── Yumney.Recipes.Domain/          # Recipes -- Domain layer
│   ├── Yumney.Recipes.Application/     # Recipes -- CQRS, DTOs, Interfaces
│   ├── Yumney.Recipes.Infrastructure/  # Recipes -- EF Core, Semantic Kernel
│   ├── Yumney.Recipes.Api/             # Recipes -- Minimal API endpoints
│   │
│   ├── Yumney.Shopping.Domain/         # Shopping -- Domain layer (planned)
│   │
│   ├── Yumney.Users.Domain/            # Users -- Domain layer
│   ├── Yumney.Users.Application/       # Users -- CQRS, DTOs, Interfaces
│   ├── Yumney.Users.Infrastructure/    # Users -- Keycloak, EF Core
│   └── Yumney.Users.Api/              # Users -- Minimal API endpoints
├── tests/                              # Test projects (mirror src structure)
├── client/                             # Angular Nx workspace
│   ├── apps/shell/                     # Host app (routing, layout)
│   ├── apps/recipes/                   # Recipes micro-frontend
│   ├── apps/shopping/                  # Shopping micro-frontend (planned)
│   ├── apps/account/                   # Account micro-frontend (planned)
│   └── libs/
│       ├── shared/                     # Models, API client, Auth, i18n
│       └── ui/                         # Shared UI components
└── .github/workflows/                  # CI/CD
```

## Architecture

**Modular Monolith** with Domain-Driven Design -- each module has four layers: Domain, Application, Infrastructure, Api.

### Module Status

| Module | Status | Description |
|--------|--------|-------------|
| Recipes | Implemented | Import from URL, LLM extraction, CRUD, search, recipe detail |
| Users | Implemented | Keycloak registration, email verification, user profiles |
| Shopping | Planned | Shopping lists, export, sharing (Phase 5) |

### API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/v1/recipes` | List recipes (pagination, sorting) |
| `GET` | `/api/v1/recipes/{identifier}` | Get recipe by ID |
| `POST` | `/api/v1/recipes/import` | Import recipe from URL (LLM extraction) |
| `POST` | `/api/v1/recipes` | Save extracted recipe |
| `POST` | `/api/v1/auth/register` | Register new user |
| `POST` | `/api/v1/auth/resend-verification-email` | Resend verification email |

### Frontend Routes

| Route | Description |
|-------|-------------|
| `/auth/login` | Login page |
| `/auth/register` | Registration page |
| `/auth/resend-verification` | Resend email verification |
| `/dashboard` | Dashboard (protected) |
| `/recipes` | Recipe list (protected) |
| `/recipes/:identifier` | Recipe detail (protected) |

## Development

### Branch Strategy

```
main       → Production (protected, PRs from develop only)
develop    → Staging (protected, PRs from feature branches only)
feature/*  → Feature branches (created from develop, PR target: develop)
```

### Branch Naming

```
feature/US-010-url-input       # New feature
fix/US-041-checkbox-state      # Bug fix
refactor/US-034-search-query   # Refactoring
test/US-011-extraction-tests   # Tests only
chore/US-000-update-deps       # Maintenance
```

### Commit Convention

[Conventional Commits](https://www.conventionalcommits.org/) with user story reference:

```
<type>(<scope>): <description> (US-XXX)

Types: feat, fix, refactor, test, docs, style, chore, perf, ci
Scopes: recipes, shopping, users, account, shared, api, shell, infra, ui
```

Example: `feat(recipes): add URL validation (US-010)`

### Testing

- **Backend:** xUnit + FluentAssertions + NSubstitute, AAA pattern
- **Frontend:** Vitest + Angular Testing Library, Playwright for E2E
- Domain/Application layers require 80-90% coverage (CI enforced)

## Contributing

1. Branch from `develop`: `feature/US-XXX-short-description`
2. Follow [Conventional Commits](https://www.conventionalcommits.org/) -- reference the user story
3. Keep branch up to date -- always rebase on `develop`:
   ```bash
   git fetch origin
   git rebase origin/develop
   ```
4. Create PR targeting `develop` -- ensure CI is green
5. PRs use **rebase and fast-forward only** (no merge commits)

See [CLAUDE.md](CLAUDE.md) for detailed architecture rules and coding guidelines.

## License

Private -- All rights reserved.

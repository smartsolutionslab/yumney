# 🍽️ Yumney

**From URL to cook-ready recipe in seconds – no ads, no blabla.**

Yumney is a Progressive Web App that extracts recipes from any URL using LLM, generates shopping lists, provides step-by-step cooking instructions, and manages your personal recipe collection.

## Tech Stack

| Area | Technology |
|------|-----------|
| Backend | ASP.NET Core 10, Minimal APIs, Clean Architecture |
| Frontend | Angular 19+ Micro-Frontends (Native Federation), Nx |
| Database | PostgreSQL 16 |
| LLM | Microsoft Semantic Kernel (OpenAI, Anthropic, Ollama) |
| Orchestration | .NET Aspire |
| Hosting | Azure |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- [Yarn](https://yarnpkg.com/)
- [Docker](https://docker.com/) (for PostgreSQL via Aspire)

### Backend

```bash
# Restore and build
dotnet restore
dotnet build

# Run with Aspire (starts PostgreSQL + Redis via Docker)
cd src/Yumney.AppHost
dotnet run
```

### Frontend

```bash
cd client
yarn install
yarn nx serve shell
```

### Tests

```bash
# Backend
dotnet test

# Frontend
cd client && yarn nx run-many -t test
```

## Architecture

**Modular Monolith** with three bounded modules:
- **Recipes** – Import, extraction, CRUD, search, portion scaling, cooking mode
- **Shopping** – Shopping lists, export, sharing, Bring! integration
- **Users** – Authentication, profiles, GDPR compliance

See [CLAUDE.md](CLAUDE.md) for detailed architecture rules and coding guidelines.

## Contributing

1. Create a feature branch: `feature/US-XXX-description`
2. Follow [Conventional Commits](https://www.conventionalcommits.org/)
3. Always rebase on main before PR
4. Ensure CI is green

## License

Private – All rights reserved.

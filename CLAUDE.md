# Yumney – Project Rules for Claude Code

## Project Overview

Yumney is a Progressive Web App that extracts recipes from any URL using LLM, generates shopping lists, provides step-by-step cooking instructions, and manages a personal recipe collection. "From URL to cook-ready recipe in seconds – no ads, no blabla."

## Architecture

### Style: Modular Monolith with DDD

- **Backend:** ASP.NET Core 10, Minimal APIs, Clean Architecture, .NET Aspire
- **Frontend:** Angular 19+ Micro-Frontends (Native Federation), Nx Monorepo
- **Database:** PostgreSQL 16 (EF Core, Code-First)
- **LLM:** Microsoft Semantic Kernel (provider-agnostic: OpenAI, Anthropic, Azure OpenAI, Ollama)
- **Hosting:** Azure (App Service, PostgreSQL Flex, Key Vault, Blob Storage, App Insights)

### Monorepo Structure

```
yumney/
├── src/
│   ├── Yumney.AppHost/              # .NET Aspire orchestration
│   ├── Yumney.ServiceDefaults/      # Shared Aspire defaults
│   ├── Yumney.Api/                  # API Host (Startup, DI, Middleware)
│   ├── Yumney.Modules.Recipes/      # Recipes module
│   ├── Yumney.Modules.Shopping/     # Shopping module
│   ├── Yumney.Modules.Users/        # Users module
│   └── Yumney.Shared/              # Cross-cutting concerns
├── tests/
│   ├── Yumney.Modules.Recipes.Tests/
│   ├── Yumney.Modules.Shopping.Tests/
│   ├── Yumney.Modules.Users.Tests/
│   ├── Yumney.Shared.Tests/
│   ├── Yumney.Integration.Tests/
│   └── Yumney.Architecture.Tests/
├── client/                          # Angular Nx workspace
│   ├── apps/shell/                  # Host app
│   ├── apps/recipes/                # Recipes MFE
│   ├── apps/shopping/               # Shopping MFE
│   ├── apps/account/                # Account MFE
│   └── libs/
│       ├── shared/                  # Models, API client, Auth, i18n, State
│       └── ui/                      # Shared UI components (Storybook)
└── .github/workflows/              # CI/CD
```

### Module Structure (per module)

```
Yumney.Modules.{Name}/
├── Domain/
│   └── {Aggregate}/                 # Grouped by aggregate, NOT by type
│       ├── {Aggregate}.cs           # Aggregate Root
│       ├── {Entity}.cs              # Child entities
│       ├── {ValueObject}.cs         # Value Objects
│       ├── Events/                  # Domain events
│       ├── Rules/                   # Business rules
│       └── Handlers/                # Domain handlers
├── Application/
│   ├── Commands/
│   ├── Queries/
│   ├── DTOs/
│   └── Interfaces/
├── Infrastructure/
│   ├── Persistence/
│   └── Services/
└── Api/
    └── {Name}Endpoints.cs           # Minimal API endpoints
```

### Layer Dependencies (STRICT)

```
API → Application → Domain ← Infrastructure

✅ API may call Application
✅ Application may use Domain
✅ Infrastructure implements Domain interfaces
❌ Domain MUST NOT reference anything else
❌ Application MUST NOT access Infrastructure directly
❌ Modules MUST NOT access each other's DB/entities directly
✅ Cross-module communication ONLY via Shared Interfaces / MediatR Notifications
```

## DDD Rules (CRITICAL)

### Value Objects – EVERYWHERE

**RULE: Every business concept gets a Value Object. No primitive types for things with meaning.**

```csharp
// ❌ FORBIDDEN – Primitive Obsession
public string Title { get; }
public int Servings { get; }
public decimal Amount { get; }

// ✅ REQUIRED – Value Objects
public RecipeTitle Title { get; }
public Servings Servings { get; }
public Amount Amount { get; }
```

### Composite Value Objects – NO primitive parameters

```csharp
// ❌ FORBIDDEN
public Quantity(decimal amount, string unit) { }

// ✅ REQUIRED – Value Objects as parameters
public Quantity(Amount amount, Unit unit) { }
```

### Domain Model Structure – by Aggregate, NOT by type

```
// ❌ FORBIDDEN
Domain/Entities/Recipe.cs
Domain/ValueObjects/RecipeTitle.cs
Domain/Events/RecipeImportedEvent.cs

// ✅ REQUIRED
Domain/Recipe/Recipe.cs
Domain/Recipe/RecipeTitle.cs
Domain/Recipe/Events/RecipeImportedEvent.cs
```

Only create subdirectories for: Events/, Handlers/, Rules/

### Natural Method Names

Methods should read like natural language:

```csharp
// ❌ Technical, unnatural
recipe.SetTitle(title);
recipe.UpdateServings(4);

// ✅ Natural language
recipe.RenameAs(title);
recipe.AdjustServingsTo(newServings);
recipe.AddIngredient(name, quantity);
```

### Repository Naming

Interface and class names contain "Repository", but variable names are ALWAYS the plural of the aggregate:

```csharp
// Interface & Class
public interface IRecipeRepository { }
public class RecipeRepository : IRecipeRepository { }

// ❌ FORBIDDEN variable names
IRecipeRepository recipeRepository
IRecipeRepository recipeRepo
IRecipeRepository repository

// ✅ REQUIRED variable names
IRecipeRepository recipes
IShoppingListRepository shoppingLists
IUserRepository users
```

### Use Destructuring and Tuples

```csharp
// ✅ Destructuring Commands
var (url, userId) = command;

// ✅ Tuple return values
public (Amount scaled, Unit adjustedUnit) ScaleBy(Servings original, Servings desired) { }
```

## Guard System – Ensure.That()

**Ensure.That() is the ONLY way for parameter validation in the ENTIRE project.**

Location: `Yumney.Shared.Guards` (no Domain dependency)

```csharp
// ✅ REQUIRED – always use Ensure.That()
Ensure.That(title).IsNotNullOrWhiteSpace().HasMaxLength(200);
Ensure.That(servings).IsPositive();
Ensure.That(url).IsValidUrl();
Ensure.That(ingredients).IsNotEmpty();

// ❌ FORBIDDEN – no bare null checks
if (title == null) throw new ArgumentNullException(nameof(title));
if (string.IsNullOrEmpty(title)) throw new ArgumentException("...");
```

Use .AndReturn() for value extraction in Value Object constructors:
```csharp
public RecipeTitle(string value)
{
    Value = Ensure.That(value).IsNotNullOrWhiteSpace().HasMaxLength(200).AndReturn().Trim();
}
```

## Naming Conventions

### Backend (C#)

| Element | Convention | Example |
|---------|-----------|---------|
| Namespace | PascalCase, mirror project structure | `Yumney.Modules.Recipes.Domain` |
| Class / Record | PascalCase, noun | `RecipeImportHandler` |
| Interface | I + PascalCase | `IRecipeRepository` |
| Method | PascalCase, verb-first | `ExtractRecipeAsync()` |
| Async method | Suffix `Async` | `FindByIdAsync()` |
| Property | PascalCase | `PreparationTimeMinutes` |
| Private field | `_camelCase` | `_context` |
| Local variable | camelCase | `extractedRecipe` |
| Command | Verb + Noun + `Command` | `ImportRecipeCommand` |
| Query | Get/Search + Noun + `Query` | `GetRecipeByIdQuery` |
| Handler | Command/Query + `Handler` | `ImportRecipeCommandHandler` |
| DTO | Suffix `Dto` | `RecipeDetailDto` |
| Validator | Name + `Validator` | `ImportRecipeCommandValidator` |
| Repository var | Plural of aggregate | `recipes`, `shoppingLists` |

### Frontend (Angular/TypeScript)

| Element | Convention | Example |
|---------|-----------|---------|
| Component | kebab-case file, PascalCase class | `recipe-import.component.ts` |
| Service | kebab-case + `.service.ts` | `recipe-api.service.ts` |
| Signal | camelCase | `recipeList = signal<Recipe[]>([])` |
| Observable | Suffix `$` | `recipes$` |
| Event handler | Prefix `on` | `onImportClick()` |
| Component prefix | `yn-` | `<yn-button>` |

### General

- **English identifiers** – code and comments always in English
- **No abbreviations** – except: id, url, dto, i18n
- **Self-documenting code** – comments only for "why", not "what"

## Code Style

### Backend
- `.editorconfig` + `dotnet format` + StyleCop.Analyzers
- File-scoped namespaces
- `var` when type is apparent, explicit type otherwise
- Braces always required
- Max 120 chars per line

### Frontend
- ESLint + @angular-eslint + Prettier
- Single quotes, trailing commas, 2-space indent
- Component prefix: `yn-`
- Max 100 chars per line

### Limits (both)

| Rule | Backend | Frontend |
|------|---------|----------|
| Max lines per file | 300 | 300 |
| Max lines per method | 30 | 50 |
| Max parameters | 4 | 4 |
| Max nesting depth | 3 | 3 |
| Cyclomatic complexity | ≤ 10 | ≤ 10 |

## Error Handling

### Result Pattern for expected errors

```csharp
public async Task<Result<ExtractedRecipeDto>> Handle(ImportRecipeCommand cmd)
{
    var (url, userId) = cmd;
    if (await recipes.ExistsWithUrl(url, userId))
        return Result<ExtractedRecipeDto>.Failure(Errors.Recipe.AlreadyImported);
    // ...
}
```

### Exceptions only for unexpected errors
- GuardException → 400 Bad Request (via Global Exception Handler)
- Custom domain exceptions: `RecipeNotFoundException`, `ExtractionFailedException`
- Global Exception Handler catches unhandled exceptions → 500

### Logging (Serilog)

```csharp
// ✅ Structured properties
_logger.LogInformation("Recipe {RecipeId} imported from {SourceUrl}", id, url);

// ❌ String interpolation
_logger.LogInformation($"Recipe {id} imported from {url}");
```

- Correct log levels: Debug/Information/Warning/Error/Fatal
- Correlation-ID in all entries
- NEVER log sensitive data

## API Design

- RESTful, resource-oriented
- Versioned: `/api/v1/recipes`
- JSON with camelCase properties
- OpenAPI/Swagger documented
- RFC 7807 Problem Details for errors
- Pagination: `?page=1&pageSize=20`
- Plural resources: `/recipes`, `/shopping-lists`
- Kebab-case paths
- No verbs in URLs

### HTTP Status Codes

| Action | Success | Error |
|--------|---------|-------|
| GET list | 200 | - |
| GET single | 200 | 404 |
| POST create | 201 + Location | 400 / 409 |
| PUT update | 200 | 400 / 404 |
| DELETE | 204 | 404 |
| Validation | - | 422 |

## Git Workflow

### ALWAYS REBASE – no merge commits, no squash

```bash
git fetch origin
git rebase origin/main
git push --force-with-lease
```

- `main` is protected – no direct push
- All changes via Pull Request with min. 1 approval
- CI must be green before merge
- PR setting: Rebase and fast-forward ONLY
- Branch naming: `{type}/{US-ID}-{description}`
- Feature branches short-lived (max 3 days)

### Conventional Commits

```
<type>(<scope>): <description>

Types: feat, fix, refactor, test, docs, style, chore, perf, ci
Scopes: recipes, shopping, account, shared, api, shell, infra
```

## Testing

### Backend
- xUnit + FluentAssertions + NSubstitute
- Naming: `{Method}_{Scenario}_{ExpectedResult}`
- AAA pattern (Arrange/Act/Assert)
- One assert per test
- Testcontainers for PostgreSQL integration tests
- Bogus for test data generation

### Frontend
- Jest (via Nx) + Angular Testing Library
- Naming: `it('should {behavior} when {condition}')`
- Cypress/Playwright for E2E
- MSW for API mocking

### Coverage Gates

| Area | Minimum | CI |
|------|---------|-----|
| Domain Layer | 90% | Blocks |
| Application Layer | 80% | Blocks |
| Angular Services | 80% | Blocks |
| Shared Libraries | 90% | Blocks |
| Infrastructure | 50% | Warns |
| Components | 70% | Warns |

## Security

- JWT Bearer Tokens (Access: 15min, Refresh: 7 days)
- Passwords: bcrypt/Argon2id via ASP.NET Core Identity
- HTTPS only + HSTS
- CORS: only allowed origins
- Rate Limiting: 10 imports/minute/user
- Secrets: Azure Key Vault only, NEVER in code
- No token in localStorage – HttpOnly Cookie
- Angular Sanitization: never disable

## Package Manager

- **Backend:** NuGet (standard)
- **Frontend:** Yarn (NOT npm)
- `yarn.lock` is committed, no `package-lock.json`

## i18n

- UI: at least DE + EN from start
- LLM extraction: multi-language support
- All UI strings via i18n keys, never hardcoded

## Key Technologies

| Area | Technology |
|------|-----------|
| ORM | Entity Framework Core |
| CQRS | MediatR |
| Validation | FluentValidation + Ensure.That() |
| LLM | Microsoft Semantic Kernel |
| HTML Parsing | HtmlAgilityPack + AngleSharp |
| Auth | ASP.NET Core Identity + JWT |
| Logging | Serilog |
| Orchestration | .NET Aspire |
| MFE | @angular-architects/native-federation |
| Mono-Repo | Nx |
| UI Docs | Storybook |
| State | Angular Signals |
| i18n | Transloco |

# Yumney – Project Rules for Claude Code

Do not add Co-Authored-By lines to git commit messages.

## Project Overview

Yumney is a Progressive Web App that extracts recipes from any URL using LLM, generates shopping lists, provides step-by-step cooking instructions, and manages a personal recipe collection. "From URL to cook-ready recipe in seconds – no ads, no blabla."

## Architecture

### Style: Modular Monolith with DDD

- **Backend:** ASP.NET Core 10, Minimal APIs, Clean Architecture, .NET Aspire
- **Frontend:** Angular 19+ Micro-Frontends (Native Federation), Nx Monorepo
- **Database:** PostgreSQL 16 (EF Core, Code-First)
- **LLM:** Microsoft Semantic Kernel (provider-agnostic: OpenAI, Anthropic, Azure OpenAI, Ollama)
- **Auth:** Keycloak via .NET Aspire (OIDC, JWT Bearer)
- **Hosting:** Azure (App Service, PostgreSQL Flex, Key Vault, Blob Storage, App Insights)

### Monorepo Structure

```
yumney/
├── src/
│   ├── Yumney.AppHost/              # .NET Aspire orchestration (Keycloak + PostgreSQL + Redis)
│   ├── Yumney.ServiceDefaults/      # Shared Aspire defaults
│   ├── Yumney.Gateway/             # YARP reverse proxy (public entry point, CORS, routing)
│   ├── Yumney.Api/                  # API Host (Startup, DI, Middleware, Auth — internal)
│   ├── Yumney.Shared/              # Cross-cutting: Guards, Result, Entity, AggregateRoot, ICurrentUser
│   │
│   ├── Yumney.Recipes.Domain/      # Value objects, aggregates, events, rules
│   ├── Yumney.Recipes.Application/ # Commands, Queries, DTOs, Interfaces
│   ├── Yumney.Recipes.Infrastructure/ # EF Core, Semantic Kernel, Web scraping
│   ├── Yumney.Recipes.Api/         # Minimal API endpoints
│   │
│   ├── Yumney.Shopping.Domain/
│   ├── Yumney.Shopping.Application/
│   ├── Yumney.Shopping.Infrastructure/
│   ├── Yumney.Shopping.Api/
│   │
│   ├── Yumney.Users.Domain/        # Minimal: AppUserProfile, preferences
│   ├── Yumney.Users.Application/
│   ├── Yumney.Users.Infrastructure/ # CurrentUserProvider, DB persistence
│   └── Yumney.Users.Api/
├── tests/
│   ├── Yumney.Shared.Tests/
│   ├── Yumney.Recipes.Domain.Tests/
│   ├── Yumney.Recipes.Application.Tests/
│   ├── Yumney.Shopping.Domain.Tests/
│   ├── Yumney.Shopping.Application.Tests/
│   ├── Yumney.Users.Domain.Tests/
│   ├── Yumney.Users.Application.Tests/
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

### Module Structure (4 projects per module)

```
Yumney.{Module}.Domain/
└── {Aggregate}/                     # Grouped by aggregate, NOT by type
    ├── {Aggregate}.cs               # Aggregate Root
    ├── {Entity}.cs                  # Child entities
    ├── {ValueObject}.cs             # Value Objects
    ├── Events/                      # Domain events
    ├── Rules/                       # Business rules
    └── Handlers/                    # Domain handlers

Yumney.{Module}.Application/
├── Commands/
├── Queries/
├── DTOs/
└── Interfaces/

Yumney.{Module}.Infrastructure/
├── Persistence/
└── Services/

Yumney.{Module}.Api/
└── {Module}Endpoints.cs             # Minimal API endpoints
```

### Layer Dependencies (STRICT — enforced at compile time via ProjectReference)

```
Domain         → Shared only (PURE — no other dependencies)
Application    → Domain + Shared (+ FluentValidation)
Infrastructure → Application + Domain + Shared (+ EF Core, external libs)
Api            → Application + Shared only (NOT Domain directly, NOT Infrastructure)

Yumney.Api (host) → all *.Api + all *.Infrastructure (composition root)
Yumney.Gateway    → ServiceDefaults + YARP (reverse proxy, CORS — no domain logic)
Yumney.AppHost    → Yumney.Api + Yumney.Gateway + ServiceDefaults (Aspire orchestration)
```

```
❌ Domain MUST NOT reference Application, Infrastructure, or Api
❌ Application MUST NOT access Infrastructure directly
❌ Modules MUST NOT access each other's DB/entities directly
✅ Cross-module communication ONLY via Shared Interfaces / Domain Events
```

### Event Communication

Two event types with distinct purposes:

- **Domain Events** (`IDomainEvent` → `IDomainEventHandler<T>`) — within a module, dispatched by `IDomainEventDispatcher` after EF Core `SaveChanges`. Example: `RecipeImportedEvent` triggers side effects inside the Recipes module.
- **Integration Events** (`IIntegrationEvent` → `IIntegrationEventHandler<T>`) — cross-module, published via `IEventBus`. Example: Recipes module publishes → Shopping module subscribes.

In-process implementations (`InProcessDomainEventDispatcher`, `InProcessEventBus`) resolve handlers from DI. Register via `builder.Services.AddInProcessEventBus()` in the composition root. Swappable to RabbitMQ/MassTransit later without changing module code.

### Centralized Package Management

All NuGet package versions are defined once in `Directory.Packages.props` at the repo root. Individual `.csproj` files use `<PackageReference Include="..." />` **without Version** attributes.

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

### Value Objects in Commands, Events, and Service Interfaces

Commands, events, and service/repository interfaces MUST use value object types
for all business-meaningful parameters. No primitive types (`string`, `int`, etc.)
for concepts that carry domain meaning.

API endpoints receive request DTOs (primitives), validate via FluentValidation,
then map to commands with value objects.

```csharp
// ❌ FORBIDDEN – Primitives in commands
public sealed record RegisterUserCommand(string Email, string Password, string DisplayName);

// ✅ REQUIRED – Value objects in commands
public sealed record RegisterUserRequest(string Email, string Password, string DisplayName); // API DTO
public sealed record RegisterUserCommand(Email Email, Password Password, DisplayName DisplayName); // Command
```

```csharp
// ❌ FORBIDDEN – Primitives in service interfaces
Task<Result<string>> CreateUserAsync(string email, string password, string displayName);

// ✅ REQUIRED – Value objects in service interfaces
Task<Result<KeycloakUserId>> CreateUserAsync(Email email, Password password, DisplayName displayName);
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
| Namespace | PascalCase, mirror project structure | `Yumney.Recipes.Domain` |
| Class / Record | PascalCase, noun | `RecipeImportHandler` |
| Interface | I + PascalCase | `IRecipeRepository` |
| Method | PascalCase, verb-first | `ExtractRecipeAsync()` |
| Async method | Suffix `Async` | `FindByIdAsync()` |
| Property | PascalCase | `PreparationTimeMinutes` |
| Private field | `camelCase` | `context` |
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
- **No abbreviations** – except: url, dto, i18n
- **Use `Identifier` not `Id`** – Value objects and properties use full word: `RecipeIdentifier`, not `RecipeId`
- **Self-documenting code** – comments only for "why", not "what"

## Code Style

### Backend
- `.editorconfig` + `dotnet format` + StyleCop.Analyzers
- File-scoped namespaces
- `var` when type is apparent, explicit type otherwise
- Braces required for multi-line blocks; single-line `if` with `return` may omit braces
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
- BusinessRuleValidationException → 422 Unprocessable Entity
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
- OpenAPI documented, Scalar UI for dev
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

### Endpoint Code Organization

Endpoints use **local static functions** colocated with their route mapping inside the `Map*Endpoints` method. Each route definition is immediately followed by its handler function.

```csharp
// ✅ REQUIRED — local function colocated with route
public static IEndpointRouteBuilder MapRecipesEndpoints(this IEndpointRouteBuilder app)
{
    var group = app.MapGroup("/recipes");

    group.MapPost("/", SaveRecipe)
        .WithName("SaveRecipe")
        .WithTags("Recipes")
        .Produces<SavedRecipeDto>(StatusCodes.Status201Created);

    static async Task<IResult> SaveRecipe(
        SaveRecipeRequest request,
        IValidator<SaveRecipeRequest> validator,
        ICommandHandler<SaveRecipeCommand, Result<SavedRecipeDto>> handler,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (validation.HasFailed()) return validation.ToValidationProblem();

        var (title, ingredients, steps, description, servings, timing, difficulty, imageUrl) = request;
        var command = new SaveRecipeCommand(title, ingredients, steps, description, servings, timing, difficulty, imageUrl);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToCreated($"/api/v1/recipes/{result.Value.Identifier}");
    }

    // next route + handler ...

    return app;
}
```

```csharp
// ❌ FORBIDDEN — separate private static methods at class level
public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
{
    app.MapPost("/", SaveAsync); // handler defined far away
    return app;
}
private static async Task<IResult> SaveAsync(...) { ... } // disconnected from route
```

### Request DTO → Value Object Conversion

Request records with **different arity** after VO conversion add a custom `Deconstruct` method that returns value objects directly. Endpoints destructure with `var (...) = request;`.

```csharp
// ✅ Deconstruct — when arity differs (e.g., PrepTime + CookTime → Timing)
public sealed record SaveRecipeRequest(string Title, ..., int? PrepTimeMinutes, int? CookTimeMinutes, ...)
{
    public void Deconstruct(out RecipeTitle title, ..., out TimingInfo? timing, ...)
    {
        title = RecipeTitle.From(Title);
        timing = TimingInfo.FromNullable(
            PreparationTime.FromNullable(PrepTimeMinutes),
            CookingTime.FromNullable(CookTimeMinutes));
    }
}

// In endpoint:
var (title, ingredients, steps, description, servings, timing, difficulty, imageUrl) = request;
```

Request records with **same arity** (no field merging) use `ToValueObjects()` to avoid ambiguity with the auto-generated `Deconstruct`.

```csharp
// ✅ ToValueObjects — when arity matches (same number of out params)
public sealed record RegisterUserRequest(string Email, string Password, string DisplayName)
{
    public (Email Email, Password Password, DisplayName DisplayName) ToValueObjects() =>
        (Email.From(Email), Password.From(Password), DisplayName.From(DisplayName));
}

// In endpoint:
var (email, password, displayName) = request.ToValueObjects();
```

Request records that already use **domain types** (no VO conversion needed) use the auto-generated record `Deconstruct` directly.

```csharp
// ✅ Auto-generated Deconstruct — when request uses domain types
public sealed record AssignRecipeRequest(DayOfWeek Day, Guid RecipeIdentifier, string RecipeTitle, MealType MealType);

// In endpoint:
var (day, recipeIdentifier, recipeTitle, mealType) = request;
```

Single-property requests keep `request.Property` access (C# doesn't support single-element destructuring).

## Git Workflow

### Branch Strategy

```
main          → Production (protected, deploy to production)
develop       → Staging (protected, deploy to staging)
feature/*     → Feature branches (PR target: develop)
```

- `main` is protected – no direct push, only PRs from `develop`
- `develop` is protected – no direct push, only PRs from feature branches
- Merge to `develop` triggers CI/CD pipeline → **Staging**
- Merge to `main` triggers CI/CD pipeline → **Production**
- Feature branches are created from and merged back into `develop`

### Developer Workflow

Every piece of work follows this process:

#### 1. Pick a User Story
- Select a story from the GitHub Project board (e.g. `US-010`)
- Assign the issue to yourself → status **In Progress**

#### 2. Create Feature Branch
- Always branch from `develop`
- Branch naming: `{type}/{US-ID}-{short-description}`

```bash
git checkout develop
git pull origin develop
git checkout -b feat/US-010-url-input
```

#### 3. Implement
- Follow all architecture, DDD, and code style rules in this document
- Write tests alongside implementation (TDD encouraged)
- Commit often using Conventional Commits
- Reference the story in commits: `feat(recipes): add URL validation (US-010)`

#### 4. Keep Branch Up to Date (ALWAYS REBASE)

```bash
git fetch origin
git rebase origin/develop
git push --force-with-lease
```

- **No merge commits, no squash**
- Resolve conflicts during rebase

#### 5. Create Pull Request → `develop`
- PR title: `feat(scope): short description (US-XXX)`
- PR body: link to issue, summary of changes, test plan
- Assign reviewer, link the GitHub issue
- CI must be green before merge
- Min. 1 approval required
- PR setting: **Rebase and fast-forward ONLY**

#### 6. After Merge
- Feature branch is deleted
- Issue is moved to **Done** on the project board
- Changes are deployed to **Staging** automatically

#### 7. Release to Production
- PR from `develop` → `main`
- After merge → deployed to **Production**

### Branch Naming

```
feat/US-010-url-input          # New feature
fix/US-041-checkbox-state      # Bug fix
refactor/US-034-search-query   # Refactoring
test/US-011-extraction-tests   # Tests only
chore/US-000-update-deps       # Maintenance
```

### Conventional Commits

```
<type>(<scope>): <description> (US-XXX)

Types: feat, fix, refactor, test, docs, style, chore, perf, ci
Scopes: recipes, shopping, users, account, shared, api, shell, infra, ui
```

### Rules
- Feature branches are short-lived (max 3 days)
- One story per feature branch – no mixing
- No direct pushes to `develop` or `main`
- Every PR must reference its user story issue

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

- **Auth:** Keycloak (OIDC) via .NET Aspire integration
- JWT Bearer Tokens validated against Keycloak
- `ICurrentUser` in Shared, implemented by `CurrentUserProvider` in Users.Infrastructure (reads JWT claims)
- Users module syncs Keycloak claims → `AppUserProfile` (KeycloakUserId, DisplayName, PreferredLanguage, PreferredUnitSystem)
- HTTPS only + HSTS
- CORS: only allowed origins (enforced at Gateway, not API)
- Rate Limiting: 10 imports/minute/user
- Secrets: Azure Key Vault only, NEVER in code
- No token in localStorage – HttpOnly Cookie
- Angular Sanitization: never disable

## Package Manager

- **Backend:** NuGet with centralized package management (`Directory.Packages.props`)
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
| CQRS | Manual (ICommandHandler / IQueryHandler interfaces) |
| Event Bus | In-process (IDomainEventDispatcher + IEventBus), swappable to RabbitMQ/MassTransit |
| Validation | FluentValidation + Ensure.That() |
| LLM | Microsoft Semantic Kernel |
| HTML Parsing | HtmlAgilityPack + AngleSharp |
| Auth | Keycloak (OIDC via .NET Aspire) |
| API Docs | Scalar |
| Logging | Serilog |
| API Gateway | YARP (Reverse Proxy) |
| Orchestration | .NET Aspire |
| MFE | @angular-architects/native-federation |
| Mono-Repo | Nx |
| UI Docs | Storybook |
| State | Angular Signals |
| i18n | Transloco |

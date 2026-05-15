# Yumney – Project Rules for Claude Code

Do not add Co-Authored-By lines to git commit messages.

## Project Overview

Yumney is a Progressive Web App that extracts recipes from any URL using LLM, generates shopping lists, provides step-by-step cooking instructions, and manages a personal recipe collection. "From URL to cook-ready recipe in seconds – no ads, no blabla."

## Architecture

### Style: Modular monolith with per-module hosts (Aspire-orchestrated)

Each module ships its own ASP.NET Core host (`Yumney.{Module}.Api`). They run as independent processes — one per module — orchestrated locally and in production by .NET Aspire (`Yumney.AppHost`). A single YARP reverse proxy (`Yumney.Gateway`) is the public entry point and fans out to the module hosts. EF migrations across all module DBs are run by a dedicated one-shot worker (`Yumney.MigrationRunner`).

The "monolith" framing is intentional: modules share the same repo, the same release pipeline, and the same Aspire deployment unit. Each module owns its own database; cross-module communication is the consumer-defined-contracts / integration-events pattern documented below — never direct DbContext access. See [docs/adr/0001-persistence-paradigm-per-aggregate.md](docs/adr/0001-persistence-paradigm-per-aggregate.md) for the rationale on why Shopping is event-sourced while the rest stay state-based.

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
│   ├── Yumney.AppHost/              # .NET Aspire orchestration (Postgres x4, Keycloak, Redis, RabbitMQ, all module hosts, Gateway)
│   ├── Yumney.ServiceDefaults/      # Shared Aspire host defaults (telemetry, health, resilience)
│   ├── Yumney.Gateway/              # YARP reverse proxy (public entry point, CORS, routing to module hosts)
│   ├── Yumney.MigrationRunner/      # One-shot worker that runs EF migrations across all module DBs at startup
│   ├── Yumney.Shared/               # Cross-cutting helpers (EnumExtensions, ICurrentUser-style abstractions)
│   ├── Yumney.Shared.Abstractions/  # Domain primitives: AggregateRoot, Entity, IDomainEvent, IValueObject, …
│   ├── Yumney.Shared.Outcomes/      # Result / Result<T> / ApiError
│   ├── Yumney.Shared.Paging/        # Page, PageSize, PagedResult, SortingOptions
│   ├── Yumney.Shared.Quantities/    # IngredientCategory, Freshness, ShelfLife, QuantityRounder
│   ├── Yumney.Shared.Persistence/   # IUnitOfWork, EF event-store + inbox helpers
│   ├── Yumney.Shared.Events/        # IEventBus, IIntegrationEvent / IModuleEvent / IDomainEvent + handler interfaces (abstractions only)
│   ├── Yumney.Shared.Events.Contracts/ # Concrete cross-module integration event records (RecipeImported, RecipeDeleted, MealConfirmed, …)
│   ├── Yumney.Shared.Events.InProcess/ # InProcessDomainEventDispatcher (single-host swap-in)
│   ├── Yumney.Shared.Events.Wolverine/ # Wolverine + RabbitMQ implementation of IEventBus / consumers
│   ├── Yumney.Shared.CQRS/          # ICommandHandler / IQueryHandler + logging decorators
│   ├── Yumney.Shared.Guards/        # Ensure.That(...) parameter-validation DSL
│   ├── Yumney.Shared.Web/           # Per-host wiring: AddYumneyDefaults, ResultExtensions, ValidationExtensions, etc.
│   │
│   ├── Yumney.Recipes.Domain/       # Value objects, aggregates, events, rules
│   ├── Yumney.Recipes.Application/  # Commands, Queries, DTOs, Interfaces, integration-event handlers
│   ├── Yumney.Recipes.Infrastructure/ # EF Core, web scraping, Recipes-specific persistence
│   ├── Yumney.Recipes.Extraction/   # Semantic Kernel chat/extraction adapters
│   ├── Yumney.Recipes.Api/          # ASP.NET Core host — Recipes Minimal API
│   │
│   ├── Yumney.Shopping.Domain/      # Event-sourced (see ADR 0001)
│   ├── Yumney.Shopping.Application/
│   ├── Yumney.Shopping.Infrastructure/ # Event store (ShoppingDbContext) + read model (ShoppingReadDbContext)
│   ├── Yumney.Shopping.Api/         # ASP.NET Core host — Shopping Minimal API
│   ├── Yumney.Shopping.Host/        # Aspire host project (composition root for the Shopping process)
│   │
│   ├── Yumney.MealPlan.Domain/
│   ├── Yumney.MealPlan.Application/
│   ├── Yumney.MealPlan.Infrastructure/
│   ├── Yumney.MealPlan.Api/         # ASP.NET Core host — MealPlan Minimal API
│   │
│   ├── Yumney.Users.Domain/         # Minimal: AppUserProfile, preferences, activity
│   ├── Yumney.Users.Application/
│   ├── Yumney.Users.Infrastructure/ # CurrentUserProvider, Keycloak admin, DB persistence
│   ├── Yumney.Users.Api/            # ASP.NET Core host — Users Minimal API
│   └── Yumney.Users.Host/           # Aspire host project (composition root for the Users process)
├── tests/
│   ├── Yumney.Shared.Tests/                  # Shared.* libs aggregate test project
│   ├── Yumney.Shared.Guards.Tests/
│   ├── Yumney.Shared.Events.Tests/
│   ├── Yumney.Recipes.Domain.Tests/
│   ├── Yumney.Recipes.Application.Tests/
│   ├── Yumney.Recipes.Infrastructure.Tests/
│   ├── Yumney.Recipes.Api.Tests/
│   ├── Yumney.Shopping.Domain.Tests/
│   ├── Yumney.Shopping.Application.Tests/
│   ├── Yumney.Shopping.Infrastructure.Tests/
│   ├── Yumney.Shopping.Api.Tests/
│   ├── Yumney.MealPlan.Domain.Tests/
│   ├── Yumney.MealPlan.Application.Tests/
│   ├── Yumney.MealPlan.Infrastructure.Tests/
│   ├── Yumney.Users.Domain.Tests/
│   ├── Yumney.Users.Application.Tests/
│   ├── Yumney.Users.Infrastructure.Tests/
│   ├── Yumney.Users.Api.Tests/
│   ├── Yumney.Integration.Tests/             # Aspire-fixture-driven cross-module + contract tests
│   └── Yumney.Architecture.Tests/            # CLAUDE.md rule enforcement (layer deps, naming, void methods, …)
├── docs/
│   └── adr/                                  # Architecture Decision Records (start with 0001)
├── client/                                   # Angular Nx workspace
│   ├── apps/shell/                           # Host app (Native Federation shell)
│   ├── apps/recipes/                         # Recipes MFE
│   ├── apps/shopping/                        # Shopping MFE
│   ├── apps/account/                         # Account MFE
│   └── libs/
│       ├── shared/                           # Models, API client, Auth, i18n, State
│       └── ui/                               # Shared UI components (Storybook)
└── .github/workflows/                        # CI/CD
```

Per-module databases (`recipesdb`, `shoppingdb`, `usersdb`, `mealplandb`) are provisioned as separate Postgres resources in the AppHost. Cross-module queries are forbidden at the persistence layer — see "Cross-Module Dependencies" below for the supported communication shapes.

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

Yumney.{Module}.Api (host) → Yumney.{Module}.Application + Yumney.{Module}.Infrastructure + Shared.Web (composition root for that module's process)
Yumney.Gateway             → ServiceDefaults + YARP (reverse proxy, CORS — no domain logic)
Yumney.MigrationRunner     → all *.Infrastructure (one-shot migrator across module DBs)
Yumney.AppHost             → all *.Api + Gateway + MigrationRunner + ServiceDefaults (Aspire orchestration of the full topology)
```

```
❌ Domain MUST NOT reference Application, Infrastructure, or Api
❌ Application MUST NOT access Infrastructure directly
❌ Modules MUST NOT access each other's DB/entities directly
✅ Cross-module communication ONLY via consumer-defined contracts (synchronous reads/writes) or Integration Events (asynchronous side effects)
```

### Cross-Module Dependencies

When module **A** needs data or behaviour from module **B**, the contract is **owned by the consumer (A)**, not by `Yumney.Shared`. `Shared` cannot reference any module's `Domain`, so contracts living in Shared are forced to use primitives — which violates the "Value Objects in Service Interfaces" rule above.

The pattern, per provider:

```
Yumney.{Consumer}.Application/
  Interfaces/I{Provider}{What}.cs        → contract, using consumer's Domain VOs
  DTOs/{Provider}{What}Result.cs         → consumer-flavoured result DTO

Yumney.{Consumer}.Infrastructure/
  ExternalServices/Http{Provider}{What}.cs → adapter; today calls
                                              the provider's REST endpoint,
                                              tomorrow could be in-process
```

Concrete example — MealPlan needs a recipe's ingredients:

```csharp
// in Yumney.MealPlan.Application/Interfaces/IRecipeIngredientLookup.cs
public interface IRecipeIngredientLookup
{
    Task<IReadOnlyList<RecipeIngredientLookupResult>> LookupAsync(
        SlotRecipeIdentifier recipe,
        CancellationToken cancellationToken = default);
}
```

Rules:

- **The provider doesn't know who consumes it.** Recipes exposes its REST endpoint (`GET /api/v1/recipes/{id}`); MealPlan, Shopping, etc. each define their own `IRecipeIngredientLookup` shape if they need to read ingredients. No shared "lookup" interface tries to satisfy everyone.
- **No cross-module project references** between `*.Application` projects. The adapter calls the provider over HTTP (or another transport) — not via a direct project reference.
- **Transport is hidden in the adapter.** A consumer can swap `Http{Provider}{What}` for an `InProcess{Provider}{What}` (when the runtime topology collapses to a single host) without touching any handler.
- **`Yumney.Shared.Common` does NOT host cross-module service interfaces.** It's reserved for primitive cross-cutting types (`Result`, `Guard`, `ICurrentUser`, paging helpers).
- **Async cross-module side effects use Integration Events**, not contracts. See the next subsection.

### Event Communication

Three event types with distinct purposes:

- **Domain Events** (`IDomainEvent` → `IDomainEventHandler<T>`) — within a module, dispatched by `IDomainEventDispatcher` after EF Core `SaveChanges`. Example: `RecipeImportedEvent` triggers side effects inside the Recipes module.
- **Integration Events** (`IIntegrationEvent` → `IIntegrationEventHandler<T>`) — **cross-module** public contract, published via `IEventBus`. Concrete event records live in `Yumney.Shared.Events.Contracts` (namespace `SmartSolutionsLab.Yumney.Shared.Events.Contracts`); the `IIntegrationEvent` / `IntegrationEvent` base + handler interfaces stay in `Yumney.Shared.Events`. Fields must be primitives or shared types only — never a module's Domain types. Producer + consumer projects (`*.Application`, `*.Infrastructure`) reference `Yumney.Shared.Events.Contracts` directly; `*.Domain` MUST NOT (enforced by `LayerDependencyTests.Domain_ShouldNotDependOn_EventsContracts`). Example: Recipes publishes `RecipeDeletedIntegrationEvent` → Shopping subscribes.
- **Module Events** (`IModuleEvent` → `IModuleEventHandler<T>`) — **in-module** bus envelope wrapping a Domain event so projection handlers can subscribe asynchronously. Live in `Yumney.{Module}.Infrastructure/Persistence/EventStore/Events/` and named `*ModuleEvent`. They MUST NOT cross module boundaries; the wrapper exists so the read-model pipeline can ride the same bus as integration events. Example: `EfCoreShoppingEventStore` publishes `ShoppingItemConsumedModuleEvent` → `ShoppingLedgerProjectionHandler` updates the read model.

Both `IIntegrationEvent` and `IModuleEvent` extend `IBusEvent`, the common tag carried by anything `IEventBus.PublishAsync(...)` accepts. Use the specialised marker on concrete types — never `IBusEvent` directly.

In-process implementations (`InProcessDomainEventDispatcher`, `InProcessEventBus`) resolve handlers from DI. The dispatcher is always registered via `AddInProcessDomainEventDispatcher()`; the bus is registered via `AddInProcessEventBus()` only when no distributed bus is in play (today, `AddWolverineEventBus(...)` in `AddYumneyDefaults` provides `IEventBus` for production hosts). Swappable to RabbitMQ/MassTransit later without changing module code.

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

### Domain Methods – Always Return a Value

**RULE: Domain aggregate and entity methods MUST return a value. No `void` methods.**

Aggregate methods return the aggregate itself (`this`) for fluent chaining.
Entity `internal` methods return the entity itself (`this`).
This makes handler code more expressive and enables method chaining.

```csharp
// ❌ FORBIDDEN – void methods on aggregates/entities
public void AssignRecipe(DayOfWeek day, Guid recipeId, string title) { ... }
public void CheckOffItem(ShoppingListItemIdentifier itemId) { ... }
internal void MarkAsCooked() { State = MealState.Cooked; }

// ✅ REQUIRED – return self for fluent chaining
public WeeklyPlan AssignRecipe(DayOfWeek day, Guid recipeId, string title) { ... return this; }
public ShoppingList CheckOffItem(ShoppingListItemIdentifier itemId) { ... return this; }
internal MealSlot MarkAsCooked() { State = MealState.Cooked; return this; }
```

**Exceptions:**
- `AggregateRoot` / `EventSourcedAggregate` base class infrastructure methods (`ClearDomainEvents`, `RaiseEvent`, `AddDomainEvent`) may remain `void`
- Private event handlers in event-sourced aggregates (`OnItemAdded`, `OnItemBought`, etc.) may remain `void`
- Repository methods (`AddAsync`, `SaveChangesAsync`) are infrastructure, not domain — `Task` return is fine

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

### Lambda parameters and locals — no single-letter names

Lambda parameters, `foreach` iterators, and local variables must be named for what they hold. Single-letter names (`x`, `i`, `s`, `r`, `e`) are forbidden — they encode no information at the call site.

```csharp
// ❌ FORBIDDEN
recipes.Where(x => x.IsFavorite).Select(r => r.ToDto());
foreach (var i in ingredients) { … }
catch (DbUpdateException e) { … }

// ✅ REQUIRED
recipes.Where(recipe => recipe.IsFavorite).Select(recipe => recipe.ToDto());
foreach (var ingredient in ingredients) { … }
catch (DbUpdateException dbException) { … }
```

The only acceptable single-letter local is `_` for an explicitly discarded value (`out _`, `(_, value) = tuple`).

### Identifier-typed variable names

When a variable holds a value object whose **type** ends in `Identifier` (e.g. `RecipeIdentifier`, `OwnerIdentifier`, `ShoppingListIdentifier`), the **variable name** drops the `Identifier` suffix:

```csharp
// ❌ FORBIDDEN — variable name parrots the type
RecipeIdentifier recipeIdentifier = …;
OwnerIdentifier ownerIdentifier = …;
public void AssignRecipe(RecipeIdentifier recipeIdentifier, …) { }

// ✅ REQUIRED — variable name is the domain noun
RecipeIdentifier recipe = …;
OwnerIdentifier owner = …;
public void AssignRecipe(RecipeIdentifier recipe, …) { }
```

When the surrounding code already has a variable called `recipe` (the aggregate or DTO), use `recipeRef` for the identifier — never `recipeId` / `recipeIdentifier`:

```csharp
public void Cook(Recipe recipe, RecipeIdentifier recipeRef) { … }
```

The TYPE name `RecipeIdentifier` is mandated by the rule above; only the variable/parameter name shifts.

### Test naming — no `sut`

Test fixtures and locals use the type name (lowercased), not the conventional `sut`:

```csharp
// ❌ FORBIDDEN
var sut = new ImportRecipeCommandHandler(...);
sut.Handle(command);

// ✅ REQUIRED
var handler = new ImportRecipeCommandHandler(...);
handler.HandleAsync(command);
```

Method-name pattern (`{Method}_{Scenario}_{ExpectedResult}`) is unchanged.

### API request types — no `Request` suffix

API request DTOs in `*.Api/Requests/` and their FluentValidation
validators in `*.Api/Requests/Validator/` carry **no `Request` suffix or
infix**. The `Requests` namespace already conveys "this is an API
request DTO"; repeating it on every type name is noise.

```csharp
// ❌ FORBIDDEN
public sealed record ImportRecipeRequest(string Url);
public sealed class ImportRecipeRequestValidator : AbstractValidator<ImportRecipeRequest> { … }

// ✅ REQUIRED
public sealed record ImportRecipe(string Url);
public sealed class ImportRecipeValidator : AbstractValidator<ImportRecipe> { … }
```

**Call-site qualification by location:**

| Code lives in | Qualification | Example |
|---|---|---|
| `*.Api/{Module}Endpoints.cs` | `using Requests = SmartSolutionsLab.Yumney.{Module}.Api.Requests;` then `Requests.X` | `Requests.ImportRecipe request, IValidator<Requests.ImportRecipe> validator` |
| `*.Api/Requests/` (request types themselves) | unqualified — same namespace | `List<SaveRecipeIngredient> Ingredients` |
| `*.Api/Requests/Validator/` | unqualified — sibling namespace, no ambiguity | `AbstractValidator<SaveRecipe>` |
| `*.Api.Tests/Requests/...` | partial qualifier `Api.Requests.X` resolved via parent-namespace lookup | `var request = new Api.Requests.ImportRecipe(...);` |

**Why the test files use `Api.Requests.X` instead of the alias:** the test
class's containing namespace ends in `Requests` (`SmartSolutionsLab.Yumney.{Module}.Api.Tests.Requests`),
so a `using Requests = ...Api.Requests;` alias would shadow the local
namespace and the compiler would refuse to resolve type members. The
partial qualifier `Api.Requests.X` resolves cleanly via the parent
namespace `SmartSolutionsLab.Yumney.{Module}.Api`.

## Code Style

### Backend
- `.editorconfig` + `dotnet format` + StyleCop.Analyzers
- File-scoped namespaces
- Max 140 chars per line

#### Braces – ALWAYS required, except short guard clauses

`for`, `foreach`, `while`, `using`, `lock` — **always require braces**, no exceptions.

`if` — braces required, **except** single-line guard clauses that exit immediately:

```csharp
// ✅ ALLOWED – short guard clause on one line, exits immediately
if (plan is null) return this;
if (!IsValid) throw new InvalidOperationException();
if (items.Count == 0) continue;
if (IsExtendedMode) return this;

// ✅ REQUIRED – braces on if with body logic
if (servings.HasValue)
{
    Servings = servings.Value;
}

// ❌ FORBIDDEN – multi-statement or non-exit without braces
if (condition)
    DoSomething();

// ❌ FORBIDDEN – foreach/for without braces
foreach (var item in items)
    item.Check();

// ✅ REQUIRED
foreach (var item in items)
{
    item.Check();
}
```

#### Type declarations – `var` vs explicit type

`var` is allowed when the type is apparent from the right-hand side.
**Explicit type is REQUIRED for collection initializations with `[]`:**

```csharp
// ❌ FORBIDDEN – var with collection expression, type is hidden
var items = new[] { "a", "b" };
var slots = new List<MealSlot>();
var tags = new[] { tag1, tag2 };

// ✅ REQUIRED – explicit type with [] collection expressions
List<MealSlot> slots = [];
List<string> items = ["a", "b"];
Dictionary<string, int> counts = [];

// ✅ ALLOWED – var when type is apparent from right-hand side
var plan = WeeklyPlan.Create(owner, week);
var result = await handler.HandleAsync(command);
var slot = FindSlot(day, mealType);
```

### Frontend
- ESLint + @angular-eslint + Prettier
- Single quotes, trailing commas, 2-space indent
- Component prefix: `yn-`
- Max 140 chars per line

### Limits (both)

| Rule | Backend | Frontend |
|------|---------|----------|
| Max lines per file | 300 | 300 |
| Max lines per method | 30 | 50 |
| Max parameters | 4 | 4 |
| Max nesting depth | 3 | 3 |
| Cyclomatic complexity | ≤ 10 | ≤ 10 |

## DTO ↔ Domain Mapping

Mappings between domain types and DTOs (and between DTOs and commands)
are centralised in **extension method classes**, never inlined inside
handlers, repositories, or endpoints. Each aggregate root gets one
mapper file.

### File location and naming

Mapping extension classes live in `*.Application/DTOs/` and are named `{Aggregate}MappingExtensions.cs`. Read-model-row → DTO mappings, when the EF entity lives in Infrastructure, sit alongside that entity in `*.Infrastructure/Persistence/ReadModel/`.

API request → command conversion does NOT use extension methods. Use the `Deconstruct` / `ToValueObjects()` pattern documented under "Request DTO → Value Object Conversion" — endpoints destructure the request and construct the command inline.

### Canonical signatures

```csharp
// Single — domain entity / aggregate to DTO
public static RecipeDto ToDto(this Recipe recipe);

// Collection — IEnumerable<TSource> to IReadOnlyList<TDto>
public static IReadOnlyList<RecipeDto> ToDtos(this IEnumerable<Recipe> recipes);

// Multiple DTO shapes for the same source — distinct method names
public static RecipeDetailDto ToDetailDto(this Recipe recipe, bool isFavorite);
public static RecipeSummaryDto ToSummaryDto(this Recipe recipe);
```

The collection method always returns `IReadOnlyList<TDto>` (not `IEnumerable`) — the consumer almost always wants a materialised list, and exposing `IEnumerable` invites repeat enumeration. Single-method calls inside `Select` are preferred over the collection method when the surrounding LINQ pipeline already materialises:

```csharp
// ✅ inside a LINQ chain
var dtos = recipes.Where(recipe => recipe.IsActive).Select(recipe => recipe.ToDto()).ToList();

// ✅ direct collection mapping
var dtos = recipes.ToDtos();
```

### What goes in a mapping extension vs handler logic

Mapping extensions contain **only** field-to-field projection plus pure transforms (string trimming, enum conversion, value-object unwrap). Anything that needs collaborators (a repository read, a `currentUser`, a `timeProvider`) stays in the handler — that's not mapping, it's orchestration.

```csharp
// ✅ Pure mapping — belongs in extension
public static RecipeDto ToDto(this Recipe recipe) =>
    new(recipe.Id.Value, recipe.Title.Value, recipe.Servings?.Value);

// ❌ Not pure — must NOT live in extension method
public static RecipeDetailDto ToDetailDto(this Recipe recipe, IFavoriteRepository favorites) { … }
```

Pass extra primitive context as a parameter when needed (`isFavorite`, `category`), don't inject services.

### No inline `new …Dto(...)` in handlers, endpoints, or repositories

```csharp
// ❌ FORBIDDEN — inline projection
return recipes.Select(recipe => new RecipeDto(recipe.Id.Value, recipe.Title.Value, …)).ToList();

// ✅ REQUIRED — through an extension method
return recipes.Select(recipe => recipe.ToDto()).ToList();
// or
return recipes.ToDtos();
```

The single allowed exception is when the constructor call is the one statement in the mapper itself (the extension method body is the projection).

## Repository Query Plumbing — Apply* Extensions

Repositories that build paged read queries with **filter / search / sort**
keep the query plumbing in dedicated `*Extensions` static classes, one per
concern, sitting next to the repository in the same `Persistence/` folder.
The repository is left as a one-liner orchestrator.

### One concern → one file → one extension class

```
src/Yumney.{Module}.Infrastructure/Persistence/
├── {Aggregate}Repository.cs
├── FilterExtensions.cs       // ApplyFilter + per-axis ApplyTags / ApplyDifficulty / ...
├── SearchExtensions.cs       // ApplySearch
└── SortingExtensions.cs      // ApplySorting
```

### Multiple methods on one receiver → C# 14 `extension(T)` block

When a class hosts more than one `Apply*` instance-style method with the
same receiver type, group them inside an `extension(IQueryable<T> query)`
block so the receiver is declared once.

```csharp
// ✅ Multiple Apply* on the same receiver
public static class FilterExtensions
{
    extension(IQueryable<Recipe> query)
    {
        public IQueryable<Recipe> ApplyTags(IReadOnlyList<RecipeTag>? tags) { ... }
        public IQueryable<Recipe> ApplyDifficulty(Difficulty? difficulty) { ... }
        public IQueryable<Recipe> ApplyFavoritesOnly(bool? favoritesOnly, ...) { ... }

        public IQueryable<Recipe> ApplyFilter(RecipeFilter? filter, ...)
        {
            if (filter is null || filter.IsEmpty) return query;
            var (tags, difficulty, maxPrepTime, maxCookTime, favoritesOnly) = filter;
            return query
                .ApplyDifficulty(difficulty)
                .ApplyMaxPrepTime(maxPrepTime)
                .ApplyCookingTime(maxCookTime)
                .ApplyTags(tags)
                .ApplyFavoritesOnly(favoritesOnly, ...);
        }
    }
}
```

### Single method → classic `this T receiver` form

```csharp
// ✅ One Apply* on the receiver
public static class SearchExtensions
{
    public static IQueryable<Recipe> ApplySearch(this IQueryable<Recipe> query, SearchTerm? search)
    {
        if (search is null) return query;
        var pattern = $"%{search.Value}%";
        return query.Where(recipe => EF.Functions.ILike(recipe.Title, pattern) || ...);
    }
}
```

### Per-axis methods early-return when inactive

Each `Apply*` checks the inactive case (null, empty, `!= true`) and returns
the query unchanged. The orchestrator never has to know which axes are set.

```csharp
public IQueryable<Recipe> ApplyDifficulty(Difficulty? difficulty)
{
    if (difficulty is null) return query;
    return query.Where(recipe => recipe.Difficulty == difficulty);
}
```

### Cross-cutting subqueries are passed in, not embedded

When an axis needs to reach into another DbSet (e.g. favorites), the
repository builds the subquery and **passes it as a parameter**. The
extension class stays free of `DbContext` and stays unit-testable.

```csharp
// ❌ FORBIDDEN — extension class reaches into DbContext
public static IQueryable<Recipe> ApplyFavoritesOnly(this IQueryable<Recipe> query, bool? on, RecipesDbContext context) { ... }

// ✅ REQUIRED — repository owns the subquery, passes it in
private IQueryable<RecipeIdentifier> GetFavoriteRecipeIdsOfUserQuery(OwnerIdentifier owner) =>
    context.RecipeFavorites.Where(favorite => favorite.Owner == owner).Select(favorite => favorite.Recipe);

// in repository:
query = query
    .ApplySearch(search)
    .ApplyFilter(filter, GetFavoriteRecipeIdsOfUserQuery(owner))
    .ApplySorting(sorting);
```

### Rules of thumb

- Don't extract until there's at least one of: (a) a switch over `SortingOptions`, (b) a multi-axis filter VO, or (c) a search predicate beyond a single inline `Where`. Trivial single-axis sorts (`OrderByDescending(x => x.OccurredAt)`) stay inline.
- The orchestrator (`ApplyFilter` / `ApplySorting`) returns `IQueryable<T>` so it composes with the rest of the chain.
- Per-axis methods are named for the **field**, not the verb (`ApplyTags`, not `ApplyTagFilter`).
- The orchestrator deconstructs the filter VO via record `Deconstruct` and forwards each piece to the matching `Apply*`.

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
  - All credentials flow through Aspire `AddParameter(..., secret: true)` — `PostgresPassword`, `KeycloakPassword`, `MessagingPassword`, `RedisPassword`, `YumneyApiClientSecret`, `OpenAiApiKey`.
  - In run mode the AppHost provides a clearly-labeled dev default for the Keycloak client secret (matching `Realms/yumney-realm.json`); in publish mode the value is sourced from a Container App secret backed by Key Vault.
  - `gitleaks` runs on every PR (`.github/workflows/secret-scan.yml`) to catch regressions.
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
| HTML Parsing | AngleSharp |
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

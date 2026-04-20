# API specs

OpenAPI specs for each backend module, consumed by `yarn generate:api-types`
to produce TypeScript types under `libs/shared/api-client/src/lib/generated/`.

## Layout

```
openapi/
  recipes.json     # from Yumney.Recipes.Api
  shopping.json    # from Yumney.Shopping.Api
  mealplan.json    # from Yumney.MealPlan.Api
  users.json       # from Yumney.Users.Api
```

Each file is the verbatim `/openapi/v1.json` output of the corresponding
ASP.NET Core API project, committed so the frontend build doesn't depend on
a running backend.

## Refreshing the specs

1. Boot the stack via Aspire:
   ```bash
   cd src/Yumney.AppHost
   dotnet run
   ```
2. Each API publishes its spec at `http://<host>:<port>/openapi/v1.json`.
   Discover the URLs in the Aspire dashboard (or in the AppHost logs) and
   pipe each to the matching file:
   ```bash
   curl -s http://localhost:<recipes-port>/openapi/v1.json \
     > client/libs/shared/api-client/openapi/recipes.json
   # ... repeat for shopping, mealplan, users
   ```
3. Regenerate TypeScript types:
   ```bash
   cd client && yarn generate:api-types
   ```
4. Commit both the updated specs and the regenerated types in the same PR
   so the history shows the contract change atomically.

## CI drift check

The CI pipeline should run `yarn generate:api-types` and fail on a dirty
working tree — guarantees committed specs and generated types stay in sync.
Setup is tracked as a follow-up on the architecture-review issue.

# Yumney Client

Angular Micro-Frontend application using Nx workspace.

## Setup

```bash
# Initialize Nx workspace (run once)
npx create-nx-workspace@latest yumney-client --preset=angular-monorepo --packageManager=yarn

# Generate MFE apps
nx g @nx/angular:app shell
nx g @nx/angular:app recipes
nx g @nx/angular:app shopping
nx g @nx/angular:app account

# Generate shared libraries
nx g @nx/angular:lib shared/models
nx g @nx/angular:lib shared/api-client
nx g @nx/angular:lib shared/auth
nx g @nx/angular:lib shared/i18n
nx g @nx/angular:lib shared/state
nx g @nx/angular:lib ui

# Add Native Federation
yarn add @angular-architects/native-federation

# Add Storybook
nx g @nx/storybook:configuration ui

# Add Transloco for i18n
yarn add @jsverse/transloco
```

## Development

```bash
yarn nx serve shell       # Start shell (host)
yarn nx serve recipes     # Start recipes MFE
yarn nx run-many -t serve # Start all
yarn nx run-many -t test  # Run all tests
yarn nx run-many -t lint  # Lint all
yarn nx run ui:storybook  # Start Storybook
```

## Component Prefix

All components use the `yn-` prefix: `<yn-button>`, `<yn-rating-stars>`, etc.

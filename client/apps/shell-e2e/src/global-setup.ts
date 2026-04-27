import { assertLocalE2eTarget } from './helpers/env-guard';

// Runs once before any worker starts. We use it solely to fail fast when the
// suite would target a non-local environment — see helpers/env-guard.ts.
export default function globalSetup(): void {
  assertLocalE2eTarget();
}

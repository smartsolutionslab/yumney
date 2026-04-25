// Centralised waits for E2E.
//
// The `default` timeout was raised 10s → 25s after diagnosing the
// profile-settings cluster: each test gets a fresh browser context, and on
// first visit to a federated MFE route (account/recipes/shopping) the
// browser has to fetch the remoteEntry, the route bundle and any deps
// from the MFE's own Vite dev server before the component constructs and
// triggers its first API call. In dev mode that whole chain regularly
// takes 5–10s on top of the 1–2s API request itself, so a 10s
// toBeVisible was racing the cold-start of the MFE.
//
// `long` covers chains where a UI action triggers a backend write that
// then has to round-trip through projections / event handlers.
export const TIMEOUTS = {
  short: 5_000,
  default: 25_000,
  long: 35_000,
  veryLong: 60_000,
} as const;

// Centralised waits for E2E.
//
// `default` is intentionally generous because we run against `nx serve shell`
// in dev mode. On a fresh browser context, the first navigation to a federated
// MFE route (account / recipes / shopping) triggers Vite to compile and serve
// the remoteEntry, the route bundle, every shared dep, plus the @vite/client
// HMR runtime — all on demand. Under parallel-worker pressure that easily
// stretches past the 10s most assertions used to allow.
//
// `long` covers UI flows that trigger a backend write that round-trips through
// projections / event handlers before the result is visible.
export const TIMEOUTS = {
  short: 5_000,
  default: 45_000,
  long: 60_000,
  veryLong: 90_000,
} as const;

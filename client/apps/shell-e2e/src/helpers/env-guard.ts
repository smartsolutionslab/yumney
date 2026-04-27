// Safety guard: refuse to run e2e against anything but localhost.
//
// The suite issues real POST/DELETE against /api/v1/recipes and mutates the
// authenticated user's profile. A misconfigured BASE_URL pointing at staging
// or production would corrupt data with no easy way to roll back.
//
// Set E2E_ALLOW_REMOTE=true to opt out (only intended for one-off staging
// smoke runs by a human who has read the consequences).

const LOCAL_HOSTNAMES = new Set(['localhost', '127.0.0.1', '::1']);

export function assertLocalE2eTarget(env: NodeJS.ProcessEnv = process.env): void {
  if (env['E2E_ALLOW_REMOTE'] === 'true') return;

  const baseUrl = env['BASE_URL'] ?? 'http://localhost:4200';
  const gatewayUrl = env['GATEWAY_URL'] ?? 'http://localhost:5100';

  assertLocalHostname('BASE_URL', baseUrl);
  assertLocalHostname('GATEWAY_URL', gatewayUrl);
}

function assertLocalHostname(varName: string, rawUrl: string): void {
  let parsed: URL;
  try {
    parsed = new URL(rawUrl);
  } catch {
    throw new Error(
      `[e2e-guard] ${varName}=${rawUrl} is not a valid URL. Expected http(s)://localhost:port`,
    );
  }

  if (!LOCAL_HOSTNAMES.has(parsed.hostname)) {
    throw new Error(
      `[e2e-guard] Refusing to run e2e against ${varName}=${rawUrl}.\n` +
        `        The suite issues real writes (POST/DELETE recipes, mutates the test user)\n` +
        `        and must only target localhost. To override for a one-off staging smoke,\n` +
        `        set E2E_ALLOW_REMOTE=true (and accept the consequences).`,
    );
  }
}

import { readFileSync } from 'node:fs';

const E2E_TITLE_PREFIX = 'E2E ';
const PAGE_SIZE = 100;

/**
 * Backstop for orphaned test data (#406). The `setupSharedRecipe` helper
 * pairs every \`beforeAll\` create with an \`afterAll\` delete, but afterAll
 * is bypassed when a test crashes (or when beforeAll itself throws after
 * partially succeeding). Without this sweeper, every crash leaves a
 * stranded recipe in the dev DB; over time the seed data drifts and
 * inter-spec ordering becomes unpredictable.
 *
 * Runs once after the entire suite finishes. Lists all recipes the test
 * user owns, filters to those whose title starts with the well-known
 * E2E prefix, and DELETEs them.
 */
export default async function globalTeardown(): Promise<void> {
  // Skip when running against a non-local target — env-guard normally
  // prevents that, but the teardown shouldn't issue mass DELETEs against
  // anything but a localhost dev DB regardless.
  const gatewayUrl = process.env['GATEWAY_URL'] ?? 'http://localhost:5100';
  if (!gatewayUrl.includes('localhost') && !gatewayUrl.includes('127.0.0.1')) {
    return;
  }

  const tokensFile = process.env['E2E_TOKENS_FILE'];
  if (!tokensFile) {
    // Local runs without a pre-fetched token dump — skip silently. The
    // sweeper is a CI hygiene net, not a hard requirement.
    return;
  }

  const accessToken = readAccessToken(tokensFile);
  if (!accessToken) return;

  const orphans = await listE2eRecipes(gatewayUrl, accessToken);
  if (orphans.length === 0) {
    console.log('[global-teardown] no orphaned E2E recipes to clean up');
    return;
  }

  let deleted = 0;
  let failed = 0;
  for (const id of orphans) {
    const ok = await deleteRecipe(gatewayUrl, accessToken, id);
    if (ok) deleted += 1;
    else failed += 1;
  }
  console.log(
    `[global-teardown] swept ${deleted} orphaned E2E recipes` +
      (failed > 0 ? ` (${failed} delete failures, ignored)` : ''),
  );
}

function readAccessToken(file: string): string | null {
  try {
    const tokens = JSON.parse(readFileSync(file, 'utf8')) as { access_token?: string };
    return tokens.access_token ?? null;
  } catch {
    return null;
  }
}

async function listE2eRecipes(gatewayUrl: string, token: string): Promise<string[]> {
  const orphans: string[] = [];
  // Walk pages so a long-running suite that produces >PAGE_SIZE recipes
  // still gets fully swept.
  for (let page = 1; page <= 20; page += 1) {
    const url = `${gatewayUrl}/api/v1/recipes?page=${page}&pageSize=${PAGE_SIZE}`;
    const res = await fetch(url, { headers: { Authorization: `Bearer ${token}` } });
    if (!res.ok) return orphans;

    const body = (await res.json()) as {
      items?: Array<{ identifier: string; title: string }>;
      totalPages?: number;
    };
    const items = body.items ?? [];
    for (const item of items) {
      if (item.title.startsWith(E2E_TITLE_PREFIX)) {
        orphans.push(item.identifier);
      }
    }

    if (body.totalPages !== undefined && page >= body.totalPages) break;
    if (items.length < PAGE_SIZE) break;
  }
  return orphans;
}

async function deleteRecipe(gatewayUrl: string, token: string, id: string): Promise<boolean> {
  try {
    const res = await fetch(`${gatewayUrl}/api/v1/recipes/${id}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` },
    });
    return res.ok || res.status === 404;
  } catch {
    return false;
  }
}

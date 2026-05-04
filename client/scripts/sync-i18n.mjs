/**
 * Mirrors scoped i18n files from each MFE into the shell.
 *
 * Each MFE owns its translations under apps/{mfe}/public/assets/i18n/{scope}/{lang}.json.
 * When the shell loads an MFE via federation, the HTTP request for translations resolves
 * against the shell's origin, so the shell needs an identical copy at
 * apps/shell/public/assets/i18n/{scope}/{lang}.json. This script keeps both sides in sync.
 *
 * Usage:
 *   node scripts/sync-i18n.mjs           # copy MFE → shell
 *   node scripts/sync-i18n.mjs --check   # exit 1 if any pair differs (CI mode)
 */
import { readdir, readFile, writeFile, mkdir } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import { join, resolve, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const clientRoot = resolve(__dirname, '..');
const appsDir = join(clientRoot, 'apps');
const shellI18nRoot = join(appsDir, 'shell', 'public', 'assets', 'i18n');

const checkMode = process.argv.includes('--check');

async function discoverScopedFiles() {
  const apps = await readdir(appsDir, { withFileTypes: true });
  const sources = [];

  for (const app of apps) {
    if (!app.isDirectory() || app.name === 'shell' || app.name.endsWith('-e2e')) continue;

    const i18nDir = join(appsDir, app.name, 'public', 'assets', 'i18n');
    if (!existsSync(i18nDir)) continue;

    const entries = await readdir(i18nDir, { withFileTypes: true });
    for (const entry of entries) {
      if (!entry.isDirectory()) continue;

      const scopeDir = join(i18nDir, entry.name);
      const files = await readdir(scopeDir);
      for (const file of files) {
        if (!file.endsWith('.json')) continue;
        sources.push({
          scope: entry.name,
          source: join(scopeDir, file),
          target: join(shellI18nRoot, entry.name, file),
        });
      }
    }
  }

  return sources;
}

async function readOrNull(path) {
  if (!existsSync(path)) return null;
  return readFile(path, 'utf8');
}

async function main() {
  const pairs = await discoverScopedFiles();
  if (pairs.length === 0) {
    console.log('No scoped i18n files found.');
    return;
  }

  const drifted = [];
  const copied = [];

  for (const { source, target } of pairs) {
    const [sourceContent, targetContent] = await Promise.all([
      readFile(source, 'utf8'),
      readOrNull(target),
    ]);

    if (sourceContent === targetContent) continue;

    drifted.push({ source, target });
    if (!checkMode) {
      await mkdir(dirname(target), { recursive: true });
      await writeFile(target, sourceContent);
      copied.push(target);
    }
  }

  if (checkMode) {
    if (drifted.length === 0) {
      console.log(`✔ All ${pairs.length} scoped i18n file(s) in sync.`);
      return;
    }
    console.error(`✖ ${drifted.length} scoped i18n file(s) out of sync with shell:`);
    for (const { source, target } of drifted) {
      console.error(`  - ${relative(clientRoot, source)} → ${relative(clientRoot, target)}`);
    }
    console.error('\nRun `yarn sync:i18n` to update the shell copies.');
    process.exit(1);
  }

  if (copied.length === 0) {
    console.log(`✔ All ${pairs.length} scoped i18n file(s) already in sync.`);
    return;
  }
  console.log(`✔ Synced ${copied.length} of ${pairs.length} scoped i18n file(s):`);
  for (const target of copied) {
    console.log(`  - ${relative(clientRoot, target)}`);
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});

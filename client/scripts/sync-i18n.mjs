/**
 * Mirrors scoped i18n files from each MFE into the shell, and (in --check mode)
 * verifies that top-level i18n keys shared across apps agree on their values.
 *
 * Each MFE owns its translations under apps/{mfe}/public/assets/i18n/{scope}/{lang}.json.
 * When the shell loads an MFE via federation, the HTTP request for translations resolves
 * against the shell's origin, so the shell needs an identical copy at
 * apps/shell/public/assets/i18n/{scope}/{lang}.json. This script keeps both sides in sync.
 *
 * Each app also has top-level apps/{app}/public/assets/i18n/{lang}.json files for strings
 * served when the app runs standalone. These overlap (shared layout/header, common toast,
 * etc.) but have no single source of truth — so --check reports both (a) value drift on
 * shared keys and (b) missing keys on REQUIRED_SHARED_PREFIXES (the chrome that every
 * app's standalone bootstrap renders, e.g. 'layout.header'; an app that declares the
 * prefix at all must declare every key under it, otherwise the header renders the raw
 * key string in standalone mode). Sync mode does not auto-copy these.
 *
 * Usage:
 *   node scripts/sync-i18n.mjs           # copy scoped MFE → shell
 *   node scripts/sync-i18n.mjs --check   # exit 1 if any scoped pair or shared-subtree key differs/is missing
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

function flatten(obj, prefix = '') {
  const out = {};
  for (const [k, v] of Object.entries(obj)) {
    const key = prefix ? `${prefix}.${k}` : k;
    if (v && typeof v === 'object' && !Array.isArray(v)) {
      Object.assign(out, flatten(v, key));
    } else {
      out[key] = v;
    }
  }
  return out;
}

async function discoverTopLevelFiles() {
  const apps = await readdir(appsDir, { withFileTypes: true });
  const byLang = new Map();

  for (const app of apps) {
    if (!app.isDirectory() || app.name.endsWith('-e2e')) continue;
    const i18nDir = join(appsDir, app.name, 'public', 'assets', 'i18n');
    if (!existsSync(i18nDir)) continue;

    const entries = await readdir(i18nDir, { withFileTypes: true });
    for (const entry of entries) {
      if (entry.isDirectory() || !entry.name.endsWith('.json')) continue;
      const lang = entry.name.replace(/\.json$/, '');
      if (!byLang.has(lang)) byLang.set(lang, []);
      byLang.get(lang).push({ app: app.name, path: join(i18nDir, entry.name) });
    }
  }

  return byLang;
}

/**
 * Subtree prefixes that every app's standalone bootstrap renders, so every
 * top-level i18n file must declare every key the shell declares under them.
 * Add a prefix here when a new piece of always-on chrome ships.
 */
const REQUIRED_SHARED_PREFIXES = ['layout.header', 'common.errors', 'common.toast'];

function startsWithPrefix(flatKey, prefix) {
  return flatKey === prefix || flatKey.startsWith(`${prefix}.`);
}

async function findTopLevelDrifts() {
  const byLang = await discoverTopLevelFiles();
  const valueDrifts = [];
  const missing = [];

  for (const [lang, files] of byLang) {
    const flatByApp = {};
    for (const { app, path } of files) {
      flatByApp[app] = flatten(JSON.parse(await readFile(path, 'utf8')));
    }

    const allKeys = new Set();
    for (const flat of Object.values(flatByApp)) {
      for (const key of Object.keys(flat)) allKeys.add(key);
    }

    for (const key of allKeys) {
      const valuesByApp = {};
      for (const [app, flat] of Object.entries(flatByApp)) {
        if (key in flat) valuesByApp[app] = flat[key];
      }
      const distinctValues = new Set(Object.values(valuesByApp));
      if (Object.keys(valuesByApp).length > 1 && distinctValues.size > 1) {
        valueDrifts.push({ lang, key, valuesByApp });
      }

      // Required shared chrome: any app that declares the prefix at all
      // must declare every key under it.
      const matchedPrefix = REQUIRED_SHARED_PREFIXES.find((prefix) =>
        startsWithPrefix(key, prefix),
      );
      if (matchedPrefix) {
        const missingApps = [];
        for (const [app, flat] of Object.entries(flatByApp)) {
          const ownsPrefix = Object.keys(flat).some((appKey) =>
            startsWithPrefix(appKey, matchedPrefix),
          );
          if (ownsPrefix && !(key in flat)) {
            missingApps.push(app);
          }
        }
        if (missingApps.length > 0) {
          missing.push({ lang, key, missingApps });
        }
      }
    }
  }

  return { valueDrifts, missing };
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
    const { valueDrifts, missing } = await findTopLevelDrifts();
    let failed = false;

    if (drifted.length > 0) {
      failed = true;
      console.error(`✖ ${drifted.length} scoped i18n file(s) out of sync with shell:`);
      for (const { source, target } of drifted) {
        console.error(`  - ${relative(clientRoot, source)} → ${relative(clientRoot, target)}`);
      }
      console.error('\nRun `yarn sync:i18n` to update the shell copies.');
    }

    if (valueDrifts.length > 0) {
      failed = true;
      if (drifted.length > 0) console.error('');
      console.error(
        `✖ ${valueDrifts.length} top-level i18n key(s) drift between apps (no auto-fix; resolve manually):`,
      );
      for (const { lang, key, valuesByApp } of valueDrifts) {
        console.error(`  - [${lang}] ${key}`);
        for (const [app, value] of Object.entries(valuesByApp)) {
          console.error(`      ${app}: ${JSON.stringify(value)}`);
        }
      }
    }

    if (missing.length > 0) {
      failed = true;
      if (drifted.length > 0 || valueDrifts.length > 0) console.error('');
      console.error(
        `✖ ${missing.length} shared-subtree i18n key(s) missing from app(s) that own the subtree:`,
      );
      for (const { lang, key, missingApps } of missing) {
        console.error(`  - [${lang}] ${key} — missing from: ${missingApps.join(', ')}`);
      }
    }

    if (failed) process.exit(1);
    console.log(`✔ All ${pairs.length} scoped i18n file(s) in sync; no top-level drift.`);
    return;
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

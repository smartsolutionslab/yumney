/**
 * Post-build script: copies remote MFE outputs into the shell dist
 * so YARP can serve everything from a single static-files root.
 * Also replaces the dev federation manifest with the production version.
 *
 * Shell dist layout after copy:
 *   dist/apps/shell/browser/
 *     ├── assets/federation.manifest.json  (prod URLs)
 *     ├── recipes/                          (recipes MFE)
 *     │   ├── remoteEntry.json
 *     │   └── ...
 *     ├── shopping/                         (shopping MFE)
 *     │   ├── remoteEntry.json
 *     │   └── ...
 *     └── account/                          (account MFE)
 *         ├── remoteEntry.json
 *         └── ...
 */
import { cpSync, copyFileSync, existsSync, mkdirSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { join, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = join(__dirname, '..');
const distRoot = join(root, 'dist', 'apps');
const shellBrowser = join(distRoot, 'shell', 'browser');

// Copy remote MFE outputs
const remotes = ['recipes', 'shopping', 'account'];

for (const remote of remotes) {
  const src = join(distRoot, remote, 'browser');
  const dest = join(shellBrowser, remote);

  if (!existsSync(src)) {
    console.warn(`⚠ Skipping ${remote}: ${src} does not exist`);
    continue;
  }

  mkdirSync(dest, { recursive: true });
  cpSync(src, dest, { recursive: true });
  console.log(`✓ Copied ${remote} → ${dest}`);
}

// Replace dev manifest with production manifest
const prodManifest = join(root, 'apps', 'shell', 'src', 'assets', 'federation.manifest.prod.json');
const distManifest = join(shellBrowser, 'assets', 'federation.manifest.json');

if (existsSync(prodManifest) && existsSync(distManifest)) {
  copyFileSync(prodManifest, distManifest);
  console.log('✓ Replaced federation manifest with production version');
} else {
  console.warn('⚠ Could not replace federation manifest');
}

// Regenerate ngsw.json against the final on-disk bytes.
//
// Background (#423): nx build shell runs Angular's service-worker plugin
// which hashes every prefetched asset and writes those hashes into
// ngsw.json. We then *overwrite* /assets/federation.manifest.json above
// (and copy in remote MFE files), so the hashes baked into ngsw.json no
// longer match what the static server actually returns. NGSW computes
// SHA-1 of each asset during install, sees the mismatch, fails the
// install, and falls into SAFE_MODE — every cache stays empty and the
// PWA tests think offline doesn't work. Regenerating ngsw.json from the
// post-copy dist directory makes the hashTable match reality.
const ngswCli = join(root, 'node_modules', '@angular', 'service-worker', 'ngsw-config.js');
const ngswConfig = join(root, 'apps', 'shell', 'ngsw-config.json');
if (existsSync(ngswCli) && existsSync(ngswConfig)) {
  // ngsw-config CLI resolves its args relative to process.cwd, so pass
  // paths relative to the client root (where we run yarn from).
  const distRel = relative(root, shellBrowser);
  const cfgRel = relative(root, ngswConfig);
  execFileSync('node', [ngswCli, distRel, cfgRel, '/'], { stdio: 'inherit', cwd: root });
  console.log('✓ Regenerated ngsw.json against post-copy bytes');
} else {
  console.warn('⚠ Could not regenerate ngsw.json — CLI or config missing');
}

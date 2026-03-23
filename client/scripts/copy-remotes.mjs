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
import { join, dirname } from 'node:path';
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

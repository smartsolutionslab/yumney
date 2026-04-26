#!/usr/bin/env node
/**
 * Static-file server for the built shell bundle. Used by the E2E pipeline
 * so Playwright runs against a production-mode build rather than `nx serve`
 * (Vite dev server). Production mode is what enables Angular's
 * provideServiceWorker registration, so PWA tests need this path.
 *
 * After `yarn build:all`, scripts/copy-remotes.mjs has merged every MFE
 * (`recipes`, `shopping`, `account`) into `dist/apps/shell/browser/{name}/`
 * and replaced the federation manifest with the prod version, so a single
 * static root serves the whole app.
 *
 * Tiny zero-dep Node http server with SPA fallback to index.html.
 */
import { createServer } from 'node:http';
import { createReadStream, statSync } from 'node:fs';
import { join, dirname, extname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(__dirname, '..', 'dist', 'apps', 'shell', 'browser');
const PORT = Number(process.env.PORT ?? 4200);

const MIME = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'application/javascript; charset=utf-8',
  '.mjs': 'application/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.webp': 'image/webp',
  '.ico': 'image/x-icon',
  '.webmanifest': 'application/manifest+json; charset=utf-8',
  '.woff2': 'font/woff2',
  '.woff': 'font/woff',
};

function serveFile(res, path) {
  try {
    const stat = statSync(path);
    if (!stat.isFile()) return false;
    // Service-worker scripts must be served fresh so updates land — Angular
    // SW versions itself via ngsw.json which is fetched on every check.
    const isSwAsset =
      path.endsWith('ngsw-worker.js') ||
      path.endsWith('ngsw.json') ||
      path.endsWith('safety-worker.js');
    res.writeHead(200, {
      'Content-Type': MIME[extname(path).toLowerCase()] ?? 'application/octet-stream',
      'Content-Length': stat.size,
      'Cache-Control': isSwAsset ? 'no-cache, no-store, must-revalidate' : 'public, max-age=300',
    });
    createReadStream(path).pipe(res);
    return true;
  } catch {
    return false;
  }
}

const server = createServer((req, res) => {
  let urlPath = (req.url ?? '/').split('?')[0];
  if (urlPath.includes('..')) {
    res.writeHead(400);
    res.end('Bad Request');
    return;
  }
  if (urlPath.endsWith('/')) urlPath += 'index.html';

  const filePath = join(ROOT, urlPath);
  if (serveFile(res, filePath)) return;

  // SPA fallback: any path without a file extension falls back to index.html
  // so client-side routes (/account, /recipes/:id, …) load the shell.
  if (!extname(urlPath)) {
    if (serveFile(res, join(ROOT, 'index.html'))) return;
  }

  res.writeHead(404, { 'Content-Type': 'text/plain' });
  res.end('Not Found');
});

server.listen(PORT, () => {
  console.log(`shell static server listening on http://localhost:${PORT} (root: ${ROOT})`);
});

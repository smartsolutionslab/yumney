#!/usr/bin/env node
/**
 * Static-file server for the built shell bundle. Used by the E2E pipeline
 * so Playwright doesn't run against `nx serve` (Vite dev server) which
 * proved unreliable under parallel-worker pressure with on-demand module
 * loading and federated MFEs. After `yarn build:all`, every MFE has been
 * copied into `dist/apps/shell/browser/{name}` (see scripts/copy-remotes.mjs)
 * so a single static root serves the whole app.
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
    res.writeHead(200, {
      'Content-Type': MIME[extname(path).toLowerCase()] ?? 'application/octet-stream',
      'Content-Length': stat.size,
      'Cache-Control': 'no-cache',
    });
    createReadStream(path).pipe(res);
    return true;
  } catch {
    return false;
  }
}

const server = createServer((req, res) => {
  // Strip query string; reject path traversal.
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
  // so client-side routes (e.g. /account, /recipes/:id) load the shell.
  if (!extname(urlPath)) {
    if (serveFile(res, join(ROOT, 'index.html'))) return;
  }

  res.writeHead(404, { 'Content-Type': 'text/plain' });
  res.end('Not Found');
});

server.listen(PORT, () => {
  console.log(`shell static server listening on http://localhost:${PORT} (root: ${ROOT})`);
});

// Shared shape configuration for the four Native Federation MFEs.
// Each app's federation.config.js consumes the constants below so we keep
// the shared package list and skip list in one place.

const singleton = { singleton: true, strictVersion: true, requiredVersion: 'auto' };

const sharedPackages = {
  '@angular/core': singleton,
  '@angular/common': singleton,
  '@angular/common/http': singleton,
  '@angular/router': singleton,
  '@angular/forms': singleton,
  '@angular/platform-browser': singleton,
  '@jsverse/transloco': singleton,
  'angular-oauth2-oidc': singleton,
  'lucide-angular': singleton,
  rxjs: singleton,
};

const skipPackages = [
  'zone.js',
  'rxjs/ajax',
  'rxjs/fetch',
  'rxjs/testing',
  'rxjs/webSocket',
  '@angular/animations',
  '@angular/animations/browser',
  '@angular/platform-browser/animations',
  '@angular/platform-browser/animations/async',
  '@angular/common/http/http',
  '@angular/common/http/testing',
  '@angular/common/testing',
  '@angular/core/testing',
  '@angular/platform-browser/testing',
  '@angular/router/testing',
  '@angular/common/upgrade',
  '@angular/common/http/upgrade',
  '@angular/platform-server',
  '@angular/ssr',
];

module.exports = { sharedPackages, skipPackages };

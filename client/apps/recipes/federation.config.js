const {
  withNativeFederation,
  share,
} = require('@angular-architects/native-federation/config');

module.exports = withNativeFederation({
  name: 'recipes',
  exposes: {
    './routes': './apps/recipes/src/app/recipes.routes.ts',
  },
  shared: share({
    '@angular/core': { singleton: true, strictVersion: true, requiredVersion: 'auto' },
    '@angular/common': { singleton: true, strictVersion: true, requiredVersion: 'auto' },
    '@angular/common/http': { singleton: true, strictVersion: true, requiredVersion: 'auto' },
    '@angular/router': { singleton: true, strictVersion: true, requiredVersion: 'auto' },
    '@angular/forms': { singleton: true, strictVersion: true, requiredVersion: 'auto' },
    '@angular/platform-browser': {
      singleton: true,
      strictVersion: true,
      requiredVersion: 'auto',
    },
    '@jsverse/transloco': { singleton: true, strictVersion: true, requiredVersion: 'auto' },
    'angular-oauth2-oidc': { singleton: true, strictVersion: true, requiredVersion: 'auto' },
    rxjs: { singleton: true, strictVersion: true, requiredVersion: 'auto' },
  }),
  skip: [
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
  ],
});

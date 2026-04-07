const { withNativeFederation, share } = require('@angular-architects/native-federation/config');
const { sharedPackages, skipPackages } = require('../../federation.shared');

module.exports = withNativeFederation({
  name: 'recipes',
  exposes: {
    './routes': './apps/recipes/src/app/recipes.routes.ts',
  },
  shared: share(sharedPackages),
  skip: skipPackages,
});

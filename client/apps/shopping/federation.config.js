const { withNativeFederation, share } = require('@angular-architects/native-federation/config');
const { sharedPackages, skipPackages } = require('../../federation.shared');

module.exports = withNativeFederation({
  name: 'shopping',
  exposes: {
    './routes': './apps/shopping/src/app/shopping.routes.ts',
  },
  shared: share(sharedPackages),
  skip: skipPackages,
});

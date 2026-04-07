const { withNativeFederation, share } = require('@angular-architects/native-federation/config');
const { sharedPackages, skipPackages } = require('../../federation.shared');

module.exports = withNativeFederation({
  name: 'account',
  exposes: {
    './routes': './apps/account/src/app/account.routes.ts',
  },
  shared: share(sharedPackages),
  skip: skipPackages,
});

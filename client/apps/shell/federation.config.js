const { withNativeFederation, share } = require('@angular-architects/native-federation/config');
const { sharedPackages, skipPackages } = require('../../federation.shared');

module.exports = withNativeFederation({
  name: 'shell',
  shared: share(sharedPackages),
  skip: skipPackages,
});

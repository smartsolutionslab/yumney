module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'scope-enum': [
      2,
      'always',
      ['recipes', 'shopping', 'users', 'account', 'shared', 'api', 'shell', 'infra', 'ui', 'scaffold', 'ci', 'auth', 'e2e', 'frontend', 'test', 'domain', 'application'],
    ],
    'header-max-length': [2, 'always', 120],
    'subject-case': [2, 'never', ['start-case', 'pascal-case', 'upper-case']],
    'body-max-line-length': [1, 'always', 120],
  },
};

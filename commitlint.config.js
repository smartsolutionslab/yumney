module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'scope-enum': [
      2,
      'always',
      ['recipes', 'shopping', 'users', 'account', 'shared', 'api', 'shell', 'infra', 'ui', 'scaffold', 'ci', 'auth', 'e2e', 'frontend', 'test', 'domain', 'application', 'mealplan'],
    ],
    'header-max-length': [2, 'always', 120],
    'subject-case': [2, 'never', ['start-case', 'pascal-case', 'upper-case']],
    'body-max-line-length': [1, 'always', 120],
  },
  // Legacy commits on long-lived branches (e.g. refactor/mealplan-event-sourcing) predate
  // the current scope/type allowlist. Rewriting that history is impractical; squash-merging
  // those PRs collapses the history into a single conforming commit. Until those merge,
  // the patterns below skip validation for the specific legacy subjects.
  ignores: [
    (msg) => /^debug\(/.test(msg),
    (msg) => /^(test|fix|build|refactor)\((chat|meal-planner|client|gateway|deps|a11y|i18n|all)\)/.test(msg),
  ],
};

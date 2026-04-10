export const SELECTORS = {
  banners: {
    success: '.success-banner',
    error: '[role="alert"]',
  },
  form: {
    fieldError: '.field-error',
    ingredients: '.ingredient-fields',
    steps: '.step-fields',
  },
  recipe: {
    card: '.recipe-card',
    title: '.recipe-title',
    deleteBtn: '.btn-danger',
    confirmDelete: '.btn-danger-filled',
  },
  chat: {
    fab: '.command-fab',
    panel: '.chat-panel',
    backdrop: '.chat-backdrop',
    close: '.chat-close',
    clear: '.chat-clear',
    send: '.chat-send',
    input: '.chat-input textarea',
    welcome: '.chat-welcome',
    examples: '.chat-examples li',
    userMessage: '.chat-message.role-user .chat-bubble',
    assistantMessage: '.chat-message.role-assistant .chat-bubble',
  },
  header: {
    langToggle: '.lang-toggle',
    logout: '.logout-button',
  },
  keycloak: {
    username: '#username',
    password: '#password',
    loginBtn: '#kc-login',
  },
} as const;

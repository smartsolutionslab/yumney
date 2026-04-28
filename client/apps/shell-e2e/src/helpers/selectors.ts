export const SELECTORS = {
  banners: {
    success: '[data-testid="success-banner"]',
    // error stays as the role-alert match — it's the canonical a11y
    // attribute for the banner and gives us free rule-out for axe a11y
    // checks. Multiple components render their own [role="alert"];
    // tests scope by container as needed.
    error: '[role="alert"]',
  },
  form: {
    fieldError: '.field-error',
    ingredients: '.ingredient-fields',
    steps: '.step-fields',
  },
  recipe: {
    card: '[data-testid="recipe-card"]',
    title: '.recipe-title',
    deleteBtn: '[data-testid="recipe-delete-btn"]',
    // confirmDelete is the modal confirmation button inside yn-confirm-dialog;
    // shared component, scope-agnostic — keep CSS selector for now until the
    // dialog gets its own data-testid in a follow-up.
    confirmDelete: '.btn-danger-filled',
  },
  chat: {
    fab: '[data-testid="chat-fab"]',
    panel: '[data-testid="chat-panel"]',
    backdrop: '[data-testid="chat-backdrop"]',
    close: '[data-testid="chat-close"]',
    clear: '[data-testid="chat-clear"]',
    send: '[data-testid="chat-send"]',
    input: '[data-testid="chat-input"]',
    welcome: '[data-testid="chat-welcome"]',
    // Multi-element / state-based — kept as class selectors (each li is a
    // separate item, role-based bubbles distinguish user vs assistant).
    examples: '.chat-examples li',
    userMessage: '.chat-message.role-user .chat-bubble',
    assistantMessage: '.chat-message.role-assistant .chat-bubble',
  },
  mealPlanner: {
    dayCard: '.day-card',
    dayHeader: '.day-header',
    mealTitle: '.meal-title',
    mealServings: '.meal-servings',
    mealFreetext: '.meal-freetext',
    mealState: '.meal-state',
    emptySlot: '.empty-slot',
    clearBtn: '.clear-btn',
    navPrev: '[data-testid="meal-planner-nav-prev"]',
    navNext: '[data-testid="meal-planner-nav-next"]',
    weekLabel: '.planner-header h1',
    generateBtn: '.generate-btn',
    shoppingResult: '.shopping-result',
    plannerActions: '.planner-actions',
    loading: '.loading',
    error: '.error',
    retryBtn: '.retry-btn',
  },
  shopping: {
    categoryGroup: '[data-testid="shopping-category-group"]',
    itemName: '.item-name',
    addInput: '[data-testid="shopping-add-input"]',
    emptyState: '[data-testid="shopping-empty-state"]',
    progressBar: '[data-testid="shopping-progress-bar"]',
    retryBtn: '.retry-btn',
  },
  profileSettings: {
    title: '.profile-settings h1',
    servingsInput: '#servings',
    dietaryTypeSelect: '#dietaryType',
    cookingEffortSelect: '#cookingEffort',
    checkboxLabel: '.checkbox-label',
    saveBtn: '.save-btn',
    savedIndicator: '.saved-indicator',
    settingsSection: '.settings-section',
    loading: '.loading',
    error: '.error',
    retryBtn: '.retry-btn',
  },
  header: {
    userMenuToggle: '[data-testid="user-menu-toggle"]',
    langSwitch: '[data-testid="lang-switch"]',
    logout: '[data-testid="logout"]',
  },
  keycloak: {
    username: '#username',
    password: '#password',
    loginBtn: '#kc-login',
  },
} as const;

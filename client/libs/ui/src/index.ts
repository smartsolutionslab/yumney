export { BackLinkComponent } from './lib/back-link/back-link.component';
export { ButtonComponent, type ButtonVariant, type ButtonType } from './lib/button/button.component';
export { CardComponent, type CardVariant } from './lib/card/card.component';
export { DialogShellComponent, type DialogRole, type DialogSize } from './lib/dialog-shell/dialog-shell.component';
export { SideSheetComponent, type SideSheetPosition, type SideSheetSize } from './lib/side-sheet/side-sheet.component';
export { EmptyStateComponent, type EmptyStateVariant } from './lib/empty-state/empty-state.component';
export { FormFieldComponent } from './lib/form-field/form-field.component';
export { MessageBannerComponent, type MessageBannerTone } from './lib/message-banner/message-banner.component';
export { SubmitButtonComponent } from './lib/submit-button/submit-button.component';
export { RecipePreviewComponent } from './lib/recipe-preview/recipe-preview.component';
export { EditableListItemComponent } from './lib/editable-list-item/editable-list-item.component';
export {
  CategorySectionComponent,
  type CategoryKey,
  CATEGORY_KEYS,
  normalizeCategory,
} from './lib/category-section/category-section.component';
export { SettingsCardComponent } from './lib/settings-card/settings-card.component';
export { ConfirmDialogComponent } from './lib/confirm-dialog/confirm-dialog.component';
export { HeaderComponent } from './lib/header/header.component';
export { AppLayoutComponent } from './lib/app-layout/app-layout.component';
export { UnitSelectComponent } from './lib/unit-select/unit-select.component';
export { UnitToggleComponent } from './lib/unit-toggle/unit-toggle.component';
export { LoadingSpinnerComponent } from './lib/loading-spinner/loading-spinner.component';
export { AsyncStateComponent } from './lib/async-state/async-state.component';

// Animation utilities
export { springPress, staggerFadeIn, prefersReducedMotion } from './lib/animation/gsap-utils';

// Dashboard components
export { QuickActionsComponent, type QuickAction } from './lib/quick-actions/quick-actions.component';
export { SuggestionCardComponent } from './lib/suggestion-card/suggestion-card.component';
export { RecentActivityComponent } from './lib/recent-activity/recent-activity.component';

export { CameraCaptureComponent } from './lib/camera-capture/camera-capture.component';
export { IngredientScannerComponent } from './lib/ingredient-scanner/ingredient-scanner.component';
export { ChatPanelComponent } from './lib/chat-panel/chat-panel.component';
export { CommandFabComponent } from './lib/command-fab/command-fab.component';
export { ShareToastComponent } from './lib/share-toast/share-toast.component';
export { IngredientsToastComponent } from './lib/ingredients-toast/ingredients-toast.component';
export { ToastHostComponent } from './lib/toast-host/toast-host.component';
export {
  FilterPanelComponent,
  type RecipeFilterValue,
  type RecipeDifficulty,
  EMPTY_FILTER,
} from './lib/filter-panel/filter-panel.component';
export { StepDisplayComponent } from './lib/step-display/step-display.component';
export { CookingTimerComponent } from './lib/cooking-timer/cooking-timer.component';
export { VoiceIndicatorComponent } from './lib/voice-indicator/voice-indicator.component';
export { FavoriteButtonComponent } from './lib/favorite-button/favorite-button.component';
export {
  ActivityTimelineComponent,
  type ActivityEntry,
  type ActivityTypeKey,
  relativeTimeFromNow,
} from './lib/activity-timeline/activity-timeline.component';
export { StarRatingComponent } from './lib/star-rating/star-rating.component';

// Icons
export { provideYumneyIcons } from './lib/icons/provide-icons';

// Directives
export { ClickOutsideDirective } from './lib/directives/click-outside.directive';
export { InfiniteScrollDirective } from './lib/directives/infinite-scroll.directive';
export { StaggerNewItemsDirective } from './lib/directives/stagger-new-items.directive';

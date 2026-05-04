import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';

export type ActivityTypeKey =
  | 'recipe_imported'
  | 'recipe_viewed'
  | 'recipe_cooked'
  | 'recipe_edited'
  | 'recipe_deleted'
  | 'shopping_list_created';

export interface ActivityEntry {
  type: ActivityTypeKey;
  recipeIdentifier: string | null;
  recipeTitle: string | null;
  occurredAt: string;
}

const ACTIVITY_ICONS: Record<ActivityTypeKey, string> = {
  recipe_imported: 'book-open',
  recipe_viewed: 'eye',
  recipe_cooked: 'flame',
  recipe_edited: 'pencil',
  recipe_deleted: 'trash-2',
  shopping_list_created: 'shopping-cart',
};

@Component({
  selector: 'yn-activity-timeline',
  imports: [TranslocoModule, LucideAngularModule],
  templateUrl: './activity-timeline.component.html',
  styleUrl: './activity-timeline.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivityTimelineComponent {
  entries = input.required<ActivityEntry[]>();

  protected readonly view = computed(() =>
    this.entries().map((entry) => ({
      ...entry,
      icon: ACTIVITY_ICONS[entry.type] ?? 'circle',
      relative: relativeTimeFromNow(new Date(entry.occurredAt)),
    })),
  );
}

const SECOND = 1000;
const MINUTE = 60 * SECOND;
const HOUR = 60 * MINUTE;
const DAY = 24 * HOUR;
const WEEK = 7 * DAY;

// Locale-agnostic relative-time formatter — returns the raw key + count so the
// caller's i18n layer can pluralize. Keeps the component free of date-fns.
export function relativeTimeFromNow(when: Date, now: Date = new Date()): {
  key: string;
  value: number;
} {
  const diff = now.getTime() - when.getTime();
  if (diff < MINUTE) return { key: 'shared.activity.relative.justNow', value: 0 };
  if (diff < HOUR) return { key: 'shared.activity.relative.minutes', value: Math.floor(diff / MINUTE) };
  if (diff < DAY) return { key: 'shared.activity.relative.hours', value: Math.floor(diff / HOUR) };
  if (diff < WEEK) return { key: 'shared.activity.relative.days', value: Math.floor(diff / DAY) };
  return { key: 'shared.activity.relative.weeks', value: Math.floor(diff / WEEK) };
}

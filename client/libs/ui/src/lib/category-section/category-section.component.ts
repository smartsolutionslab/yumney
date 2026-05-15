import { Component, ChangeDetectionStrategy, input, output, signal } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';

export type CategoryKey =
  | 'produce'
  | 'dairy'
  | 'meat-fish'
  | 'bakery'
  | 'frozen'
  | 'beverages'
  | 'pantry'
  | 'spices'
  | 'household'
  | 'other';

/** Canonical display order. Pass to groupByCategory() to render sections in this sequence. */
export const CATEGORY_KEYS: readonly CategoryKey[] = [
  'produce',
  'dairy',
  'meat-fish',
  'bakery',
  'frozen',
  'beverages',
  'pantry',
  'spices',
  'household',
  'other',
];

/** Coerce an arbitrary server-supplied category string to a known CategoryKey. */
export function normalizeCategory(value: string | null | undefined): CategoryKey {
  const candidate = (value ?? 'other').toLowerCase();
  return (CATEGORY_KEYS as readonly string[]).includes(candidate) ? (candidate as CategoryKey) : 'other';
}

const CATEGORY_ICONS: Record<CategoryKey, string> = {
  produce: 'apple',
  dairy: 'milk',
  'meat-fish': 'fish',
  bakery: 'cookie',
  frozen: 'snowflake',
  beverages: 'cup-soda',
  pantry: 'package',
  spices: 'flame',
  household: 'spray-can',
  other: 'shopping-basket',
};

@Component({
  selector: 'yn-category-section',
  imports: [TranslocoModule, LucideAngularModule],
  templateUrl: './category-section.component.html',
  styleUrl: './category-section.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategorySectionComponent {
  category = input.required<CategoryKey>();
  itemCount = input.required<number>();

  toggle = output<void>();

  protected readonly isOpen = signal(true);

  protected get icon(): string {
    return CATEGORY_ICONS[this.category()] ?? CATEGORY_ICONS.other;
  }

  protected onHeaderClick(): void {
    this.isOpen.update((open) => !open);
    this.toggle.emit();
  }
}

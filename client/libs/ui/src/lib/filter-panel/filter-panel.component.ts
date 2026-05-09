import {
  ChangeDetectionStrategy,
  Component,
  computed,
  HostListener,
  input,
  output,
} from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

export type RecipeDifficulty = 'easy' | 'medium' | 'hard';

export interface RecipeFilterValue {
  tags: string[];
  difficulty: RecipeDifficulty | null;
  maxPrepTime: number | null;
  maxCookTime: number | null;
  favoritesOnly: boolean;
}

export const EMPTY_FILTER: RecipeFilterValue = {
  tags: [],
  difficulty: null,
  maxPrepTime: null,
  maxCookTime: null,
  favoritesOnly: false,
};

const DIFFICULTY_OPTIONS: RecipeDifficulty[] = ['easy', 'medium', 'hard'];

@Component({
  selector: 'yn-filter-panel',
  standalone: true,
  imports: [TranslocoModule],
  templateUrl: './filter-panel.component.html',
  styleUrl: './filter-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilterPanelComponent {
  value = input.required<RecipeFilterValue>();
  availableTags = input<string[]>([]);
  valueChange = output<RecipeFilterValue>();
  closed = output<void>();

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    this.closed.emit();
  }

  protected readonly difficultyOptions = DIFFICULTY_OPTIONS;

  protected activeCount = computed(() => {
    const filter = this.value();
    let count = filter.tags.length;
    if (filter.difficulty !== null) count += 1;
    if (filter.maxPrepTime !== null) count += 1;
    if (filter.maxCookTime !== null) count += 1;
    if (filter.favoritesOnly) count += 1;
    return count;
  });

  protected toggleFavoritesOnly(): void {
    const current = this.value();
    this.valueChange.emit({ ...current, favoritesOnly: !current.favoritesOnly });
  }

  protected isTagActive(tag: string): boolean {
    return this.value().tags.includes(tag);
  }

  protected toggleTag(tag: string): void {
    const current = this.value();
    const tags = current.tags.includes(tag)
      ? current.tags.filter((existing) => existing !== tag)
      : [...current.tags, tag];
    this.valueChange.emit({ ...current, tags });
  }

  protected toggleDifficulty(difficulty: RecipeDifficulty): void {
    const current = this.value();
    const next = current.difficulty === difficulty ? null : difficulty;
    this.valueChange.emit({ ...current, difficulty: next });
  }

  protected onPrepTimeChange(event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    const parsed = raw === '' ? null : Number(raw);
    this.valueChange.emit({
      ...this.value(),
      maxPrepTime: parsed !== null && !Number.isNaN(parsed) && parsed > 0 ? parsed : null,
    });
  }

  protected onCookTimeChange(event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    const parsed = raw === '' ? null : Number(raw);
    this.valueChange.emit({
      ...this.value(),
      maxCookTime: parsed !== null && !Number.isNaN(parsed) && parsed > 0 ? parsed : null,
    });
  }

  protected clearAll(): void {
    this.valueChange.emit({ ...EMPTY_FILTER });
  }
}

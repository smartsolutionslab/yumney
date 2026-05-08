import { Component, ChangeDetectionStrategy, input, output, signal } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { ClickOutsideDirective } from '@yumney/ui';

export interface SortMenuOption {
  value: string;
  labelKey: string;
}

@Component({
  selector: 'yn-sort-menu',
  imports: [TranslocoModule, LucideAngularModule, ClickOutsideDirective],
  templateUrl: './sort-menu.component.html',
  styleUrl: './sort-menu.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SortMenuComponent {
  currentValue = input.required<string>();
  options = input.required<readonly SortMenuOption[]>();
  ariaLabel = input<string>('');
  sortSelect = output<string>();

  protected open = signal(false);

  protected currentLabelKey(): string {
    return this.options().find((option) => option.value === this.currentValue())?.labelKey ?? '';
  }

  protected toggle(): void {
    this.open.update((open) => !open);
  }

  protected select(value: string): void {
    this.open.set(false);
    this.sortSelect.emit(value);
  }

  protected onDismiss(): void {
    if (this.open()) this.open.set(false);
  }
}

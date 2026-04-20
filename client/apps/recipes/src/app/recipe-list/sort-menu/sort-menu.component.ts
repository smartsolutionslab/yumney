import {
  Component,
  ChangeDetectionStrategy,
  ElementRef,
  HostListener,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';

export interface SortMenuOption {
  value: string;
  labelKey: string;
}

@Component({
  selector: 'yn-sort-menu',
  imports: [TranslocoModule, LucideAngularModule],
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
  private dropdown = viewChild<ElementRef>('dropdown');

  protected currentLabelKey(): string {
    return this.options().find((o) => o.value === this.currentValue())?.labelKey ?? '';
  }

  protected toggle(): void {
    this.open.update((o) => !o);
  }

  protected select(value: string): void {
    this.open.set(false);
    this.sortSelect.emit(value);
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.open()) this.open.set(false);
  }

  @HostListener('document:click', ['$event.target'])
  protected onDocumentClick(target: EventTarget | null): void {
    const el = this.dropdown()?.nativeElement;
    if (this.open() && target instanceof Node && !el?.contains(target)) {
      this.open.set(false);
    }
  }
}

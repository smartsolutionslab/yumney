import {
  Component,
  ChangeDetectionStrategy,
  ViewEncapsulation,
  computed,
  input,
  output,
} from '@angular/core';

export type SideSheetSize = 'sm' | 'md' | 'lg';
export type SideSheetPosition = 'left' | 'right';

@Component({
  selector: 'yn-side-sheet',
  templateUrl: './side-sheet.component.html',
  styleUrl: './side-sheet.component.scss',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:keydown.escape)': 'onEscape()',
  },
})
export class SideSheetComponent {
  size = input<SideSheetSize>('md');
  position = input<SideSheetPosition>('right');
  labelledBy = input<string | undefined>(undefined);
  testId = input<string | undefined>(undefined);
  cancelOnBackdrop = input(true);
  cancelOnEscape = input(true);

  cancelled = output<void>();

  protected readonly sheetClass = computed(
    () => `yn-side-sheet yn-side-sheet--${this.size()} yn-side-sheet--${this.position()}`,
  );

  onEscape(): void {
    if (this.cancelOnEscape()) this.cancelled.emit();
  }

  onOverlayClick(event: MouseEvent): void {
    if (this.cancelOnBackdrop() && event.target === event.currentTarget) {
      this.cancelled.emit();
    }
  }
}

import {
  Component,
  ChangeDetectionStrategy,
  ViewEncapsulation,
  computed,
  input,
  output,
} from '@angular/core';

export type DialogSize = 'sm' | 'md' | 'lg';
export type DialogRole = 'dialog' | 'alertdialog';

@Component({
  selector: 'yn-dialog-shell',
  templateUrl: './dialog-shell.component.html',
  styleUrl: './dialog-shell.component.scss',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:keydown.escape)': 'onEscape()',
  },
})
export class DialogShellComponent {
  size = input<DialogSize>('md');
  role = input<DialogRole>('dialog');
  labelledBy = input<string | undefined>(undefined);
  testId = input<string | undefined>(undefined);
  cancelOnBackdrop = input(true);
  cancelOnEscape = input(true);

  cancelled = output<void>();

  protected readonly dialogClass = computed(() => `yn-dialog yn-dialog--${this.size()}`);

  onEscape(): void {
    if (this.cancelOnEscape()) this.cancelled.emit();
  }

  onOverlayClick(event: MouseEvent): void {
    if (this.cancelOnBackdrop() && event.target === event.currentTarget) {
      this.cancelled.emit();
    }
  }
}

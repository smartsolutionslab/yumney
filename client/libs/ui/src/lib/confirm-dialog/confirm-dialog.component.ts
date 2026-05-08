import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { ButtonComponent } from '../button/button.component';
import { DialogShellComponent } from '../dialog-shell/dialog-shell.component';

@Component({
  selector: 'yn-confirm-dialog',
  imports: [ButtonComponent, DialogShellComponent],
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmDialogComponent {
  message = input.required<string>();
  confirmLabel = input('OK');
  cancelLabel = input('Cancel');

  confirmed = output<void>();
  cancelled = output<void>();

  onConfirm(): void {
    this.confirmed.emit();
  }

  onCancel(): void {
    this.cancelled.emit();
  }
}

import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { AbstractControl, FormGroup } from '@angular/forms';
import { TranslocoModule } from '@jsverse/transloco';
import { KeyValuePipe } from '@angular/common';

@Component({
  selector: 'yn-form-field',
  imports: [TranslocoModule, KeyValuePipe],
  templateUrl: './form-field.component.html',
  styleUrl: './form-field.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormFieldComponent {
  label = input('');
  for = input('');
  control = input.required<AbstractControl>();
  errors = input<Record<string, string>>({});
  group = input<FormGroup | undefined>(undefined);
  groupErrors = input<Record<string, string>>({});

  hasControlError(errorKey: string): boolean {
    const ctrl = this.control();
    return ctrl.hasError(errorKey) && ctrl.touched;
  }

  hasGroupError(errorKey: string): boolean {
    const grp = this.group();
    const ctrl = this.control();
    return !!grp?.hasError(errorKey) && ctrl.touched;
  }
}

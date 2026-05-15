import { Component, ChangeDetectionStrategy, ViewEncapsulation, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormGroup } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { KeyValuePipe } from '@angular/common';
import { of, startWith, switchMap } from 'rxjs';

@Component({
  selector: 'yn-form-field',
  imports: [TranslocoPipe, KeyValuePipe],
  templateUrl: './form-field.component.html',
  styleUrl: './form-field.component.scss',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormFieldComponent {
  label = input('');
  for = input('');
  control = input.required<AbstractControl>();
  errors = input<Record<string, string>>({});
  group = input<FormGroup | undefined>(undefined);
  groupErrors = input<Record<string, string>>({});

  // Track control/group events so OnPush re-renders on touched/status changes
  // triggered by ancestors (e.g. markAllAsTouched on submit).
  private readonly controlEvents = toSignal(toObservable(this.control).pipe(switchMap((control) => control.events.pipe(startWith(null)))));
  private readonly groupEvents = toSignal(
    toObservable(this.group).pipe(switchMap((group) => (group ? group.events.pipe(startWith(null)) : of(null)))),
  );

  hasControlError(errorKey: string): boolean {
    this.controlEvents();
    const ctrl = this.control();
    return ctrl.hasError(errorKey) && ctrl.touched;
  }

  hasGroupError(errorKey: string): boolean {
    this.controlEvents();
    this.groupEvents();
    const grp = this.group();
    const ctrl = this.control();
    return !!grp?.hasError(errorKey) && ctrl.touched;
  }
}

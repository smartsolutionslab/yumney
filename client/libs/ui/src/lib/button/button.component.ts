import { Component, ChangeDetectionStrategy, ViewEncapsulation, computed, input } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'danger-filled' | 'dashed' | 'link';

export type ButtonType = 'button' | 'submit' | 'reset';

@Component({
  selector: 'yn-button',
  imports: [NgTemplateOutlet, RouterLink],
  templateUrl: './button.component.html',
  styleUrl: './button.component.scss',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ButtonComponent {
  variant = input<ButtonVariant>('primary');
  type = input<ButtonType>('button');
  disabled = input(false);
  loading = input(false);
  showSpinner = input(false);
  routerLink = input<string | unknown[] | null | undefined>(undefined);
  ariaLabel = input<string | undefined>(undefined);
  testId = input<string | undefined>(undefined);
  extraClass = input<string>('');

  protected readonly className = computed(() => {
    const extra = this.extraClass();
    return extra ? `btn-${this.variant()} ${extra}` : `btn-${this.variant()}`;
  });
}

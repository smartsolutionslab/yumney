import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'yn-back-link',
  imports: [RouterLink, TranslocoPipe],
  template: `<a [routerLink]="route()" class="back-link">&larr; {{ label() | transloco }}</a>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BackLinkComponent {
  route = input.required<string>();
  label = input.required<string>();
}

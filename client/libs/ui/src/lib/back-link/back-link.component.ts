import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'yn-back-link',
  imports: [RouterLink, TranslocoPipe, LucideAngularModule],
  template: `<a [routerLink]="route()" class="back-link"><lucide-icon name="arrow-left" [size]="20" /> {{ label() | transloco }}</a>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BackLinkComponent {
  route = input.required<string>();
  label = input.required<string>();
}

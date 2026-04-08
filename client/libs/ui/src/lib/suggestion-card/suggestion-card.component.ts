import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { ROUTES } from '@yumney/shared/models';

@Component({
  selector: 'yn-suggestion-card',
  standalone: true,
  imports: [RouterLink, TranslocoModule],
  template: `
    <a
      class="suggestion-card"
      [routerLink]="ROUTES.recipes.detail(identifier())"
      *transloco="let t"
    >
      @if (imageUrl()) {
        <img class="suggestion-image" [src]="imageUrl()!" [alt]="title()" loading="lazy" />
      } @else {
        <div class="suggestion-image-placeholder"></div>
      }
      <div class="suggestion-info">
        <h3 class="suggestion-title">{{ title() }}</h3>
        @if (prepTimeMinutes()) {
          <span class="suggestion-time">{{
            t('dashboard.suggestions.prepTime', { minutes: prepTimeMinutes() })
          }}</span>
        }
        <span class="suggestion-reason">{{ reason() }}</span>
      </div>
    </a>
  `,
  styleUrl: './suggestion-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SuggestionCardComponent {
  protected readonly ROUTES = ROUTES;

  identifier = input.required<string>();
  title = input.required<string>();
  imageUrl = input<string | null>(null);
  prepTimeMinutes = input<number | null>(null);
  reason = input.required<string>();
}

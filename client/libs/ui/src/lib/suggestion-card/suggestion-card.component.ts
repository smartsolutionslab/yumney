import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'yn-suggestion-card',
  standalone: true,
  imports: [RouterLink],
  template: `
    <a class="suggestion-card" [routerLink]="['/recipes', identifier()]">
      @if (imageUrl()) {
        <img class="suggestion-image" [src]="imageUrl()!" [alt]="title()" loading="lazy" />
      } @else {
        <div class="suggestion-image-placeholder"></div>
      }
      <div class="suggestion-info">
        <h3 class="suggestion-title">{{ title() }}</h3>
        @if (prepTimeMinutes()) {
          <span class="suggestion-time">{{ prepTimeMinutes() }} min</span>
        }
        <span class="suggestion-reason">{{ reason() }}</span>
      </div>
    </a>
  `,
  styleUrl: './suggestion-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SuggestionCardComponent {
  identifier = input.required<string>();
  title = input.required<string>();
  imageUrl = input<string | null>(null);
  prepTimeMinutes = input<number | null>(null);
  reason = input.required<string>();
}

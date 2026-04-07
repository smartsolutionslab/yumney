import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { ROUTES } from '@yumney/shared/models';

export interface ActivityItem {
  type: string;
  recipeIdentifier: string | null;
  recipeTitle: string | null;
  occurredAt: string;
}

@Component({
  selector: 'yn-recent-activity',
  standalone: true,
  imports: [TranslocoModule, RouterLink, DatePipe],
  template: `
    <div class="recent-activity" *transloco="let t">
      <h3 class="activity-heading">{{ t('dashboard.recentActivity.title') }}</h3>
      @if (activities().length === 0) {
        <p class="activity-empty">{{ t('dashboard.recentActivity.empty') }}</p>
      } @else {
        <ul class="activity-list">
          @for (item of activities(); track item.occurredAt) {
            <li class="activity-item">
              <span class="activity-icon">{{ getIcon(item.type) }}</span>
              <div class="activity-detail">
                @if (item.recipeIdentifier) {
                  <a
                    [routerLink]="ROUTES.recipes.detail(item.recipeIdentifier)"
                    class="activity-link"
                  >
                    {{ item.recipeTitle ?? t('dashboard.recentActivity.unknownRecipe') }}
                  </a>
                } @else {
                  <span>{{ t('dashboard.recentActivity.types.' + item.type) }}</span>
                }
                <time class="activity-time">{{ item.occurredAt | date: 'short' }}</time>
              </div>
            </li>
          }
        </ul>
      }
    </div>
  `,
  styleUrl: './recent-activity.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecentActivityComponent {
  protected readonly ROUTES = ROUTES;

  activities = input.required<ActivityItem[]>();

  getIcon(type: string): string {
    const icons: Record<string, string> = {
      recipe_imported: '+',
      recipe_viewed: '○',
      recipe_edited: '✎',
      recipe_deleted: '×',
      shopping_list_created: '☐',
    };
    return icons[type] ?? '·';
  }
}

import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

export interface QuickAction {
  key: string;
  labelKey: string;
}

@Component({
  selector: 'yn-quick-actions',
  standalone: true,
  imports: [TranslocoModule],
  template: `
    <div class="quick-actions" *transloco="let t">
      @for (action of actions(); track action.key) {
        <button class="action-chip" (click)="actionClicked.emit(action.key)">
          {{ t(action.labelKey) }}
        </button>
      }
    </div>
  `,
  styleUrl: './quick-actions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuickActionsComponent {
  actions = input.required<QuickAction[]>();
  actionClicked = output<string>();
}

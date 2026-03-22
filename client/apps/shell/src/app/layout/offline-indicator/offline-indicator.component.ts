import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { OfflineStatusService } from './offline-status.service';

@Component({
  selector: 'yn-offline-indicator',
  imports: [TranslocoModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-container *transloco="let t">
      @if (offlineStatus.isOffline()) {
        <div class="offline-banner" role="alert">
          {{ t('layout.offlineIndicator.offline') }}
        </div>
      } @else if (offlineStatus.justCameOnline()) {
        <div class="online-banner" role="status">
          {{ t('layout.offlineIndicator.backOnline') }}
        </div>
      }
    </ng-container>
  `,
  styles: `
    .offline-banner {
      background-color: var(--yn-danger);
      color: #fff;
      text-align: center;
      padding: 8px;
      font-size: 14px;
    }

    .online-banner {
      background-color: var(--yn-success);
      color: #fff;
      text-align: center;
      padding: 8px;
      font-size: 14px;
    }
  `,
})
export class OfflineIndicatorComponent {
  protected offlineStatus = inject(OfflineStatusService);
}

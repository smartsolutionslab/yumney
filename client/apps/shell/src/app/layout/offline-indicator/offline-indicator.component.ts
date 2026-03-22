import { Component, ChangeDetectionStrategy, Injectable, signal, inject } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

@Injectable({ providedIn: 'root' })
export class OfflineStatusService {
  readonly isOffline = signal(!navigator.onLine);
  readonly justCameOnline = signal(false);

  constructor() {
    window.addEventListener('online', () => {
      this.isOffline.set(false);
      this.justCameOnline.set(true);
      setTimeout(() => this.justCameOnline.set(false), 3000);
    });
    window.addEventListener('offline', () => this.isOffline.set(true));
  }
}

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

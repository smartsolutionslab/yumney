import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { OfflineStatusService } from './offline-status.service';

@Component({
  selector: 'yn-offline-indicator',
  imports: [TranslocoModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './offline-indicator.component.html',
  styleUrl: './offline-indicator.component.scss',
})
export class OfflineIndicatorComponent {
  protected offlineStatus = inject(OfflineStatusService);
}

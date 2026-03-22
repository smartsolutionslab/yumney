import { Injectable, signal } from '@angular/core';
import { UI } from '@yumney/shared/models';

@Injectable({ providedIn: 'root' })
export class OfflineStatusService {
  readonly isOffline = signal(!navigator.onLine);
  readonly justCameOnline = signal(false);

  constructor() {
    window.addEventListener('online', () => {
      this.isOffline.set(false);
      this.justCameOnline.set(true);
      setTimeout(() => this.justCameOnline.set(false), UI.ONLINE_TOAST_DURATION_MS);
    });
    window.addEventListener('offline', () => this.isOffline.set(true));
  }
}

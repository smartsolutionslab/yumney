import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { RouterModule } from '@angular/router';
import {
  ChatPanelComponent,
  CommandFabComponent,
  HeaderComponent,
  ToastHostComponent,
} from '@yumney/ui';
import { OfflineIndicatorComponent } from './layout/offline-indicator/offline-indicator.component';
import { filter } from 'rxjs';

@Component({
  imports: [
    RouterModule,
    HeaderComponent,
    ChatPanelComponent,
    CommandFabComponent,
    OfflineIndicatorComponent,
    ToastHostComponent,
  ],
  selector: 'yn-root',
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App implements OnInit {
  private readonly swUpdate = inject(SwUpdate);

  ngOnInit(): void {
    if (!this.swUpdate.isEnabled) return;

    this.swUpdate.versionUpdates
      .pipe(filter((evt): evt is VersionReadyEvent => evt.type === 'VERSION_READY'))
      .subscribe(() => document.location.reload());
  }
}

import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule } from '@angular/router';
import { HeaderComponent } from '@yumney/ui';
import { OfflineIndicatorComponent } from './layout/offline-indicator/offline-indicator.component';

@Component({
  imports: [RouterModule, HeaderComponent, OfflineIndicatorComponent],
  selector: 'yn-root',
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}

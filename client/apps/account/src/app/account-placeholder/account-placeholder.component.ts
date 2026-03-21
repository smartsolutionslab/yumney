import { Component, ChangeDetectionStrategy } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

@Component({
  selector: 'yn-account-placeholder',
  imports: [TranslocoModule],
  templateUrl: './account-placeholder.component.html',
  styleUrl: './account-placeholder.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountPlaceholderComponent {}

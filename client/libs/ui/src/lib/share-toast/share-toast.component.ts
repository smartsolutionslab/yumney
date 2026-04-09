import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'yn-share-toast',
  standalone: true,
  imports: [TranslocoModule, LucideAngularModule],
  templateUrl: './share-toast.component.html',
  styleUrl: './share-toast.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShareToastComponent {
  url = input.required<string>();
  titleKey = input<string>('dashboard.share.toastTitle');
  dismissed = output<void>();
}

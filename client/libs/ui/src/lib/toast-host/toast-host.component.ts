import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { ToastService } from '@yumney/shared/models';

@Component({
  selector: 'yn-toast-host',
  standalone: true,
  imports: [TranslocoModule, LucideAngularModule],
  templateUrl: './toast-host.component.html',
  styleUrl: './toast-host.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastHostComponent {
  private service = inject(ToastService);

  protected toasts = this.service.toasts;

  protected dismiss(id: number): void {
    this.service.dismiss(id);
  }
}

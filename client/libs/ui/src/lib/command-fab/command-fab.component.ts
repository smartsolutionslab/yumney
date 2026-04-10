import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { ChatStateService } from '@yumney/shared/models';

@Component({
  selector: 'yn-command-fab',
  standalone: true,
  imports: [TranslocoModule, LucideAngularModule],
  templateUrl: './command-fab.component.html',
  styleUrl: './command-fab.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommandFabComponent {
  protected chatState = inject(ChatStateService);

  protected onToggle(): void {
    this.chatState.toggle();
  }
}

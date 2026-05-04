import { Component, ChangeDetectionStrategy, input, signal } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'yn-settings-card',
  imports: [TranslocoModule, LucideAngularModule],
  templateUrl: './settings-card.component.html',
  styleUrl: './settings-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsCardComponent {
  titleKey = input.required<string>();
  descriptionKey = input<string | null>(null);
  startOpen = input<boolean>(true);

  protected readonly isOpen = signal(true);

  ngOnInit(): void {
    this.isOpen.set(this.startOpen());
  }

  protected onHeaderClick(): void {
    this.isOpen.update((open) => !open);
  }
}

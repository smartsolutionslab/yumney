import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import type { UnitSystem } from '@yumney/shared/models';

@Component({
  selector: 'yn-unit-toggle',
  imports: [TranslocoModule],
  templateUrl: './unit-toggle.component.html',
  styleUrl: './unit-toggle.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnitToggleComponent {
  system = input.required<UnitSystem>();
  systemChange = output<UnitSystem>();

  protected onSelect(system: UnitSystem): void {
    if (system === this.system()) return;
    this.systemChange.emit(system);
  }
}

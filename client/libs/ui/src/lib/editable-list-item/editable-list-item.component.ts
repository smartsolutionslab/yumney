import { Component, ChangeDetectionStrategy, input, output, computed } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'yn-editable-list-item',
  imports: [TranslocoModule, LucideAngularModule],
  templateUrl: './editable-list-item.component.html',
  styleUrl: './editable-list-item.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditableListItemComponent {
  index = input.required<number>();
  total = input.required<number>();

  moveUp = output<void>();
  moveDown = output<void>();
  remove = output<void>();

  isFirst = computed(() => this.index() === 0);
  isLast = computed(() => this.index() === this.total() - 1);
}

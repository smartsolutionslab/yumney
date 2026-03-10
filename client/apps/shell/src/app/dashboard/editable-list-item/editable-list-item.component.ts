import { Component, ChangeDetectionStrategy, input, output, computed } from '@angular/core';

@Component({
  selector: 'yn-editable-list-item',
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

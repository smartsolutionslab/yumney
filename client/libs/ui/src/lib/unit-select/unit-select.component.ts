import { Component, ChangeDetectionStrategy, computed, input, signal, forwardRef, HostListener } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { type UnitGroupInfo, type KnownUnit, KNOWN_UNITS } from '@yumney/shared/models';
import { ClickOutsideDirective } from '../directives/click-outside.directive';

@Component({
  selector: 'yn-unit-select',
  imports: [TranslocoModule, LucideAngularModule, ClickOutsideDirective],
  templateUrl: './unit-select.component.html',
  styleUrl: './unit-select.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => UnitSelectComponent),
      multi: true,
    },
  ],
})
export class UnitSelectComponent implements ControlValueAccessor {
  unitGroups = input.required<UnitGroupInfo[]>();
  placeholder = input('');

  isOpen = signal(false);
  selectedValue = signal<string | null>(null);
  isDisabled = signal(false);
  focusedIndex = signal(-1);

  protected flatUnits = computed(() => this.unitGroups().flatMap((group) => group.units));

  // eslint-disable-next-line @typescript-eslint/no-empty-function
  private onChange: (value: string | null) => void = () => {};
  // eslint-disable-next-line @typescript-eslint/no-empty-function
  private onTouched: () => void = () => {};

  get selectedLabel(): string | null {
    const value = this.selectedValue();
    if (!value) return null;
    const unit = KNOWN_UNITS.find((knownUnit) => knownUnit.value === value);
    return unit?.labelKey ?? value;
  }

  onDismiss(): void {
    if (this.isOpen()) {
      this.isOpen.set(false);
      this.focusedIndex.set(-1);
    }
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (!this.isOpen()) return;
    const units = this.flatUnits();
    if (units.length === 0) return;

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.focusedIndex.update((index) => (index + 1) % units.length);
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.focusedIndex.update((index) => (index <= 0 ? units.length - 1 : index - 1));
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const idx = this.focusedIndex();
      if (idx >= 0 && idx < units.length) {
        this.selectUnit(units[idx]);
        this.focusedIndex.set(-1);
      }
    }
  }

  toggle(): void {
    if (this.isDisabled()) return;
    this.isOpen.update((open) => !open);
    this.focusedIndex.set(-1);
    this.onTouched();
  }

  selectUnit(unit: KnownUnit): void {
    this.selectedValue.set(unit.value);
    this.onChange(unit.value);
    this.isOpen.set(false);
  }

  clearSelection(event: MouseEvent): void {
    event.stopPropagation();
    this.selectedValue.set(null);
    this.onChange(null);
    this.isOpen.set(false);
  }

  writeValue(value: string | null): void {
    this.selectedValue.set(value);
  }

  registerOnChange(fn: (value: string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled.set(isDisabled);
  }
}

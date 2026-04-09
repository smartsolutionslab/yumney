import {
  Component,
  ChangeDetectionStrategy,
  input,
  signal,
  forwardRef,
  ElementRef,
  inject,
  HostListener,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { type UnitGroupInfo, type KnownUnit, KNOWN_UNITS } from '@yumney/shared/models';

@Component({
  selector: 'yn-unit-select',
  imports: [TranslocoModule, LucideAngularModule],
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

  private elementRef = inject(ElementRef);
  // eslint-disable-next-line @typescript-eslint/no-empty-function
  private onChange: (value: string | null) => void = () => {};
  // eslint-disable-next-line @typescript-eslint/no-empty-function
  private onTouched: () => void = () => {};

  get selectedLabel(): string | null {
    const value = this.selectedValue();
    if (!value) return null;
    const unit = KNOWN_UNITS.find((u) => u.value === value);
    return unit?.labelKey ?? value;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.isOpen()) {
      this.isOpen.set(false);
    }
  }

  toggle(): void {
    if (this.isDisabled()) return;
    this.isOpen.update((open) => !open);
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

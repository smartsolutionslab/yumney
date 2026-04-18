import { provideYumneyIcons } from '../icons/provide-icons';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, signal, viewChild } from '@angular/core';
import { UnitSelectComponent } from './unit-select.component';
import { setupTranslocoTesting } from '@yumney/shared/models';
import type { UnitGroupInfo } from '@yumney/shared/models';

const en = {
  dashboard: {
    preview: {
      unit: 'Unit',
      clearUnit: 'Clear unit',
    },
  },
  units: {
    group: {
      volume: 'Volume',
      weight: 'Weight',
    },
    ml: 'ml',
    l: 'l',
    g: 'g',
    kg: 'kg',
  },
};

const mockUnitGroups: UnitGroupInfo[] = [
  {
    key: 'volume',
    labelKey: 'units.group.volume',
    units: [
      { value: 'ml', labelKey: 'units.ml', group: 'volume' },
      { value: 'l', labelKey: 'units.l', group: 'volume' },
    ],
  },
  {
    key: 'weight',
    labelKey: 'units.group.weight',
    units: [
      { value: 'g', labelKey: 'units.g', group: 'weight' },
      { value: 'kg', labelKey: 'units.kg', group: 'weight' },
    ],
  },
];

@Component({
  template: `
    <yn-unit-select [unitGroups]="unitGroups()" [placeholder]="placeholder()" />
  `,
  imports: [UnitSelectComponent],
})
class TestHostComponent {
  unitGroups = signal<UnitGroupInfo[]>(mockUnitGroups);
  placeholder = signal('');
  unitSelect = viewChild(UnitSelectComponent);
}

describe('UnitSelectComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let host: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent, setupTranslocoTesting(en)],
      providers: [provideYumneyIcons()],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render the toggle button', () => {
    const toggle = fixture.nativeElement.querySelector('.unit-toggle');
    expect(toggle).toBeTruthy();
  });

  it('should show placeholder when no value selected', () => {
    const placeholder = fixture.nativeElement.querySelector('.unit-placeholder');
    expect(placeholder.textContent).toContain('Unit');
  });

  it('should open dropdown when toggle is clicked', () => {
    const toggle = fixture.nativeElement.querySelector('.unit-toggle');
    toggle.click();
    fixture.detectChanges();

    const menu = fixture.nativeElement.querySelector('.unit-menu');
    expect(menu).toBeTruthy();
  });

  it('should render unit groups in dropdown', () => {
    const toggle = fixture.nativeElement.querySelector('.unit-toggle');
    toggle.click();
    fixture.detectChanges();

    const groupLabels = fixture.nativeElement.querySelectorAll('.unit-group-label');
    expect(groupLabels.length).toBe(2);
    expect(groupLabels[0].textContent).toContain('Volume');
    expect(groupLabels[1].textContent).toContain('Weight');
  });

  it('should select a unit when clicked', () => {
    const toggle = fixture.nativeElement.querySelector('.unit-toggle');
    toggle.click();
    fixture.detectChanges();

    const options = fixture.nativeElement.querySelectorAll('.unit-menu-item');
    options[0].click();
    fixture.detectChanges();

    const label = fixture.nativeElement.querySelector('.unit-label');
    expect(label.textContent).toContain('ml');
  });

  it('should close dropdown after selecting a unit', () => {
    const toggle = fixture.nativeElement.querySelector('.unit-toggle');
    toggle.click();
    fixture.detectChanges();

    const options = fixture.nativeElement.querySelectorAll('.unit-menu-item');
    options[0].click();
    fixture.detectChanges();

    const menu = fixture.nativeElement.querySelector('.unit-menu');
    expect(menu).toBeNull();
  });

  it('should close dropdown on escape key', () => {
    const toggle = fixture.nativeElement.querySelector('.unit-toggle');
    toggle.click();
    fixture.detectChanges();

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    fixture.detectChanges();

    const menu = fixture.nativeElement.querySelector('.unit-menu');
    expect(menu).toBeNull();
  });

  it('should show clear button when a value is selected', () => {
    const component = host.unitSelect()!;
    component.selectUnit(mockUnitGroups[0].units[0]);
    fixture.detectChanges();

    const clearBtn = fixture.nativeElement.querySelector('.unit-clear');
    expect(clearBtn).toBeTruthy();
  });

  it('should clear selection when clear button is clicked', () => {
    const component = host.unitSelect()!;
    component.selectUnit(mockUnitGroups[0].units[0]);
    fixture.detectChanges();

    const clearBtn = fixture.nativeElement.querySelector('.unit-clear');
    clearBtn.click();
    fixture.detectChanges();

    const placeholder = fixture.nativeElement.querySelector('.unit-placeholder');
    expect(placeholder).toBeTruthy();
    expect(placeholder.textContent).toContain('Unit');
  });

  it('should not open dropdown when disabled', () => {
    const component = host.unitSelect()!;
    component.setDisabledState(true);
    fixture.detectChanges();

    const toggle = fixture.nativeElement.querySelector('.unit-toggle');
    toggle.click();
    fixture.detectChanges();

    const menu = fixture.nativeElement.querySelector('.unit-menu');
    expect(menu).toBeNull();
  });

  it('should close dropdown when clicking outside', () => {
    const toggle = fixture.nativeElement.querySelector('.unit-toggle');
    toggle.click();
    fixture.detectChanges();

    document.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();

    const menu = fixture.nativeElement.querySelector('.unit-menu');
    expect(menu).toBeNull();
  });
});

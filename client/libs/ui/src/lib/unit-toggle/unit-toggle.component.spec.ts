import { ComponentFixture, TestBed } from '@angular/core/testing';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { UnitToggleComponent } from './unit-toggle.component';

const en = {
  shared: {
    unitToggle: {
      label: 'Unit system',
      metric: 'Metric',
      imperial: 'Imperial',
    },
  },
};

describe('UnitToggleComponent', () => {
  let fixture: ComponentFixture<UnitToggleComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [UnitToggleComponent, setupTranslocoTesting(en)],
    });
    fixture = TestBed.createComponent(UnitToggleComponent);
    fixture.componentRef.setInput('system', 'metric');
    fixture.detectChanges();
  });

  it('marks the selected system aria-checked', () => {
    const metric = fixture.nativeElement.querySelector('[data-testid="unit-toggle-metric"]') as HTMLButtonElement;
    const imperial = fixture.nativeElement.querySelector('[data-testid="unit-toggle-imperial"]') as HTMLButtonElement;
    expect(metric.getAttribute('aria-checked')).toBe('true');
    expect(imperial.getAttribute('aria-checked')).toBe('false');
  });

  it('emits systemChange when the other option is clicked', () => {
    const handler = vi.fn();
    fixture.componentInstance.systemChange.subscribe(handler);

    const imperial = fixture.nativeElement.querySelector('[data-testid="unit-toggle-imperial"]') as HTMLButtonElement;
    imperial.click();

    expect(handler).toHaveBeenCalledWith('imperial');
  });

  it('does not emit when the already-selected option is clicked', () => {
    const handler = vi.fn();
    fixture.componentInstance.systemChange.subscribe(handler);

    const metric = fixture.nativeElement.querySelector('[data-testid="unit-toggle-metric"]') as HTMLButtonElement;
    metric.click();

    expect(handler).not.toHaveBeenCalled();
  });

  it('reflects an updated input', () => {
    fixture.componentRef.setInput('system', 'imperial');
    fixture.detectChanges();

    const imperial = fixture.nativeElement.querySelector('[data-testid="unit-toggle-imperial"]') as HTMLButtonElement;
    expect(imperial.getAttribute('aria-checked')).toBe('true');
  });
});

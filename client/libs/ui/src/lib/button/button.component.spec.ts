import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ButtonComponent, type ButtonVariant } from './button.component';

@Component({
  imports: [ButtonComponent],
  template: `<yn-button
    [variant]="variant"
    [disabled]="disabled"
    [loading]="loading"
    [showSpinner]="showSpinner"
    [type]="type"
    [routerLink]="routerLink"
    [ariaLabel]="ariaLabel"
    [testId]="testId"
    [extraClass]="extraClass"
  >
    <span>Click me</span>
  </yn-button>`,
})
class HostComponent {
  variant: ButtonVariant = 'primary';
  disabled = false;
  loading = false;
  showSpinner = false;
  type: 'button' | 'submit' | 'reset' = 'button';
  routerLink: string | undefined = undefined;
  ariaLabel: string | undefined = undefined;
  testId: string | undefined = undefined;
  extraClass = '';
}

describe('ButtonComponent', () => {
  let fixture: ComponentFixture<HostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
  });

  // ── Variant rendering ──────────────────────────────────────────────────────

  it('should apply the btn-primary class for the primary variant', () => {
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.classList.contains('btn-primary')).toBe(true);
  });

  it('should apply the btn-secondary class for the secondary variant', () => {
    fixture.componentInstance.variant = 'secondary';

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.classList.contains('btn-secondary')).toBe(true);
  });

  it('should apply the btn-danger class for the danger variant', () => {
    fixture.componentInstance.variant = 'danger';

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.classList.contains('btn-danger')).toBe(true);
  });

  // ── Button vs link rendering ───────────────────────────────────────────────

  it('should render a button element by default', () => {
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    const anchor = fixture.nativeElement.querySelector('a');
    expect(button).toBeTruthy();
    expect(anchor).toBeFalsy();
  });

  it('should render an anchor element when routerLink is provided', () => {
    fixture.componentInstance.routerLink = '/recipes';

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    const anchor = fixture.nativeElement.querySelector('a');
    expect(button).toBeFalsy();
    expect(anchor).toBeTruthy();
    expect(anchor.getAttribute('href')).toContain('/recipes');
  });

  // ── Disabled / loading state ───────────────────────────────────────────────

  it('should disable the button when disabled is true', () => {
    fixture.componentInstance.disabled = true;

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.disabled).toBe(true);
  });

  it('should disable the button when loading is true', () => {
    fixture.componentInstance.loading = true;

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.disabled).toBe(true);
  });

  it('should mark anchor with aria-disabled when disabled and using routerLink', () => {
    fixture.componentInstance.routerLink = '/recipes';
    fixture.componentInstance.disabled = true;

    fixture.detectChanges();

    const anchor = fixture.nativeElement.querySelector('a');
    expect(anchor.getAttribute('aria-disabled')).toBe('true');
  });

  // ── Spinner ────────────────────────────────────────────────────────────────

  it('should render a spinner when loading and showSpinner are both true', () => {
    fixture.componentInstance.loading = true;
    fixture.componentInstance.showSpinner = true;

    fixture.detectChanges();

    const spinner = fixture.nativeElement.querySelector('.btn-spinner');
    expect(spinner).toBeTruthy();
  });

  it('should not render a spinner when loading but showSpinner is false', () => {
    fixture.componentInstance.loading = true;

    fixture.detectChanges();

    const spinner = fixture.nativeElement.querySelector('.btn-spinner');
    expect(spinner).toBeFalsy();
  });

  // ── Type / extraClass / aria / testId ──────────────────────────────────────

  it('should default the button type to "button"', () => {
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('type')).toBe('button');
  });

  it('should respect a submit type input', () => {
    fixture.componentInstance.type = 'submit';

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('type')).toBe('submit');
  });

  it('should append extra classes alongside the variant class', () => {
    fixture.componentInstance.extraClass = 'import-another';

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.classList.contains('btn-primary')).toBe(true);
    expect(button.classList.contains('import-another')).toBe(true);
  });

  it('should mirror the testId input as data-testid on the rendered element', () => {
    fixture.componentInstance.testId = 'save-button';

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('data-testid')).toBe('save-button');
  });

  it('should mirror the ariaLabel input as aria-label on the rendered element', () => {
    fixture.componentInstance.ariaLabel = 'Save the recipe';

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('aria-label')).toBe('Save the recipe');
  });

  // ── Content projection ─────────────────────────────────────────────────────

  it('should project content into the rendered element', () => {
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.textContent).toContain('Click me');
  });
});

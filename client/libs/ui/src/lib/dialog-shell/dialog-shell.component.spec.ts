import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DialogShellComponent, type DialogRole, type DialogSize } from './dialog-shell.component';

@Component({
  imports: [DialogShellComponent],
  template: `<yn-dialog-shell
    [size]="size()"
    [role]="role()"
    [labelledBy]="labelledBy()"
    [testId]="testId()"
    [cancelOnBackdrop]="cancelOnBackdrop()"
    [cancelOnEscape]="cancelOnEscape()"
    (cancelled)="cancelledCount = cancelledCount + 1"
  >
    <h2 [id]="labelledBy() ?? null">Dialog content</h2>
    <p>Body</p>
  </yn-dialog-shell>`,
})
class HostComponent {
  size = signal<DialogSize>('md');
  role = signal<DialogRole>('dialog');
  labelledBy = signal<string | undefined>(undefined);
  testId = signal<string | undefined>(undefined);
  cancelOnBackdrop = signal(true);
  cancelOnEscape = signal(true);
  cancelledCount = 0;
}

describe('DialogShellComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
  });

  // ── Variants ───────────────────────────────────────────────────────────────

  it('should apply the medium size class by default', () => {
    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector('.yn-dialog');
    expect(dialog.classList.contains('yn-dialog--md')).toBe(true);
  });

  it('should apply the small size class when size is sm', () => {
    host.size.set('sm');

    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector('.yn-dialog');
    expect(dialog.classList.contains('yn-dialog--sm')).toBe(true);
  });

  it('should apply the large size class when size is lg', () => {
    host.size.set('lg');

    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector('.yn-dialog');
    expect(dialog.classList.contains('yn-dialog--lg')).toBe(true);
  });

  // ── ARIA wiring ────────────────────────────────────────────────────────────

  it('should set role="dialog" and aria-modal="true" by default', () => {
    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector('.yn-dialog');
    expect(dialog.getAttribute('role')).toBe('dialog');
    expect(dialog.getAttribute('aria-modal')).toBe('true');
  });

  it('should expose role="alertdialog" when role is alertdialog', () => {
    host.role.set('alertdialog');

    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector('.yn-dialog');
    expect(dialog.getAttribute('role')).toBe('alertdialog');
  });

  it('should mirror labelledBy as aria-labelledby on the dialog', () => {
    host.labelledBy.set('header-title');

    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector('.yn-dialog');
    expect(dialog.getAttribute('aria-labelledby')).toBe('header-title');
  });

  it('should mirror testId as data-testid on the dialog', () => {
    host.testId.set('my-dialog');

    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector('.yn-dialog');
    expect(dialog.getAttribute('data-testid')).toBe('my-dialog');
  });

  // ── Backdrop click ─────────────────────────────────────────────────────────

  it('should emit cancelled when the overlay backdrop is clicked', () => {
    fixture.detectChanges();

    const overlay = fixture.nativeElement.querySelector('.yn-dialog-overlay');
    overlay.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(host.cancelledCount).toBe(1);
  });

  it('should not emit cancelled when clicks bubble from inside the dialog', () => {
    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector('.yn-dialog');
    dialog.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(host.cancelledCount).toBe(0);
  });

  it('should suppress backdrop cancellation when cancelOnBackdrop is false', () => {
    host.cancelOnBackdrop.set(false);

    fixture.detectChanges();

    const overlay = fixture.nativeElement.querySelector('.yn-dialog-overlay');
    overlay.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(host.cancelledCount).toBe(0);
  });

  // ── Escape key ─────────────────────────────────────────────────────────────

  it('should emit cancelled when Escape is pressed on the document', () => {
    fixture.detectChanges();

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(host.cancelledCount).toBe(1);
  });

  it('should suppress Escape cancellation when cancelOnEscape is false', () => {
    host.cancelOnEscape.set(false);

    fixture.detectChanges();

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(host.cancelledCount).toBe(0);
  });

  // ── Content projection ────────────────────────────────────────────────────

  it('should project content into the dialog element', () => {
    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector('.yn-dialog');
    expect(dialog.textContent).toContain('Dialog content');
    expect(dialog.textContent).toContain('Body');
  });
});

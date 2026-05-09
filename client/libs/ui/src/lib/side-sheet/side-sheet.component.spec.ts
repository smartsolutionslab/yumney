import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  SideSheetComponent,
  type SideSheetPosition,
  type SideSheetSize,
} from './side-sheet.component';

@Component({
  imports: [SideSheetComponent],
  template: `<yn-side-sheet
    [size]="size()"
    [position]="position()"
    [labelledBy]="labelledBy()"
    [testId]="testId()"
    [cancelOnBackdrop]="cancelOnBackdrop()"
    [cancelOnEscape]="cancelOnEscape()"
    (cancelled)="cancelledCount = cancelledCount + 1"
  >
    <h2 [id]="labelledBy() ?? null">Sheet content</h2>
    <p>Body</p>
  </yn-side-sheet>`,
})
class HostComponent {
  size = signal<SideSheetSize>('md');
  position = signal<SideSheetPosition>('right');
  labelledBy = signal<string | undefined>(undefined);
  testId = signal<string | undefined>(undefined);
  cancelOnBackdrop = signal(true);
  cancelOnEscape = signal(true);
  cancelledCount = 0;
}

describe('SideSheetComponent', () => {
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

  it('should apply the medium size and right position by default', () => {
    fixture.detectChanges();

    const sheet = fixture.nativeElement.querySelector('.yn-side-sheet');
    expect(sheet.classList.contains('yn-side-sheet--md')).toBe(true);
    expect(sheet.classList.contains('yn-side-sheet--right')).toBe(true);
  });

  it('should apply the small size class when size is sm', () => {
    host.size.set('sm');

    fixture.detectChanges();

    const sheet = fixture.nativeElement.querySelector('.yn-side-sheet');
    expect(sheet.classList.contains('yn-side-sheet--sm')).toBe(true);
  });

  it('should apply the large size class when size is lg', () => {
    host.size.set('lg');

    fixture.detectChanges();

    const sheet = fixture.nativeElement.querySelector('.yn-side-sheet');
    expect(sheet.classList.contains('yn-side-sheet--lg')).toBe(true);
  });

  it('should apply the left position class when position is left', () => {
    host.position.set('left');

    fixture.detectChanges();

    const sheet = fixture.nativeElement.querySelector('.yn-side-sheet');
    expect(sheet.classList.contains('yn-side-sheet--left')).toBe(true);
  });

  // ── ARIA wiring ────────────────────────────────────────────────────────────

  it('should set role="dialog" and aria-modal="true"', () => {
    fixture.detectChanges();

    const sheet = fixture.nativeElement.querySelector('.yn-side-sheet');
    expect(sheet.getAttribute('role')).toBe('dialog');
    expect(sheet.getAttribute('aria-modal')).toBe('true');
  });

  it('should mirror labelledBy as aria-labelledby on the sheet', () => {
    host.labelledBy.set('header-title');

    fixture.detectChanges();

    const sheet = fixture.nativeElement.querySelector('.yn-side-sheet');
    expect(sheet.getAttribute('aria-labelledby')).toBe('header-title');
  });

  it('should mirror testId as data-testid on the sheet', () => {
    host.testId.set('my-sheet');

    fixture.detectChanges();

    const sheet = fixture.nativeElement.querySelector('.yn-side-sheet');
    expect(sheet.getAttribute('data-testid')).toBe('my-sheet');
  });

  // ── Backdrop click ─────────────────────────────────────────────────────────

  it('should emit cancelled when the overlay backdrop is clicked', () => {
    fixture.detectChanges();

    const overlay = fixture.nativeElement.querySelector('.yn-side-sheet-overlay');
    overlay.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(host.cancelledCount).toBe(1);
  });

  it('should not emit cancelled when clicks bubble from inside the sheet', () => {
    fixture.detectChanges();

    const sheet = fixture.nativeElement.querySelector('.yn-side-sheet');
    sheet.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(host.cancelledCount).toBe(0);
  });

  it('should suppress backdrop cancellation when cancelOnBackdrop is false', () => {
    host.cancelOnBackdrop.set(false);

    fixture.detectChanges();

    const overlay = fixture.nativeElement.querySelector('.yn-side-sheet-overlay');
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

  it('should project content into the sheet element', () => {
    fixture.detectChanges();

    const sheet = fixture.nativeElement.querySelector('.yn-side-sheet');
    expect(sheet.textContent).toContain('Sheet content');
    expect(sheet.textContent).toContain('Body');
  });
});

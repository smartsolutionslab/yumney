import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ClickOutsideDirective } from './click-outside.directive';

@Component({
  imports: [ClickOutsideDirective],
  template: `
    <div
      ynClickOutside
      (clickOutside)="outsideCount = outsideCount + 1"
      (escape)="escapeCount = escapeCount + 1"
      data-testid="wrapper"
    >
      <button data-testid="inner-button">click me</button>
    </div>
    <button data-testid="external-button">outside</button>
  `,
})
class HostComponent {
  outsideCount = 0;
  escapeCount = 0;
}

describe('ClickOutsideDirective', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── clickOutside output ────────────────────────────────────────────────────

  it('should not emit clickOutside when a click lands inside the host element', () => {
    const inner = fixture.nativeElement.querySelector('[data-testid="inner-button"]');
    inner.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(host.outsideCount).toBe(0);
  });

  it('should emit clickOutside when a click lands on a sibling outside the host', () => {
    const external = fixture.nativeElement.querySelector('[data-testid="external-button"]');
    external.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(host.outsideCount).toBe(1);
  });

  it('should emit clickOutside when a click lands on the document body', () => {
    document.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(host.outsideCount).toBeGreaterThanOrEqual(1);
  });

  // ── escape output ──────────────────────────────────────────────────────────

  it('should emit escape when Escape is pressed on the document', () => {
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(host.escapeCount).toBe(1);
  });

  it('should not emit escape on other keys', () => {
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

    expect(host.escapeCount).toBe(0);
  });
});

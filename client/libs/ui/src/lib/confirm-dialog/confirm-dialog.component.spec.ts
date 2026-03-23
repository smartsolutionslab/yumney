import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfirmDialogComponent } from './confirm-dialog.component';
import { Component, signal } from '@angular/core';

@Component({
  template: `
    <yn-confirm-dialog
      [message]="message()"
      [confirmLabel]="confirmLabel()"
      [cancelLabel]="cancelLabel()"
      (confirmed)="onConfirmed()"
      (cancelled)="onCancelled()"
    />
  `,
  imports: [ConfirmDialogComponent],
})
class TestHostComponent {
  message = signal('Are you sure?');
  confirmLabel = signal('Yes');
  cancelLabel = signal('No');
  confirmedCount = 0;
  cancelledCount = 0;

  onConfirmed(): void {
    this.confirmedCount++;
  }

  onCancelled(): void {
    this.cancelledCount++;
  }
}

describe('ConfirmDialogComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let host: TestHostComponent;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TestHostComponent],
    });

    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render the message', () => {
    const message = fixture.nativeElement.querySelector('.confirm-message');
    expect(message.textContent).toContain('Are you sure?');
  });

  it('should render custom button labels', () => {
    const buttons = fixture.nativeElement.querySelectorAll('button');
    expect(buttons[0].textContent).toContain('No');
    expect(buttons[1].textContent).toContain('Yes');
  });

  it('should emit confirmed on confirm button click', () => {
    const confirmButton = fixture.nativeElement.querySelector('.btn-danger');
    confirmButton.click();

    expect(host.confirmedCount).toBe(1);
  });

  it('should emit cancelled on cancel button click', () => {
    const cancelButton = fixture.nativeElement.querySelector('.btn-secondary');
    cancelButton.click();

    expect(host.cancelledCount).toBe(1);
  });

  it('should emit cancelled on overlay click', () => {
    const overlay = fixture.nativeElement.querySelector('.confirm-overlay');
    overlay.click();

    expect(host.cancelledCount).toBe(1);
  });

  it('should not emit cancelled when clicking inside dialog', () => {
    const dialog = fixture.nativeElement.querySelector('.confirm-dialog');
    dialog.click();

    expect(host.cancelledCount).toBe(0);
  });

  it('should have alertdialog role and aria-modal', () => {
    const dialog = fixture.nativeElement.querySelector('.confirm-dialog');
    expect(dialog.getAttribute('role')).toBe('alertdialog');
    expect(dialog.getAttribute('aria-modal')).toBe('true');
  });

  it('should use default labels when not provided', () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [DefaultLabelHostComponent],
    });

    const defaultFixture = TestBed.createComponent(DefaultLabelHostComponent);
    defaultFixture.detectChanges();

    const buttons = defaultFixture.nativeElement.querySelectorAll('button');
    expect(buttons[0].textContent).toContain('Cancel');
    expect(buttons[1].textContent).toContain('OK');
  });
});

@Component({
  template: `
    <yn-confirm-dialog [message]="'Test message'" (confirmed)="noop()" (cancelled)="noop()" />
  `,
  imports: [ConfirmDialogComponent],
})
class DefaultLabelHostComponent {
  noop(): void {}
}

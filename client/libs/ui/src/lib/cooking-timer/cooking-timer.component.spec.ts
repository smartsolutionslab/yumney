import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';
import { CookingTimerComponent } from './cooking-timer.component';
import { setupTranslocoTesting } from '@yumney/shared/models';
import type { CookingTimer } from '@yumney/shared/models';

const en = {
  recipes: {
    cook: {
      timer: {
        cancel: 'Cancel',
        done: 'Timer done',
      },
    },
  },
};

const mockTimer: CookingTimer = {
  id: 'timer-1',
  name: 'Boil pasta',
  totalSeconds: 600,
  remainingSeconds: 345,
  status: 'running',
};

@Component({
  template: ` <yn-cooking-timer [timer]="timer()" (cancel)="onCancel($event)" /> `,
  imports: [CookingTimerComponent],
})
class TestHostComponent {
  timer = signal<CookingTimer>(mockTimer);
  onCancel = vi.fn();
}

describe('CookingTimerComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let host: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent, setupTranslocoTesting(en)],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render the timer display', () => {
    const timerText = fixture.nativeElement.querySelector('.timer-text');
    expect(timerText).toBeTruthy();
  });

  it('should display correct time format for 345 seconds', () => {
    const timerText = fixture.nativeElement.querySelector('.timer-text');
    expect(timerText.textContent.trim()).toBe('5:45');
  });

  it('should display 0:00 when remaining seconds is zero', () => {
    host.timer.set({ ...mockTimer, remainingSeconds: 0 });
    fixture.detectChanges();

    const timerText = fixture.nativeElement.querySelector('.timer-text');
    expect(timerText.textContent.trim()).toBe('0:00');
  });

  it('should pad single-digit seconds with a leading zero', () => {
    host.timer.set({ ...mockTimer, remainingSeconds: 65 });
    fixture.detectChanges();

    const timerText = fixture.nativeElement.querySelector('.timer-text');
    expect(timerText.textContent.trim()).toBe('1:05');
  });

  it('should display the timer name', () => {
    const timerName = fixture.nativeElement.querySelector('.timer-name');
    expect(timerName.textContent).toContain('Boil pasta');
  });

  it('should emit cancel with timer id when cancel button is clicked', () => {
    const cancelBtn = fixture.nativeElement.querySelector('.timer-cancel');
    cancelBtn.click();

    expect(host.onCancel).toHaveBeenCalledWith('timer-1');
  });

  it('should render the cancel button with correct text', () => {
    const cancelBtn = fixture.nativeElement.querySelector('.timer-cancel');
    expect(cancelBtn.textContent).toContain('Cancel');
  });

  it('should apply completed class when timer status is completed', () => {
    host.timer.set({ ...mockTimer, remainingSeconds: 0, status: 'completed' });
    fixture.detectChanges();

    const timerEl = fixture.nativeElement.querySelector('.cooking-timer');
    expect(timerEl.classList.contains('completed')).toBe(true);
  });

  it('should not apply completed class when timer is running', () => {
    const timerEl = fixture.nativeElement.querySelector('.cooking-timer');
    expect(timerEl.classList.contains('completed')).toBe(false);
  });

  it('should render the SVG ring', () => {
    const svg = fixture.nativeElement.querySelector('.timer-ring');
    expect(svg).toBeTruthy();
  });
});

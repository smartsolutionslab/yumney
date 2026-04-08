import { TestBed } from '@angular/core/testing';
import { CookingTimerService } from './cooking-timer.service';
import { VoiceService } from './voice.service';

describe('CookingTimerService', () => {
  let service: CookingTimerService;
  let voiceSpeak: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    vi.useFakeTimers();
    voiceSpeak = vi.fn();
    TestBed.configureTestingModule({
      providers: [CookingTimerService, { provide: VoiceService, useValue: { speak: voiceSpeak } }],
    });
    service = TestBed.inject(CookingTimerService);
  });

  afterEach(() => {
    service.cancelAll();
    vi.useRealTimers();
  });

  it('should start a timer with correct totalSeconds', () => {
    const id = service.start('Pasta', 1);
    const timer = service.all().find((t) => t.id === id);

    expect(timer?.totalSeconds).toBe(60);
  });

  it('should set initial remainingSeconds equal to totalSeconds', () => {
    const id = service.start('Pasta', 2);
    const timer = service.all().find((t) => t.id === id);

    expect(timer?.remainingSeconds).toBe(120);
  });

  it('should mark hasActive true while a timer is running', () => {
    service.start('Pasta', 1);

    expect(service.hasActive()).toBe(true);
  });

  it('should decrement remainingSeconds each second', () => {
    const id = service.start('Pasta', 1);

    vi.advanceTimersByTime(3000);
    const timer = service.all().find((t) => t.id === id);

    expect(timer?.remainingSeconds).toBe(57);
  });

  it('should mark timer completed when countdown reaches zero', () => {
    const id = service.start('Quick', 1 / 60); // 1 second

    vi.advanceTimersByTime(1100);
    const timer = service.all().find((t) => t.id === id);

    expect(timer?.status).toBe('completed');
  });

  it('should announce completion via voice service', () => {
    service.start('Quick', 1 / 60);

    vi.advanceTimersByTime(1100);

    expect(voiceSpeak).toHaveBeenCalledWith('Timer done');
  });

  it('should remove timer when canceled', () => {
    const id = service.start('Pasta', 5);

    service.cancel(id);

    expect(service.all().find((t) => t.id === id)).toBeUndefined();
  });

  it('should cancel all timers via cancelAll', () => {
    service.start('A', 1);
    service.start('B', 1);

    service.cancelAll();

    expect(service.all()).toHaveLength(0);
  });
});

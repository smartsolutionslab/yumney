import { Injectable, signal, computed, inject } from '@angular/core';
import { VoiceService } from './voice.service';

export interface CookingTimer {
  id: string;
  name: string;
  totalSeconds: number;
  remainingSeconds: number;
  status: 'running' | 'completed';
}

const VIBRATION_PATTERN = [200, 100, 200, 100, 200];

@Injectable({ providedIn: 'root' })
export class CookingTimerService {
  private readonly timers = signal<CookingTimer[]>([]);
  private readonly intervals = new Map<string, ReturnType<typeof setInterval>>();
  private voice = inject(VoiceService);

  readonly all = computed(() => this.timers());
  readonly hasActive = computed(() => this.timers().some((t) => t.status === 'running'));

  start(name: string, minutes: number): string {
    const id = `timer-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
    const totalSeconds = Math.max(1, Math.floor(minutes * 60));
    const timer: CookingTimer = {
      id,
      name,
      totalSeconds,
      remainingSeconds: totalSeconds,
      status: 'running',
    };
    this.timers.update((list) => [...list, timer]);

    const interval = setInterval(() => this.tick(id), 1000);
    this.intervals.set(id, interval);
    return id;
  }

  cancel(id: string): void {
    this.clearInterval(id);
    this.timers.update((list) => list.filter((t) => t.id !== id));
  }

  cancelAll(): void {
    for (const id of this.intervals.keys()) {
      this.clearInterval(id);
    }
    this.timers.set([]);
  }

  private tick(id: string): void {
    const current = this.timers().find((t) => t.id === id);
    if (!current || current.status === 'completed') return;

    const next = current.remainingSeconds - 1;
    if (next <= 0) {
      this.complete(id);
    } else {
      this.timers.update((list) =>
        list.map((t) => (t.id === id ? { ...t, remainingSeconds: next } : t)),
      );
    }
  }

  private complete(id: string): void {
    this.clearInterval(id);
    this.timers.update((list) =>
      list.map((t) =>
        t.id === id ? { ...t, remainingSeconds: 0, status: 'completed' as const } : t,
      ),
    );
    this.triggerHaptics();
    this.voice.speak('Timer done');
  }

  private clearInterval(id: string): void {
    const handle = this.intervals.get(id);
    if (handle !== undefined) {
      clearInterval(handle);
      this.intervals.delete(id);
    }
  }

  private triggerHaptics(): void {
    if (typeof navigator !== 'undefined' && 'vibrate' in navigator) {
      try {
        navigator.vibrate(VIBRATION_PATTERN);
      } catch {
        /* ignore */
      }
    }
  }
}

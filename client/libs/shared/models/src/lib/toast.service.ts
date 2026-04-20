import { Injectable, signal } from '@angular/core';

export type ToastKind = 'info' | 'success' | 'warning' | 'error';

export interface Toast {
  id: number;
  kind: ToastKind;
  messageKey: string;
  params?: Record<string, unknown>;
  durationMs: number;
}

export interface ShowToastOptions {
  kind?: ToastKind;
  messageKey: string;
  params?: Record<string, unknown>;
  durationMs?: number;
}

const DEFAULT_DURATION_MS = 5000;

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly _toasts = signal<Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();

  private nextId = 1;

  show(options: ShowToastOptions): number {
    const id = this.nextId++;
    const toast: Toast = {
      id,
      kind: options.kind ?? 'info',
      messageKey: options.messageKey,
      params: options.params,
      durationMs: options.durationMs ?? DEFAULT_DURATION_MS,
    };
    this._toasts.update((list) => [...list, toast]);

    if (toast.durationMs > 0) {
      setTimeout(() => this.dismiss(id), toast.durationMs);
    }
    return id;
  }

  success(messageKey: string, params?: Record<string, unknown>): number {
    return this.show({ kind: 'success', messageKey, params });
  }

  error(messageKey: string, params?: Record<string, unknown>): number {
    return this.show({ kind: 'error', messageKey, params });
  }

  info(messageKey: string, params?: Record<string, unknown>): number {
    return this.show({ kind: 'info', messageKey, params });
  }

  warning(messageKey: string, params?: Record<string, unknown>): number {
    return this.show({ kind: 'warning', messageKey, params });
  }

  dismiss(id: number): void {
    this._toasts.update((list) => list.filter((t) => t.id !== id));
  }

  clear(): void {
    this._toasts.set([]);
  }
}

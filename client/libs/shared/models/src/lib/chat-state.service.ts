import { Injectable, signal } from '@angular/core';
import type { ChatMessage } from '@yumney/shared/chat-api';

@Injectable({ providedIn: 'root' })
export class ChatStateService {
  readonly isOpen = signal(false);
  readonly messages = signal<ChatMessage[]>([]);
  readonly isThinking = signal(false);

  open(): void {
    this.isOpen.set(true);
  }

  close(): void {
    this.isOpen.set(false);
  }

  toggle(): void {
    this.isOpen.update((open) => !open);
  }

  addMessage(message: ChatMessage): void {
    this.messages.update((list) => [...list, message]);
  }

  setThinking(value: boolean): void {
    this.isThinking.set(value);
  }

  clear(): void {
    this.messages.set([]);
  }
}

import { ChatStateService } from './chat-state.service';
import type { ChatMessage } from '@yumney/shared/chat-api';

describe('ChatStateService', () => {
  let service: ChatStateService;

  beforeEach(() => {
    service = new ChatStateService();
  });

  describe('isOpen state', () => {
    it('should default to closed', () => {
      expect(service.isOpen()).toBe(false);
    });

    it('should open the chat', () => {
      service.open();
      expect(service.isOpen()).toBe(true);
    });

    it('should close the chat', () => {
      service.open();
      service.close();
      expect(service.isOpen()).toBe(false);
    });

    it('should toggle the chat', () => {
      service.toggle();
      expect(service.isOpen()).toBe(true);
      service.toggle();
      expect(service.isOpen()).toBe(false);
    });
  });

  describe('messages', () => {
    it('should default to empty messages', () => {
      expect(service.messages()).toEqual([]);
    });

    it('should add a message', () => {
      const msg: ChatMessage = { role: 'user', content: 'Hello' };
      service.addMessage(msg);
      expect(service.messages()).toEqual([msg]);
    });

    it('should preserve message order when adding multiple', () => {
      service.addMessage({ role: 'user', content: 'A' });
      service.addMessage({ role: 'assistant', content: 'B' });
      service.addMessage({ role: 'user', content: 'C' });
      expect(service.messages().map((m) => m.content)).toEqual(['A', 'B', 'C']);
    });

    it('should clear messages', () => {
      service.addMessage({ role: 'user', content: 'Hello' });
      service.clear();
      expect(service.messages()).toEqual([]);
    });
  });

  describe('thinking indicator', () => {
    it('should default to not thinking', () => {
      expect(service.isThinking()).toBe(false);
    });

    it('should set thinking state', () => {
      service.setThinking(true);
      expect(service.isThinking()).toBe(true);
      service.setThinking(false);
      expect(service.isThinking()).toBe(false);
    });
  });
});

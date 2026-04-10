import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { ChatHintService } from './chat-hint.service';

describe('ChatHintService', () => {
  let service: ChatHintService;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([])],
    });
    service = TestBed.inject(ChatHintService);
    router = TestBed.inject(Router);
  });

  it('should return default hint for unknown routes', () => {
    expect(service.hintKey()).toBe('commandBar.hints.default');
  });

  it('should return undefined page context for unknown routes', () => {
    expect(service.pageContext()).toBeUndefined();
  });
});

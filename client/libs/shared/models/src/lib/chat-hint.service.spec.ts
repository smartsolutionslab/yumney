import { TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { Router, provideRouter } from '@angular/router';
import { ChatHintService } from './chat-hint.service';

@Component({ template: '' })
class DummyComponent {}

describe('ChatHintService', () => {
  let service: ChatHintService;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'dashboard', component: DummyComponent },
          { path: 'recipes', component: DummyComponent },
          { path: 'shopping', component: DummyComponent },
          { path: 'meal-planner', component: DummyComponent },
          { path: 'account', component: DummyComponent },
        ]),
      ],
    });
    service = TestBed.inject(ChatHintService);
    router = TestBed.inject(Router);
  });

  it('should return default hint for root route', () => {
    expect(service.hintKey()).toBe('commandBar.hints.default');
  });

  it('should return undefined page context for root route', () => {
    expect(service.pageContext()).toBeUndefined();
  });

  it('should return dashboard hint after navigating to /dashboard', async () => {
    await router.navigateByUrl('/dashboard');
    expect(service.hintKey()).toBe('commandBar.hints.dashboard');
    expect(service.pageContext()).toBe('dashboard');
  });

  it('should return recipes hint after navigating to /recipes', async () => {
    await router.navigateByUrl('/recipes');
    expect(service.hintKey()).toBe('commandBar.hints.recipes');
    expect(service.pageContext()).toBe('recipes');
  });

  it('should return shopping hint after navigating to /shopping', async () => {
    await router.navigateByUrl('/shopping');
    expect(service.hintKey()).toBe('commandBar.hints.shopping');
    expect(service.pageContext()).toBe('shopping-list');
  });

  it('should return meal planner hint after navigating to /meal-planner', async () => {
    await router.navigateByUrl('/meal-planner');
    expect(service.hintKey()).toBe('commandBar.hints.mealPlanner');
    expect(service.pageContext()).toBe('meal-planner');
  });

  it('should return account hint after navigating to /account', async () => {
    await router.navigateByUrl('/account');
    expect(service.hintKey()).toBe('commandBar.hints.account');
    expect(service.pageContext()).toBe('account');
  });

  it('should return default hint for unknown routes', async () => {
    await router.navigateByUrl('/some-unknown-page');
    expect(service.hintKey()).toBe('commandBar.hints.default');
    expect(service.pageContext()).toBeUndefined();
  });
});

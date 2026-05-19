import { DestroyRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { DashboardApiService } from '@yumney/shared/api-dashboard';
import { DashboardSuggestionsService } from './dashboard-suggestions.service';

describe('DashboardSuggestionsService', () => {
  function createService(api: Partial<DashboardApiService>): DashboardSuggestionsService {
    TestBed.configureTestingModule({
      providers: [
        DashboardSuggestionsService,
        { provide: DashboardApiService, useValue: api },
        { provide: DestroyRef, useValue: { onDestroy: () => () => undefined } },
      ],
    });
    return TestBed.inject(DashboardSuggestionsService);
  }

  it('populates signals and reports non-empty when suggestions arrive', () => {
    const api = {
      getSuggestions: vi.fn().mockReturnValue(
        of({
          quickActions: ['cook_now'],
          suggestions: [
            {
              recipeIdentifier: 'r1',
              title: 'A',
              imageUrl: null,
              prepTimeMinutes: null,
              reason: '',
            },
          ],
        }),
      ),
      getRecentActivity: vi.fn().mockReturnValue(of([])),
    };
    const service = createService(api);

    let result: { initialDataIsEmpty: boolean } | null = null;
    service.load().subscribe((value) => (result = value));

    expect(result).toEqual({ initialDataIsEmpty: false });
    expect(service.loading()).toBe(false);
    expect(service.quickActions()).toEqual([{ key: 'cook_now', labelKey: 'dashboard.quickActions.cook_now' }]);
    expect(service.suggestions()?.suggestions).toHaveLength(1);
  });

  it('reports empty when both lists are empty', () => {
    const api = {
      getSuggestions: vi.fn().mockReturnValue(of({ quickActions: [], suggestions: [] })),
      getRecentActivity: vi.fn().mockReturnValue(of([])),
    };
    const service = createService(api);

    let result: { initialDataIsEmpty: boolean } | null = null;
    service.load().subscribe((value) => (result = value));

    expect(result).toEqual({ initialDataIsEmpty: true });
  });

  it('treats suggestions error as empty and clears loading', () => {
    const api = {
      getSuggestions: vi.fn().mockReturnValue(throwError(() => new Error('boom'))),
      getRecentActivity: vi.fn().mockReturnValue(of([])),
    };
    const service = createService(api);

    let result: { initialDataIsEmpty: boolean } | null = null;
    service.load().subscribe((value) => (result = value));

    expect(result).toEqual({ initialDataIsEmpty: true });
    expect(service.loading()).toBe(false);
  });

  it('falls back to empty list when activity request fails', () => {
    const api = {
      getSuggestions: vi.fn().mockReturnValue(of({ quickActions: [], suggestions: [] })),
      getRecentActivity: vi.fn().mockReturnValue(throwError(() => new Error('boom'))),
    };
    const service = createService(api);

    service.load().subscribe();

    expect(service.recentActivity()).toEqual([]);
  });
});

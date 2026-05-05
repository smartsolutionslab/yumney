import { provideYumneyIcons } from '../icons/provide-icons';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';
import {
  ActivityTimelineComponent,
  type ActivityEntry,
  relativeTimeFromNow,
} from './activity-timeline.component';
import { setupTranslocoTesting } from '@yumney/shared/models';

const en = {
  shared: {
    activity: {
      empty: 'No activity yet',
      deletedRecipe: 'Deleted recipe',
      type: {
        recipe_imported: 'Imported',
        recipe_viewed: 'Viewed',
        recipe_cooked: 'Cooked',
        recipe_edited: 'Edited',
        recipe_deleted: 'Deleted',
        shopping_list_created: 'Created list',
      },
      relative: {
        justNow: 'Just now',
        minutes: '{{value}} min ago',
        hours: '{{value}} h ago',
        days: '{{value}} d ago',
        weeks: '{{value}} w ago',
      },
    },
  },
};

@Component({
  template: `<yn-activity-timeline [entries]="entries()" />`,
  imports: [ActivityTimelineComponent],
})
class TestHostComponent {
  entries = signal<ActivityEntry[]>([]);
}

describe('ActivityTimelineComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let host: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [provideYumneyIcons()],
      imports: [TestHostComponent, setupTranslocoTesting(en)],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render the empty state when no entries are provided', () => {
    const empty = fixture.nativeElement.querySelector('.activity-timeline__empty');
    expect(empty.textContent.trim()).toBe('No activity yet');
  });

  it('should render one item per entry with the recipe title', () => {
    host.entries.set([
      {
        type: 'recipe_cooked',
        recipeIdentifier: 'r1',
        recipeTitle: 'Pasta',
        occurredAt: new Date().toISOString(),
      },
      {
        type: 'recipe_imported',
        recipeIdentifier: 'r2',
        recipeTitle: 'Pizza',
        occurredAt: new Date().toISOString(),
      },
    ]);
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.activity-timeline__item');
    expect(items).toHaveLength(2);
    expect(items[0].textContent).toContain('Pasta');
    expect(items[1].textContent).toContain('Pizza');
  });

  it('should fall back to "Deleted recipe" label when title is null and type is recipe_deleted', () => {
    host.entries.set([
      {
        type: 'recipe_deleted',
        recipeIdentifier: 'r1',
        recipeTitle: null,
        occurredAt: new Date().toISOString(),
      },
    ]);
    fixture.detectChanges();

    const item = fixture.nativeElement.querySelector('.activity-timeline__item');
    expect(item.textContent).toContain('Deleted recipe');
  });
});

describe('relativeTimeFromNow', () => {
  const now = new Date('2026-05-05T12:00:00Z');

  it('returns justNow for under a minute', () => {
    const when = new Date('2026-05-05T11:59:30Z');
    expect(relativeTimeFromNow(when, now)).toEqual({
      key: 'shared.activity.relative.justNow',
      value: 0,
    });
  });

  it('returns minutes for under an hour', () => {
    const when = new Date('2026-05-05T11:45:00Z');
    expect(relativeTimeFromNow(when, now)).toEqual({
      key: 'shared.activity.relative.minutes',
      value: 15,
    });
  });

  it('returns hours for under a day', () => {
    const when = new Date('2026-05-05T08:00:00Z');
    expect(relativeTimeFromNow(when, now)).toEqual({
      key: 'shared.activity.relative.hours',
      value: 4,
    });
  });

  it('returns days for under a week', () => {
    const when = new Date('2026-05-02T12:00:00Z');
    expect(relativeTimeFromNow(when, now)).toEqual({
      key: 'shared.activity.relative.days',
      value: 3,
    });
  });

  it('returns weeks beyond a week', () => {
    const when = new Date('2026-04-15T12:00:00Z');
    expect(relativeTimeFromNow(when, now)).toEqual({
      key: 'shared.activity.relative.weeks',
      value: 2,
    });
  });
});

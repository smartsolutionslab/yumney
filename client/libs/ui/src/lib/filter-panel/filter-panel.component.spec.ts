import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';
import { FilterPanelComponent, type RecipeFilterValue, EMPTY_FILTER } from './filter-panel.component';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { provideYumneyIcons } from '../icons/provide-icons';

const en = {
  recipes: {
    list: {
      filter: {
        title: 'Filters',
        clear: 'Clear all',
        tags: 'Tags',
        favorites: 'Favorites',
        favoritesOnly: 'Favorites only',
        difficulty: 'Difficulty',
        difficultyOption: {
          easy: 'Easy',
          medium: 'Medium',
          hard: 'Hard',
        },
        maxPrepTime: 'Max prep time',
        maxCookTime: 'Max cook time',
        minutesPlaceholder: 'min',
      },
    },
  },
};

@Component({
  template: ` <yn-filter-panel [value]="value()" [availableTags]="availableTags()" (valueChange)="onValueChange($event)" /> `,
  imports: [FilterPanelComponent],
})
class TestHostComponent {
  value = signal<RecipeFilterValue>({ ...EMPTY_FILTER });
  availableTags = signal<string[]>([]);
  lastEmitted: RecipeFilterValue | null = null;

  onValueChange(value: RecipeFilterValue): void {
    this.lastEmitted = value;
  }
}

describe('FilterPanelComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let host: TestHostComponent;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TestHostComponent, setupTranslocoTesting(en)],
      providers: [provideYumneyIcons()],
    });

    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
  });

  it('should create the component', () => {
    fixture.detectChanges();

    const panel = fixture.nativeElement.querySelector('.filter-panel');
    expect(panel).toBeTruthy();
  });

  it('should show tag chips when availableTags provided', () => {
    host.availableTags.set(['Italian', 'Quick', 'Vegan']);
    fixture.detectChanges();

    const chips = fixture.nativeElement.querySelectorAll('.chip-row .filter-chip');
    expect(chips.length).toBeGreaterThanOrEqual(3);
  });

  it('should emit tag toggle when chip clicked', () => {
    host.availableTags.set(['Italian', 'Quick']);
    fixture.detectChanges();

    const tagChips = fixture.nativeElement.querySelectorAll('.chip-row .filter-chip');
    const italianChip = Array.from(tagChips).find((chip) => (chip as HTMLElement).textContent?.trim() === 'Italian') as HTMLElement;
    italianChip.click();

    expect(host.lastEmitted?.tags).toContain('Italian');
  });

  it('should show difficulty chips', () => {
    fixture.detectChanges();

    const allChips = fixture.nativeElement.querySelectorAll('.filter-chip');
    const difficultyTexts = Array.from(allChips).map((c) => (c as HTMLElement).textContent?.trim());
    expect(difficultyTexts).toContain('Easy');
  });

  it('should emit difficulty toggle when chip clicked', () => {
    fixture.detectChanges();

    const allChips = fixture.nativeElement.querySelectorAll('.filter-chip');
    const easyChip = Array.from(allChips).find((chip) => (chip as HTMLElement).textContent?.trim() === 'Easy') as HTMLElement;
    easyChip.click();

    expect(host.lastEmitted?.difficulty).toBe('easy');
  });

  it('should deselect difficulty when same chip clicked again', () => {
    host.value.set({ ...EMPTY_FILTER, difficulty: 'easy' });
    fixture.detectChanges();

    const allChips = fixture.nativeElement.querySelectorAll('.filter-chip');
    const easyChip = Array.from(allChips).find((chip) => (chip as HTMLElement).textContent?.trim() === 'Easy') as HTMLElement;
    easyChip.click();

    expect(host.lastEmitted?.difficulty).toBeNull();
  });

  it('should emit favoritesOnly toggle', () => {
    fixture.detectChanges();

    const allChips = fixture.nativeElement.querySelectorAll('.filter-chip');
    const favChip = Array.from(allChips).find((chip) => (chip as HTMLElement).textContent?.trim() === 'Favorites only') as HTMLElement;
    favChip.click();

    expect(host.lastEmitted?.favoritesOnly).toBe(true);
  });

  it('should show active count badge for each active filter', () => {
    host.value.set({ ...EMPTY_FILTER, tags: ['Italian'], difficulty: 'easy', favoritesOnly: true });
    fixture.detectChanges();

    const count = fixture.nativeElement.querySelector('.filter-count');
    expect(count.textContent).toContain('3');
  });

  it('should emit clear all', () => {
    host.value.set({ ...EMPTY_FILTER, tags: ['Italian'], difficulty: 'easy', favoritesOnly: true });
    fixture.detectChanges();

    const clearButton = fixture.nativeElement.querySelector('.clear-button');
    clearButton.click();

    expect(host.lastEmitted).toEqual(EMPTY_FILTER);
  });

  it('should show prep time and cook time inputs', () => {
    fixture.detectChanges();

    const prepInput = fixture.nativeElement.querySelector('#filter-max-prep');
    const cookInput = fixture.nativeElement.querySelector('#filter-max-cook');
    expect(prepInput).toBeTruthy();
    expect(cookInput).toBeTruthy();
  });
});

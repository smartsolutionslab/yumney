import { provideYumneyIcons } from '../icons/provide-icons';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';
import { StarRatingComponent } from './star-rating.component';
import { setupTranslocoTesting } from '@yumney/shared/models';

const en = {
  shared: {
    rating: {
      label: 'Rating',
      value: 'Rate {{value}} of 5',
    },
  },
};

@Component({
  template: `
    <yn-star-rating [rating]="rating()" [readonly]="readonly()" (ratingChange)="onChange($event)" />
  `,
  imports: [StarRatingComponent],
})
class TestHostComponent {
  rating = signal<number | null>(null);
  readonly = signal(false);
  onChange = vi.fn();
}

describe('StarRatingComponent', () => {
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

  function getStars(): HTMLButtonElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('.star-rating__star'));
  }

  it('should render five stars', () => {
    expect(getStars()).toHaveLength(5);
  });

  it('should fill exactly the rated number of stars', () => {
    host.rating.set(3);
    fixture.detectChanges();

    const filled = getStars().filter((star) =>
      star.classList.contains('star-rating__star--filled'),
    );
    expect(filled).toHaveLength(3);
  });

  it('should emit ratingChange when a star is clicked', () => {
    getStars()[3].click();
    expect(host.onChange).toHaveBeenCalledWith(4);
  });

  it('should preview hover by filling stars up to the hovered value', () => {
    host.rating.set(2);
    fixture.detectChanges();

    getStars()[4].dispatchEvent(new MouseEvent('mouseenter'));
    fixture.detectChanges();

    const filled = getStars().filter((star) =>
      star.classList.contains('star-rating__star--filled'),
    );
    expect(filled).toHaveLength(5);
  });

  it('should not emit when in readonly mode', () => {
    host.readonly.set(true);
    fixture.detectChanges();

    getStars()[2].click();

    expect(host.onChange).not.toHaveBeenCalled();
  });

  it('should disable the buttons in readonly mode', () => {
    host.readonly.set(true);
    fixture.detectChanges();

    expect(getStars().every((star) => star.disabled)).toBe(true);
  });
});

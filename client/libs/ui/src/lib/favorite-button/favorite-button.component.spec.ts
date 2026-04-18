import { provideYumneyIcons } from '../icons/provide-icons';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';
import { FavoriteButtonComponent } from './favorite-button.component';
import { setupTranslocoTesting } from '@yumney/shared/models';

const en = {
  recipes: {
    favorite: {
      addAriaLabel: 'Add to favorites',
      removeAriaLabel: 'Remove from favorites',
    },
  },
};

@Component({
  template: ` <yn-favorite-button [isFavorite]="isFavorite()" (toggled)="onToggled()" /> `,
  imports: [FavoriteButtonComponent],
})
class TestHostComponent {
  isFavorite = signal(false);
  onToggled = vi.fn();
}

describe('FavoriteButtonComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let host: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent, setupTranslocoTesting(en)],
      providers: [provideYumneyIcons()],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render the favorite button', () => {
    const button = fixture.nativeElement.querySelector('button.favorite-button');
    expect(button).toBeTruthy();
  });

  it('should set aria-pressed to false when not favorite', () => {
    const button = fixture.nativeElement.querySelector('button.favorite-button');
    expect(button.getAttribute('aria-pressed')).toBe('false');
  });

  it('should set aria-pressed to true when favorite', () => {
    host.isFavorite.set(true);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button.favorite-button');
    expect(button.getAttribute('aria-pressed')).toBe('true');
  });

  it('should show add aria-label when not favorite', () => {
    const button = fixture.nativeElement.querySelector('button.favorite-button');
    expect(button.getAttribute('aria-label')).toBe('Add to favorites');
  });

  it('should show remove aria-label when favorite', () => {
    host.isFavorite.set(true);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button.favorite-button');
    expect(button.getAttribute('aria-label')).toBe('Remove from favorites');
  });

  it('should emit toggled when clicked', () => {
    const button = fixture.nativeElement.querySelector('button.favorite-button');
    button.click();

    expect(host.onToggled).toHaveBeenCalled();
  });

  it('should stop event propagation on click', () => {
    const button = fixture.nativeElement.querySelector('button.favorite-button');
    const event = new MouseEvent('click', { bubbles: true });
    const stopSpy = vi.spyOn(event, 'stopPropagation');

    button.dispatchEvent(event);

    expect(stopSpy).toHaveBeenCalled();
  });

  it('should apply is-favorite class when favorite', () => {
    host.isFavorite.set(true);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button.favorite-button');
    expect(button.classList.contains('is-favorite')).toBe(true);
  });

  it('should not apply is-favorite class when not favorite', () => {
    const button = fixture.nativeElement.querySelector('button.favorite-button');
    expect(button.classList.contains('is-favorite')).toBe(false);
  });
});

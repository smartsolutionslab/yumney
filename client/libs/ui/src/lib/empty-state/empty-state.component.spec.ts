import { ComponentFixture, TestBed } from '@angular/core/testing';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { EmptyStateComponent } from './empty-state.component';

const en = {
  list: {
    empty: {
      title: 'Nothing here yet',
      message: 'Add your first recipe to get started.',
    },
    search: {
      noResults: 'No matches for "{{ query }}"',
    },
  },
};

describe('EmptyStateComponent', () => {
  let fixture: ComponentFixture<EmptyStateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmptyStateComponent, setupTranslocoTesting(en)],
    }).compileComponents();

    fixture = TestBed.createComponent(EmptyStateComponent);
  });

  // ── Variant class ──────────────────────────────────────────────────────────

  it('should default to the card variant', () => {
    fixture.componentRef.setInput('message', 'list.empty.message');

    fixture.detectChanges();

    const container = fixture.nativeElement.querySelector('.empty-state');
    expect(container.classList.contains('empty-state--card')).toBe(true);
  });

  it('should apply the minimal variant class when requested', () => {
    fixture.componentRef.setInput('variant', 'minimal');
    fixture.componentRef.setInput('message', 'list.empty.message');

    fixture.detectChanges();

    const container = fixture.nativeElement.querySelector('.empty-state');
    expect(container.classList.contains('empty-state--minimal')).toBe(true);
  });

  // ── Title rendering ────────────────────────────────────────────────────────

  it('should not render an h2 when title is omitted', () => {
    fixture.componentRef.setInput('message', 'list.empty.message');

    fixture.detectChanges();

    const heading = fixture.nativeElement.querySelector('h2');
    expect(heading).toBeFalsy();
  });

  it('should translate the title input as an h2', () => {
    fixture.componentRef.setInput('title', 'list.empty.title');
    fixture.componentRef.setInput('message', 'list.empty.message');

    fixture.detectChanges();

    const heading = fixture.nativeElement.querySelector('h2');
    expect(heading.textContent.trim()).toBe('Nothing here yet');
  });

  // ── Message rendering ─────────────────────────────────────────────────────

  it('should translate the message as a paragraph', () => {
    fixture.componentRef.setInput('message', 'list.empty.message');

    fixture.detectChanges();

    const message = fixture.nativeElement.querySelector('p');
    expect(message.textContent.trim()).toBe('Add your first recipe to get started.');
  });

  it('should interpolate messageParams', () => {
    fixture.componentRef.setInput('message', 'list.search.noResults');
    fixture.componentRef.setInput('messageParams', { query: 'pasta' });

    fixture.detectChanges();

    const message = fixture.nativeElement.querySelector('p');
    expect(message.textContent.trim()).toBe('No matches for "pasta"');
  });

  // ── Test id passthrough ────────────────────────────────────────────────────

  it('should set data-testid when testId input is provided', () => {
    fixture.componentRef.setInput('message', 'list.empty.message');
    fixture.componentRef.setInput('testId', 'recipes-empty');

    fixture.detectChanges();

    const container = fixture.nativeElement.querySelector('.empty-state');
    expect(container.getAttribute('data-testid')).toBe('recipes-empty');
  });

  it('should omit data-testid when testId input is not provided', () => {
    fixture.componentRef.setInput('message', 'list.empty.message');

    fixture.detectChanges();

    const container = fixture.nativeElement.querySelector('.empty-state');
    expect(container.getAttribute('data-testid')).toBeNull();
  });
});

import { provideYumneyIcons } from '../icons/provide-icons';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';
import { CategorySectionComponent, CategoryKey } from './category-section.component';
import { setupTranslocoTesting } from '@yumney/shared/models';

const en = {
  shopping: {
    category: {
      produce: 'Produce',
      dairy: 'Dairy',
      'meat-fish': 'Meat & Fish',
      bakery: 'Bakery',
      frozen: 'Frozen',
      beverages: 'Beverages',
      pantry: 'Pantry',
      spices: 'Spices',
      household: 'Household',
      other: 'Other',
    },
  },
};

@Component({
  template: `
    <yn-category-section [category]="category()" [itemCount]="itemCount()" (toggle)="onToggle()">
      <span class="projected">Projected</span>
    </yn-category-section>
  `,
  imports: [CategorySectionComponent],
})
class TestHostComponent {
  category = signal<CategoryKey>('produce');
  itemCount = signal(3);
  onToggle = vi.fn();
}

describe('CategorySectionComponent', () => {
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

  it('should render the count badge', () => {
    const badge = fixture.nativeElement.querySelector('.category-count');
    expect(badge.textContent.trim()).toBe('3');
  });

  it('should project body content while expanded', () => {
    const projected = fixture.nativeElement.querySelector('.projected');
    expect(projected.textContent).toBe('Projected');
  });

  it('should collapse the body when the header is clicked', () => {
    fixture.nativeElement.querySelector('.category-header').click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.projected')).toBeNull();
    expect(fixture.nativeElement.querySelector('.category-body')).toBeNull();
  });

  it('should reflect the open state via aria-expanded', () => {
    const header = fixture.nativeElement.querySelector('.category-header');
    expect(header.getAttribute('aria-expanded')).toBe('true');

    header.click();
    fixture.detectChanges();

    expect(header.getAttribute('aria-expanded')).toBe('false');
  });

  it('should emit toggle when the header is clicked', () => {
    fixture.nativeElement.querySelector('.category-header').click();
    expect(host.onToggle).toHaveBeenCalled();
  });

  it('should localize the label from the category key', () => {
    host.category.set('spices');
    fixture.detectChanges();
    const label = fixture.nativeElement.querySelector('.category-label');
    expect(label.textContent.trim()).toBe('Spices');
  });
});

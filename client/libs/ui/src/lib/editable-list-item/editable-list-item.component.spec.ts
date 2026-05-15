import { provideYumneyIcons } from '../icons/provide-icons';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';
import { EditableListItemComponent } from './editable-list-item.component';
import { setupTranslocoTesting } from '@yumney/shared/models';

const en = {
  shared: {
    editableList: {
      moveUp: 'Move up',
      moveDown: 'Move down',
      remove: 'Remove',
    },
  },
};

@Component({
  template: `
    <yn-editable-list-item [index]="index()" [total]="total()" (moveUp)="onMoveUp()" (moveDown)="onMoveDown()" (remove)="onRemove()">
      <span class="projected">Projected Content</span>
    </yn-editable-list-item>
  `,
  imports: [EditableListItemComponent],
})
class TestHostComponent {
  index = signal(1);
  total = signal(3);
  onMoveUp = vi.fn();
  onMoveDown = vi.fn();
  onRemove = vi.fn();
}

describe('EditableListItemComponent', () => {
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

  it('should create the component', () => {
    const component = fixture.debugElement.children[0].componentInstance;
    expect(component).toBeTruthy();
  });

  it('should project content', () => {
    const projected = fixture.nativeElement.querySelector('.projected');
    expect(projected.textContent).toBe('Projected Content');
  });

  it('should emit moveUp when up button is clicked', () => {
    const upBtn = fixture.nativeElement.querySelector('[aria-label="Move up"]');
    upBtn.click();

    expect(host.onMoveUp).toHaveBeenCalled();
  });

  it('should emit moveDown when down button is clicked', () => {
    const downBtn = fixture.nativeElement.querySelector('[aria-label="Move down"]');
    downBtn.click();

    expect(host.onMoveDown).toHaveBeenCalled();
  });

  it('should emit remove when remove button is clicked', () => {
    const removeBtn = fixture.nativeElement.querySelector('[aria-label="Remove"]');
    removeBtn.click();

    expect(host.onRemove).toHaveBeenCalled();
  });

  it('should disable up button when index is 0', () => {
    host.index.set(0);
    fixture.detectChanges();

    const upBtn = fixture.nativeElement.querySelector('[aria-label="Move up"]');
    expect(upBtn.disabled).toBe(true);
  });

  it('should disable down button when index is last', () => {
    host.index.set(2);
    fixture.detectChanges();

    const downBtn = fixture.nativeElement.querySelector('[aria-label="Move down"]');
    expect(downBtn.disabled).toBe(true);
  });

  it('should enable both buttons for middle items', () => {
    const upBtn = fixture.nativeElement.querySelector('[aria-label="Move up"]');
    const downBtn = fixture.nativeElement.querySelector('[aria-label="Move down"]');
    expect(upBtn.disabled).toBe(false);
    expect(downBtn.disabled).toBe(false);
  });

  it('should never disable the remove button', () => {
    const removeBtn = fixture.nativeElement.querySelector('[aria-label="Remove"]');
    expect(removeBtn.disabled).toBe(false);
  });
});

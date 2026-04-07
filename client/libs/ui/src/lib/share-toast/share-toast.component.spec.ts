import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ShareToastComponent } from './share-toast.component';
import { setupTranslocoTesting } from '@yumney/shared/models';

const en = {
  dashboard: {
    share: {
      toastTitle: 'Recipe shared',
      dismiss: 'Dismiss',
    },
  },
};

describe('ShareToastComponent', () => {
  let fixture: ComponentFixture<ShareToastComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ShareToastComponent,
        setupTranslocoTesting(en),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ShareToastComponent);
    fixture.componentRef.setInput('url', 'https://example.com/recipe');
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should display the URL', () => {
    const urlEl = fixture.nativeElement.querySelector('.share-toast-url');
    expect(urlEl.textContent).toContain('https://example.com/recipe');
  });

  it('should emit dismissed when dismiss button clicked', () => {
    const spy = vi.fn();
    fixture.componentInstance.dismissed.subscribe(spy);

    const dismissBtn = fixture.nativeElement.querySelector('.share-toast-dismiss');
    dismissBtn.click();

    expect(spy).toHaveBeenCalled();
  });
});

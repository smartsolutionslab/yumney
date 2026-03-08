import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { HttpErrorResponse } from '@angular/common/http';
import { of, Subject, throwError } from 'rxjs';
import { DashboardComponent } from './dashboard.component';
import { RecipeApiService } from '@yumney/shared/api-client';

const en = {
  dashboard: {
    title: 'Dashboard',
    welcome: 'Welcome to Yumney!',
    import: {
      title: 'Import a Recipe',
      subtitle: 'Paste a URL from any recipe website',
      placeholder: 'https://example.com/recipe/...',
      submit: 'Import Recipe',
      submitting: 'Importing...',
      errors: {
        urlRequired: 'Please enter a URL.',
        urlInvalid: 'Please enter a valid HTTP or HTTPS URL.',
        generic: 'An unexpected error occurred. Please try again later.',
      },
    },
  },
};

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let recipeApiMock: { importRecipe: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    recipeApiMock = { importRecipe: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [
        DashboardComponent,
        TranslocoTestingModule.forRoot({
          langs: { en },
          translocoConfig: {
            availableLangs: ['en'],
            defaultLang: 'en',
          },
        }),
      ],
      providers: [{ provide: RecipeApiService, useValue: recipeApiMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should render the welcome message', () => {
    const element = fixture.nativeElement;
    expect(element.textContent).toContain('Welcome to Yumney!');
  });

  it('should render the dashboard title', () => {
    const heading = fixture.nativeElement.querySelector('h1');
    expect(heading.textContent).toContain('Dashboard');
  });

  it('should render the URL import input', () => {
    const input = fixture.nativeElement.querySelector('input#url');
    expect(input).toBeTruthy();
  });

  it('should show validation error when submitting empty URL', () => {
    component.onImport();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.field-error');
    expect(error.textContent).toContain('Please enter a URL.');
  });

  it('should show validation error for invalid URL format', () => {
    component.form.controls.url.setValue('not-a-url');
    component.onImport();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.field-error');
    expect(error.textContent).toContain('Please enter a valid HTTP or HTTPS URL.');
  });

  it('should accept valid HTTP URL', () => {
    component.form.controls.url.setValue('http://example.com/recipe');

    expect(component.form.valid).toBe(true);
  });

  it('should accept valid HTTPS URL', () => {
    component.form.controls.url.setValue('https://example.com/recipe');

    expect(component.form.valid).toBe(true);
  });

  it('should accept URL with query parameters', () => {
    component.form.controls.url.setValue('https://example.com/recipe?id=123&lang=en');

    expect(component.form.valid).toBe(true);
  });

  it('should call recipeApi.importRecipe on valid submit', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(of({ message: 'OK' }));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(recipeApiMock.importRecipe).toHaveBeenCalledWith({
      url: 'https://example.com/recipe',
    });
  }));

  it('should show loading indicator during import', () => {
    const subject = new Subject();
    recipeApiMock.importRecipe.mockReturnValue(subject);

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(button.textContent).toContain('Importing...');

    subject.next({ message: 'OK' });
    subject.complete();
  });

  it('should show server error on API failure', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();
    fixture.detectChanges();

    const errorBanner = fixture.nativeElement.querySelector('.error-banner');
    expect(errorBanner.textContent).toContain('An unexpected error occurred.');
  }));

  it('should disable submit button while loading', () => {
    component.isLoading.set(true);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(button.disabled).toBe(true);
  });

  it('should not call API when form is invalid', () => {
    component.onImport();

    expect(recipeApiMock.importRecipe).not.toHaveBeenCalled();
  });

  it('should mark fields as touched on invalid submit', () => {
    component.onImport();

    expect(component.form.controls.url.touched).toBe(true);
  });

  it('should reset form after successful import', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(of({ message: 'OK' }));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.form.controls.url.value).toBe('');
  }));

  it('should clear server error on new submission', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();
    expect(component.serverError()).toBe('dashboard.import.errors.generic');

    recipeApiMock.importRecipe.mockReturnValue(of({ message: 'OK' }));
    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    expect(component.serverError()).toBeNull();
  }));

  it('should show url invalid error on 422 response', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 422 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.serverError()).toBe('dashboard.import.errors.urlInvalid');
  }));

  it('should reject ftp URL', () => {
    component.form.controls.url.setValue('ftp://example.com/file');

    expect(component.form.valid).toBe(false);
  });

  it('should return false from hasError when control is not touched', () => {
    component.form.controls.url.setValue('');
    expect(component.hasError('url', 'required')).toBe(false);
  });

  it('should return true from hasError when control is touched and has error', () => {
    component.form.controls.url.setValue('');
    component.form.controls.url.markAsTouched();
    expect(component.hasError('url', 'required')).toBe(true);
  });

  it('should set isLoading to false after error', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.isLoading()).toBe(false);
  }));
});

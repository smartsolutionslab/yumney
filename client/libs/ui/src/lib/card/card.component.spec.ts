import { ComponentFixture, TestBed } from '@angular/core/testing';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { CardComponent } from './card.component';

const en = {
  auth: {
    register: {
      title: 'Create your account',
      subtitle: 'Sign up in 30 seconds',
    },
  },
};

describe('CardComponent', () => {
  let fixture: ComponentFixture<CardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CardComponent, setupTranslocoTesting(en)],
    }).compileComponents();

    fixture = TestBed.createComponent(CardComponent);
  });

  // ── Variant class ──────────────────────────────────────────────────────────

  it('should apply the auth-card class for the auth variant', () => {
    fixture.detectChanges();

    const card = fixture.nativeElement.querySelector('div');
    expect(card.classList.contains('auth-card')).toBe(true);
  });

  // ── Title / subtitle rendering ────────────────────────────────────────────

  it('should not render an h1 when title is omitted', () => {
    fixture.detectChanges();

    const heading = fixture.nativeElement.querySelector('h1');
    expect(heading).toBeFalsy();
  });

  it('should not render a subtitle when subtitle is omitted', () => {
    fixture.detectChanges();

    const subtitle = fixture.nativeElement.querySelector('.subtitle');
    expect(subtitle).toBeFalsy();
  });

  it('should translate the title input as an h1', () => {
    fixture.componentRef.setInput('title', 'auth.register.title');

    fixture.detectChanges();

    const heading = fixture.nativeElement.querySelector('h1');
    expect(heading.textContent.trim()).toBe('Create your account');
  });

  it('should translate the subtitle input as a .subtitle paragraph', () => {
    fixture.componentRef.setInput('title', 'auth.register.title');
    fixture.componentRef.setInput('subtitle', 'auth.register.subtitle');

    fixture.detectChanges();

    const subtitle = fixture.nativeElement.querySelector('.subtitle');
    expect(subtitle.textContent.trim()).toBe('Sign up in 30 seconds');
  });
});

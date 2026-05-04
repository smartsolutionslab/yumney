import { provideYumneyIcons } from '../icons/provide-icons';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { SettingsCardComponent } from './settings-card.component';
import { setupTranslocoTesting } from '@yumney/shared/models';

const en = {
  test: {
    title: 'Identity',
    desc: 'Who you are',
  },
};

@Component({
  template: `
    <yn-settings-card titleKey="test.title" descriptionKey="test.desc">
      <span class="projected">body</span>
    </yn-settings-card>
  `,
  imports: [SettingsCardComponent],
})
class TestHostComponent {}

describe('SettingsCardComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [provideYumneyIcons()],
      imports: [TestHostComponent, setupTranslocoTesting(en)],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
  });

  it('should render title and description from i18n keys', () => {
    expect(fixture.nativeElement.querySelector('.settings-card__title').textContent.trim()).toBe(
      'Identity',
    );
    expect(
      fixture.nativeElement.querySelector('.settings-card__description').textContent.trim(),
    ).toBe('Who you are');
  });

  it('should project body content while open', () => {
    expect(fixture.nativeElement.querySelector('.projected').textContent).toBe('body');
  });

  it('should toggle the body when the header is clicked', () => {
    fixture.nativeElement.querySelector('.settings-card__header').click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.projected')).toBeNull();
    expect(fixture.nativeElement.querySelector('.settings-card__header').getAttribute('aria-expanded')).toBe('false');
  });
});

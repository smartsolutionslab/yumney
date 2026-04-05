import { Meta, StoryObj } from '@storybook/angular';
import { Component } from '@angular/core';

@Component({
  selector: 'yn-spacing-docs',
  standalone: true,
  template: `
    <div class="spacing-docs">
      <section>
        <h2>Spacing Scale</h2>
        <div class="spacing-samples">
          @for (step of spacingScale; track step.token) {
            <div class="spacing-row">
              <code>{{ step.token }}</code>
              <div class="spacing-bar-container">
                <div class="spacing-bar" [style.width]="'var(' + step.token + ')'"></div>
              </div>
              <span class="spacing-value">{{ step.value }}</span>
            </div>
          }
        </div>
      </section>

      <section>
        <h2>Semantic Spacing</h2>
        <div class="spacing-samples">
          @for (step of semanticSpacing; track step.token) {
            <div class="spacing-row">
              <code>{{ step.token }}</code>
              <div class="spacing-bar-container">
                <div class="spacing-bar semantic" [style.width]="'var(' + step.token + ')'"></div>
              </div>
              <span class="spacing-value">{{ step.value }}</span>
            </div>
          }
        </div>
      </section>
    </div>
  `,
  styles: [`
    .spacing-docs { max-width: 700px; }
    section { margin-bottom: 2.5rem; }
    h2 { font-size: 1.25rem; font-weight: 600; margin-bottom: 1rem; color: var(--yn-text); }
    .spacing-samples { display: flex; flex-direction: column; gap: 0.5rem; }
    .spacing-row { display: flex; align-items: center; gap: 1rem; }
    code { font-size: 0.75rem; font-family: var(--yn-font-mono); color: var(--yn-text-muted); min-width: 140px; }
    .spacing-bar-container { flex: 1; }
    .spacing-bar { height: 24px; background: var(--yn-primary-light); border: 1px solid var(--yn-primary);
      border-radius: 4px; min-width: 2px; }
    .spacing-bar.semantic { background: #e0f2fe; border-color: #0284c7; }
    .spacing-value { font-size: 0.75rem; color: var(--yn-text-light); min-width: 80px; text-align: right; }
  `],
})
class SpacingDocsComponent {
  spacingScale = [
    { token: '--yn-space-1', value: '0.25rem (4px)' },
    { token: '--yn-space-2', value: '0.375rem (6px)' },
    { token: '--yn-space-3', value: '0.5rem (8px)' },
    { token: '--yn-space-4', value: '0.75rem (12px)' },
    { token: '--yn-space-5', value: '1rem (16px)' },
    { token: '--yn-space-6', value: '1.25rem (20px)' },
    { token: '--yn-space-7', value: '1.5rem (24px)' },
    { token: '--yn-space-8', value: '2rem (32px)' },
    { token: '--yn-space-9', value: '2.5rem (40px)' },
    { token: '--yn-space-10', value: '3rem (48px)' },
  ];

  semanticSpacing = [
    { token: '--yn-space-xs', value: '4px' },
    { token: '--yn-space-sm', value: '8px' },
    { token: '--yn-space-md', value: '16px' },
    { token: '--yn-space-lg', value: '24px' },
    { token: '--yn-space-xl', value: '32px' },
  ];
}

const meta: Meta<SpacingDocsComponent> = {
  title: 'Foundations/Spacing',
  component: SpacingDocsComponent,
};

export default meta;
type Story = StoryObj<SpacingDocsComponent>;

export const SpacingScale: Story = {};

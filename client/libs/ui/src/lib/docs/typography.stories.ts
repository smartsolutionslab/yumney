import { Meta, StoryObj } from '@storybook/angular';
import { Component } from '@angular/core';

@Component({
  selector: 'yn-typography-docs',
  standalone: true,
  template: `
    <div class="type-docs">
      <section>
        <h2>Type Scale</h2>
        <div class="type-samples">
          @for (step of typeScale; track step.token) {
            <div class="type-row">
              <span class="type-sample" [style.font-size]="'var(' + step.token + ')'"> The quick brown fox jumps </span>
              <div class="type-meta">
                <code>{{ step.token }}</code>
                <span>{{ step.value }}</span>
              </div>
            </div>
          }
        </div>
      </section>

      <section>
        <h2>Font Weights</h2>
        <div class="type-samples">
          @for (weight of weights; track weight.token) {
            <div class="type-row">
              <span class="weight-sample" [style.font-weight]="'var(' + weight.token + ')'"> {{ weight.label }} ({{ weight.value }}) </span>
              <code>{{ weight.token }}</code>
            </div>
          }
        </div>
      </section>

      <section>
        <h2>Line Heights</h2>
        <div class="type-samples">
          @for (lh of lineHeights; track lh.token) {
            <div class="type-row">
              <span class="lh-sample" [style.line-height]="'var(' + lh.token + ')'">
                {{ lh.label }}: Multi-line text sample that wraps to demonstrate line height spacing between lines of text in a paragraph.
              </span>
              <code>{{ lh.token }}: {{ lh.value }}</code>
            </div>
          }
        </div>
      </section>
    </div>
  `,
  styles: [
    `
      .type-docs {
        max-width: 800px;
      }
      section {
        margin-bottom: 2.5rem;
      }
      h2 {
        font-size: 1.25rem;
        font-weight: 600;
        margin-bottom: 1rem;
        color: var(--yn-text);
      }
      .type-samples {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }
      .type-row {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        gap: 1rem;
        padding: 0.75rem 0;
        border-bottom: 1px solid var(--yn-border-light);
      }
      .type-sample {
        color: var(--yn-text);
        flex: 1;
      }
      .type-meta {
        text-align: right;
        flex-shrink: 0;
      }
      code {
        font-size: 0.75rem;
        color: var(--yn-text-muted);
        font-family: var(--yn-font-mono);
        display: block;
      }
      .type-meta span {
        font-size: 0.6875rem;
        color: var(--yn-text-light);
      }
      .weight-sample {
        font-size: 1.125rem;
        color: var(--yn-text);
        flex: 1;
      }
      .lh-sample {
        font-size: 0.875rem;
        color: var(--yn-text);
        max-width: 400px;
      }
    `,
  ],
})
class TypographyDocsComponent {
  typeScale = [
    { token: '--yn-text-5xl', value: '1.75rem (28px)' },
    { token: '--yn-text-4xl', value: '1.5rem (24px)' },
    { token: '--yn-text-3xl', value: '1.375rem (22px)' },
    { token: '--yn-text-2xl', value: '1.25rem (20px)' },
    { token: '--yn-text-xl', value: '1.125rem (18px)' },
    { token: '--yn-text-lg', value: '1rem (16px)' },
    { token: '--yn-text-md', value: '0.9375rem (15px)' },
    { token: '--yn-text-base', value: '0.875rem (14px)' },
    { token: '--yn-text-sm', value: '0.75rem (12px)' },
    { token: '--yn-text-xs', value: '0.6875rem (11px)' },
  ];

  weights = [
    { token: '--yn-font-normal', value: '400', label: 'Normal' },
    { token: '--yn-font-medium', value: '500', label: 'Medium' },
    { token: '--yn-font-semibold', value: '600', label: 'Semibold' },
    { token: '--yn-font-bold', value: '700', label: 'Bold' },
  ];

  lineHeights = [
    { token: '--yn-leading-none', value: '1', label: 'None' },
    { token: '--yn-leading-tight', value: '1.3', label: 'Tight' },
    { token: '--yn-leading-snug', value: '1.4', label: 'Snug' },
    { token: '--yn-leading-normal', value: '1.5', label: 'Normal' },
    { token: '--yn-leading-relaxed', value: '1.6', label: 'Relaxed' },
  ];
}

const meta: Meta<TypographyDocsComponent> = {
  title: 'Foundations/Typography',
  component: TypographyDocsComponent,
};

export default meta;
type Story = StoryObj<TypographyDocsComponent>;

export const TypeScale: Story = {};

import { Meta, StoryObj } from '@storybook/angular';
import { Component } from '@angular/core';

@Component({
  selector: 'yn-color-swatches',
  standalone: true,
  template: `
    <div class="color-docs">
      @for (group of colorGroups; track group.name) {
        <section>
          <h2>{{ group.name }}</h2>
          <div class="swatch-grid">
            @for (color of group.colors; track color.token) {
              <div class="swatch-card">
                <div class="swatch" [style.background]="'var(' + color.token + ')'"></div>
                <div class="swatch-info">
                  <code>{{ color.token }}</code>
                  <span class="swatch-value">{{ color.value }}</span>
                </div>
              </div>
            }
          </div>
        </section>
      }
    </div>

    <section>
      <h2>Contrast Guide</h2>
      <p class="contrast-note">Text colors safe on each surface (WCAG AA 4.5:1 minimum):</p>
      <table class="contrast-table">
        <tr><th>Surface</th><th>Safe text colors</th></tr>
        <tr><td>--yn-surface (#f0f8f0)</td><td>--yn-text, --yn-text-secondary, --yn-text-muted</td></tr>
        <tr><td>--yn-surface-elevated (#fff)</td><td>--yn-text, --yn-text-secondary, --yn-text-muted</td></tr>
        <tr><td>--yn-background (#dcedc8)</td><td>--yn-text, --yn-text-secondary</td></tr>
        <tr><td>--yn-primary (#c2410c)</td><td>--yn-text-inverse (#fff)</td></tr>
        <tr><td>--yn-danger (#dc2626)</td><td>--yn-text-inverse (#fff)</td></tr>
      </table>
    </section>
    </div>
  `,
  styles: [
    `
      .color-docs {
        max-width: 960px;
      }
      section {
        margin-bottom: 2rem;
      }
      h2 {
        font-size: 1.25rem;
        font-weight: 600;
        margin-bottom: 1rem;
        color: var(--yn-text);
      }
      .swatch-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
        gap: 1rem;
      }
      .swatch-card {
        border-radius: 8px;
        overflow: hidden;
        border: 1px solid var(--yn-border-light);
        background: #fff;
      }
      .swatch {
        height: 64px;
      }
      .swatch-info {
        padding: 0.5rem 0.75rem;
      }
      code {
        display: block;
        font-size: 0.75rem;
        color: var(--yn-text);
        font-family: var(--yn-font-mono);
        margin-bottom: 0.25rem;
      }
      .swatch-value {
        font-size: 0.6875rem;
        color: var(--yn-text-muted);
      }
      .contrast-note {
        font-size: 0.875rem;
        color: var(--yn-text-muted);
        margin-bottom: 0.75rem;
      }
      .contrast-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 0.8125rem;
      }
      .contrast-table th,
      .contrast-table td {
        text-align: left;
        padding: 0.5rem 0.75rem;
        border-bottom: 1px solid var(--yn-border-light);
      }
      .contrast-table th {
        font-weight: 600;
        color: var(--yn-text);
      }
      .contrast-table td {
        color: var(--yn-text-muted);
        font-family: var(--yn-font-mono);
        font-size: 0.75rem;
      }
    `,
  ],
})
class ColorSwatchesComponent {
  colorGroups = [
    {
      name: 'Brand',
      colors: [
        { token: '--yn-primary', value: '#c2410c' },
        { token: '--yn-primary-hover', value: '#9a3412' },
        { token: '--yn-primary-light', value: '#fff7ed' },
        { token: '--yn-primary-light-end', value: '#fed7aa' },
      ],
    },
    {
      name: 'Text',
      colors: [
        { token: '--yn-text', value: '#1a2e1a' },
        { token: '--yn-text-secondary', value: '#2d4a2d' },
        { token: '--yn-text-muted', value: '#4a6b4a' },
        { token: '--yn-text-light', value: '#6b8a6b' },
        { token: '--yn-text-placeholder', value: '#8aaa8a' },
        { token: '--yn-text-inverse', value: '#ffffff' },
      ],
    },
    {
      name: 'Accent',
      colors: [
        { token: '--yn-accent', value: '#059669' },
        { token: '--yn-accent-light', value: '#ecfdf5' },
      ],
    },
    {
      name: 'Surface & Background',
      colors: [
        { token: '--yn-surface', value: '#f0f8f0' },
        { token: '--yn-surface-elevated', value: '#ffffff' },
        { token: '--yn-background', value: '#dcedc8' },
        { token: '--yn-background-subtle', value: '#e8f5e9' },
        { token: '--yn-background-hover', value: '#c8e6c9' },
        { token: '--yn-overlay', value: 'rgb(0 0 0 / 50%)' },
      ],
    },
    {
      name: 'Border',
      colors: [
        { token: '--yn-border', value: '#a5d6a7' },
        { token: '--yn-border-light', value: '#e8f5e9' },
        { token: '--yn-border-medium', value: '#c8e6c9' },
        { token: '--yn-border-strong', value: '#81c784' },
      ],
    },
    {
      name: 'Status',
      colors: [
        { token: '--yn-danger', value: '#dc2626' },
        { token: '--yn-danger-light', value: '#fef2f2' },
        { token: '--yn-success', value: '#059669' },
        { token: '--yn-success-light', value: '#ecfdf5' },
        { token: '--yn-warning', value: '#d97706' },
        { token: '--yn-warning-light', value: '#fffbeb' },
        { token: '--yn-info', value: '#0284c7' },
        { token: '--yn-info-light', value: '#f0f9ff' },
      ],
    },
    {
      name: 'Links',
      colors: [
        { token: '--yn-link', value: '#e05a1a' },
        { token: '--yn-link-hover', value: '#c2410c' },
      ],
    },
  ];
}

const meta: Meta<ColorSwatchesComponent> = {
  title: 'Foundations/Colors',
  component: ColorSwatchesComponent,
};

export default meta;
type Story = StoryObj<ColorSwatchesComponent>;

export const AllColors: Story = {};

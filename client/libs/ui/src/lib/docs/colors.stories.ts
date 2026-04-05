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
    `,
  ],
})
class ColorSwatchesComponent {
  colorGroups = [
    {
      name: 'Brand',
      colors: [
        { token: '--yn-primary', value: '#f97316' },
        { token: '--yn-primary-hover', value: '#ea580c' },
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
      name: 'Surface & Background',
      colors: [
        { token: '--yn-surface', value: '#f0f8f0' },
        { token: '--yn-background', value: '#dcedc8' },
        { token: '--yn-background-subtle', value: '#e8f5e9' },
        { token: '--yn-background-hover', value: '#c8e6c9' },
      ],
    },
    {
      name: 'Border',
      colors: [
        { token: '--yn-border', value: '#a5d6a7' },
        { token: '--yn-border-light', value: '#e8f5e9' },
        { token: '--yn-border-medium', value: '#c8e6c9' },
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
        { token: '--yn-info', value: '#0284c7' },
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

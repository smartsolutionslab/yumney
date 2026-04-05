import { Meta, StoryObj } from '@storybook/angular';
import { Component } from '@angular/core';

@Component({
  selector: 'yn-radii-shadows-docs',
  standalone: true,
  template: `
    <div class="docs">
      <section>
        <h2>Border Radius</h2>
        <div class="radius-grid">
          @for (r of radii; track r.token) {
            <div class="radius-card">
              <div class="radius-box" [style.border-radius]="'var(' + r.token + ')'"></div>
              <code>{{ r.token }}</code>
              <span>{{ r.value }}</span>
            </div>
          }
        </div>
      </section>

      <section>
        <h2>Shadows</h2>
        <div class="shadow-grid">
          @for (s of shadows; track s.token) {
            <div class="shadow-card" [style.box-shadow]="'var(' + s.token + ')'">
              <code>{{ s.token }}</code>
              <span>{{ s.label }}</span>
            </div>
          }
        </div>
      </section>

      <section>
        <h2>Focus Rings</h2>
        <div class="ring-grid">
          <div class="ring-sample" [style.box-shadow]="'var(--yn-ring-primary)'">
            <code>--yn-ring-primary</code>
          </div>
          <div class="ring-sample danger" [style.box-shadow]="'var(--yn-ring-danger)'">
            <code>--yn-ring-danger</code>
          </div>
        </div>
      </section>
    </div>
  `,
  styles: [
    `
      .docs {
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

      .radius-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
        gap: 1rem;
      }
      .radius-card {
        text-align: center;
      }
      .radius-box {
        width: 80px;
        height: 80px;
        background: var(--yn-primary-light);
        border: 2px solid var(--yn-primary);
        margin: 0 auto 0.5rem;
      }
      .radius-card code {
        display: block;
        font-size: 0.6875rem;
        font-family: var(--yn-font-mono);
        color: var(--yn-text-muted);
      }
      .radius-card span {
        font-size: 0.6875rem;
        color: var(--yn-text-light);
      }

      .shadow-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
        gap: 1.5rem;
      }
      .shadow-card {
        padding: 1.5rem;
        border-radius: 12px;
        background: var(--yn-surface);
      }
      .shadow-card code {
        display: block;
        font-size: 0.75rem;
        font-family: var(--yn-font-mono);
        color: var(--yn-text-muted);
        margin-bottom: 0.25rem;
      }
      .shadow-card span {
        font-size: 0.75rem;
        color: var(--yn-text-light);
      }

      .ring-grid {
        display: flex;
        gap: 2rem;
      }
      .ring-sample {
        padding: 1rem 1.5rem;
        border-radius: 10px;
        background: var(--yn-surface);
        border: 1.5px solid var(--yn-primary);
      }
      .ring-sample.danger {
        border-color: var(--yn-danger);
      }
      .ring-sample code {
        font-size: 0.75rem;
        font-family: var(--yn-font-mono);
        color: var(--yn-text-muted);
      }
    `,
  ],
})
class RadiiShadowsDocsComponent {
  radii = [
    { token: '--yn-radius-sm', value: '4px' },
    { token: '--yn-radius-md', value: '8px' },
    { token: '--yn-radius-lg', value: '10px' },
    { token: '--yn-radius-xl', value: '12px' },
    { token: '--yn-radius-2xl', value: '16px' },
    { token: '--yn-radius-full', value: '999px' },
    { token: '--yn-radius-circle', value: '50%' },
  ];

  shadows = [
    { token: '--yn-shadow-xs', label: 'Extra Small' },
    { token: '--yn-shadow-sm', label: 'Small' },
    { token: '--yn-shadow-md', label: 'Medium' },
    { token: '--yn-shadow-lg', label: 'Large' },
    { token: '--yn-shadow-xl', label: 'Extra Large' },
    { token: '--yn-shadow-card', label: 'Card (layered)' },
  ];
}

const meta: Meta<RadiiShadowsDocsComponent> = {
  title: 'Foundations/Radii & Shadows',
  component: RadiiShadowsDocsComponent,
};

export default meta;
type Story = StoryObj<RadiiShadowsDocsComponent>;

export const RadiiAndShadows: Story = {};

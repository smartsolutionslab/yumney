import { Meta, StoryObj } from '@storybook/angular';
import { Component } from '@angular/core';

@Component({
  selector: 'yn-button-docs',
  standalone: true,
  template: `
    <div class="button-docs">
      <section>
        <h2>Button Variants</h2>
        <div class="button-row">
          <button class="btn-primary">Primary</button>
          <button class="btn-secondary">Secondary</button>
          <button class="btn-ghost">Ghost</button>
          <button class="btn-danger">Danger</button>
          <button class="btn-danger-filled">Danger Filled</button>
          <button class="btn-link">Link</button>
        </div>
      </section>

      <section>
        <h2>Disabled</h2>
        <div class="button-row">
          <button class="btn-primary" disabled>Primary</button>
          <button class="btn-secondary" disabled>Secondary</button>
          <button class="btn-ghost" disabled>Ghost</button>
          <button class="btn-danger" disabled>Danger</button>
        </div>
      </section>

      <section>
        <h2>Dashed (Add)</h2>
        <button class="btn-dashed">+ Add Ingredient</button>
      </section>

      <section>
        <h2>Icon Buttons</h2>
        <div class="button-row">
          <button class="btn-icon">&#x2191;</button>
          <button class="btn-icon">&#x2193;</button>
          <button class="btn-icon btn-icon--danger">&#x2715;</button>
          <button class="btn-icon" disabled>&#x2191;</button>
        </div>
      </section>

      <section>
        <h2>Back Link</h2>
        <a class="back-link">&larr; Back to recipes</a>
      </section>
    </div>
  `,
  styles: [
    `
      .button-docs {
        max-width: 800px;
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
      .button-row {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
        align-items: center;
      }
    `,
  ],
})
class ButtonDocsComponent {}

const meta: Meta<ButtonDocsComponent> = {
  title: 'Components/Buttons',
  component: ButtonDocsComponent,
};

export default meta;
type Story = StoryObj<ButtonDocsComponent>;

export const AllVariants: Story = {};

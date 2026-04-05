import { Meta, StoryObj } from '@storybook/angular';
import { Component } from '@angular/core';

@Component({
  selector: 'yn-banner-docs',
  standalone: true,
  template: `
    <div class="banner-docs">
      <div class="error-banner">Something went wrong. Please try again.</div>
      <div class="success-banner">Recipe saved successfully!</div>
    </div>
  `,
  styles: [`
    .banner-docs { max-width: 600px; display: flex; flex-direction: column; gap: 1rem; }
  `],
})
class BannerDocsComponent {}

const meta: Meta<BannerDocsComponent> = {
  title: 'Components/Banners',
  component: BannerDocsComponent,
};

export default meta;
type Story = StoryObj<BannerDocsComponent>;

export const ErrorAndSuccess: Story = {};

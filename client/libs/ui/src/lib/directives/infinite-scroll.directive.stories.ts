import { Component, input, output } from '@angular/core';
import { Meta, StoryObj, moduleMetadata } from '@storybook/angular';
import { expect, fn, within } from 'storybook/test';
import { InfiniteScrollDirective } from './infinite-scroll.directive';

@Component({
  selector: 'yn-infinite-scroll-demo',
  standalone: true,
  imports: [InfiniteScrollDirective],
  template: `
    <div style="height: 240px; overflow-y: auto; border: 1px solid #e5e7eb; border-radius: 8px; padding: 16px;">
      @for (n of items(); track n) {
        <div style="padding: 12px 0; border-bottom: 1px solid #f3f4f6;">Item {{ n }}</div>
      }
      <div
        ynInfiniteScroll
        [enabled]="enabled()"
        (loadMore)="loadMore.emit()"
        style="height: 32px; display: flex; align-items: center; justify-content: center; color: #6b7280; font-size: 12px;"
      >
        {{ enabled() ? 'Scroll sentinel' : 'Disabled' }}
      </div>
    </div>
  `,
})
class InfiniteScrollDemoComponent {
  items = input<number[]>([]);
  enabled = input(true);
  loadMore = output<void>();
}

const meta: Meta<InfiniteScrollDemoComponent> = {
  title: 'Directives/Infinite Scroll',
  component: InfiniteScrollDemoComponent,
  decorators: [moduleMetadata({ imports: [InfiniteScrollDirective] })],
  argTypes: {
    enabled: { control: 'boolean' },
  },
  args: {
    items: Array.from({ length: 20 }, (_, i) => i + 1),
    enabled: true,
    loadMore: fn(),
  },
};

export default meta;
type Story = StoryObj<InfiniteScrollDemoComponent>;

export const Enabled: Story = {};

export const Disabled: Story = {
  args: { enabled: false },
};

export const SentinelVisible_EmitsLoadMore: Story = {
  args: {
    items: [1, 2, 3],
    enabled: true,
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);
    await canvas.findByText('Scroll sentinel');
    // IntersectionObserver fires asynchronously once the sentinel is visible.
    await new Promise((r) => setTimeout(r, 50));
    await expect(args.loadMore).toHaveBeenCalled();
  },
};

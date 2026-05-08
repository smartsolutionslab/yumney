import { Meta, StoryObj, moduleMetadata } from '@storybook/angular';
import { provideRouter } from '@angular/router';
import { ButtonComponent } from '../button/button.component';
import { EmptyStateComponent } from './empty-state.component';

const meta: Meta<EmptyStateComponent> = {
  title: 'Components/Empty State',
  component: EmptyStateComponent,
  decorators: [moduleMetadata({ imports: [ButtonComponent], providers: [provideRouter([])] })],
  argTypes: {
    variant: { control: 'select', options: ['card', 'minimal'] },
    title: { control: 'text' },
    message: { control: 'text' },
    testId: { control: 'text' },
  },
};

export default meta;
type Story = StoryObj<EmptyStateComponent>;

export const Card: Story = {
  args: {
    variant: 'card',
    title: 'recipes.list.empty.title',
    message: 'recipes.list.empty.message',
  },
};

export const CardWithCta: Story = {
  args: {
    variant: 'card',
    title: 'recipes.list.empty.title',
    message: 'recipes.list.empty.message',
  },
  render: (args) => ({
    props: args,
    template: `<yn-empty-state [variant]="variant" [title]="title" [message]="message">
      <yn-button variant="primary" routerLink="/">Get started</yn-button>
    </yn-empty-state>`,
  }),
};

export const Minimal: Story = {
  args: {
    variant: 'minimal',
    message: 'shopping.empty',
  },
};

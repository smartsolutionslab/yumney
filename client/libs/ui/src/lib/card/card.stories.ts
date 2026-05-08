import { Meta, StoryObj } from '@storybook/angular';
import { CardComponent } from './card.component';

const meta: Meta<CardComponent> = {
  title: 'Components/Card',
  component: CardComponent,
  argTypes: {
    variant: { control: 'select', options: ['auth'] },
    title: { control: 'text' },
    subtitle: { control: 'text' },
  },
  render: (args) => ({
    props: args,
    template: `<div class="auth-container">
      <yn-card [variant]="variant" [title]="title" [subtitle]="subtitle">
        <p>Card body content goes here.</p>
      </yn-card>
    </div>`,
  }),
};

export default meta;
type Story = StoryObj<CardComponent>;

export const Default: Story = {
  args: {
    variant: 'auth',
    title: 'auth.register.title',
    subtitle: 'auth.register.subtitle',
  },
};

export const Untitled: Story = {
  args: { variant: 'auth' },
};

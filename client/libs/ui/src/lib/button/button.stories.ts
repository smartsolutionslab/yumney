import { Meta, StoryObj, moduleMetadata } from '@storybook/angular';
import { provideRouter } from '@angular/router';
import { ButtonComponent } from './button.component';

const meta: Meta<ButtonComponent> = {
  title: 'Components/Button',
  component: ButtonComponent,
  decorators: [moduleMetadata({ providers: [provideRouter([])] })],
  argTypes: {
    variant: {
      control: 'select',
      options: ['primary', 'secondary', 'ghost', 'danger', 'danger-filled', 'dashed', 'link'],
    },
    type: { control: 'select', options: ['button', 'submit', 'reset'] },
    disabled: { control: 'boolean' },
    loading: { control: 'boolean' },
    showSpinner: { control: 'boolean' },
    routerLink: { control: 'text' },
    ariaLabel: { control: 'text' },
    testId: { control: 'text' },
    extraClass: { control: 'text' },
  },
  render: (args) => ({
    props: args,
    template: `<yn-button
      [variant]="variant"
      [type]="type"
      [disabled]="disabled"
      [loading]="loading"
      [showSpinner]="showSpinner"
      [routerLink]="routerLink"
      [ariaLabel]="ariaLabel"
      [testId]="testId"
      [extraClass]="extraClass"
    >Click me</yn-button>`,
  }),
};

export default meta;
type Story = StoryObj<ButtonComponent>;

export const Primary: Story = { args: { variant: 'primary' } };
export const Secondary: Story = { args: { variant: 'secondary' } };
export const Ghost: Story = { args: { variant: 'ghost' } };
export const Danger: Story = { args: { variant: 'danger' } };
export const DangerFilled: Story = { args: { variant: 'danger-filled' } };
export const Dashed: Story = { args: { variant: 'dashed' } };
export const Link: Story = { args: { variant: 'link' } };

export const Loading: Story = {
  args: { variant: 'primary', loading: true, showSpinner: true },
};

export const Disabled: Story = {
  args: { variant: 'primary', disabled: true },
};

export const RouterLinkAnchor: Story = {
  args: { variant: 'primary', routerLink: '/recipes' },
};

import { Meta, StoryObj } from '@storybook/angular';
import { SubmitButtonComponent } from './submit-button.component';

const meta: Meta<SubmitButtonComponent> = {
  title: 'Components/Submit Button',
  component: SubmitButtonComponent,
  argTypes: {
    label: { control: 'text' },
    loadingLabel: { control: 'text' },
    loading: { control: 'boolean' },
    disabled: { control: 'boolean' },
    showSpinner: { control: 'boolean' },
    type: { control: 'select', options: ['submit', 'button'] },
  },
};

export default meta;
type Story = StoryObj<SubmitButtonComponent>;

export const Default: Story = {
  args: {
    label: 'Save Recipe',
    loadingLabel: 'Saving...',
    loading: false,
    disabled: false,
    showSpinner: false,
    type: 'submit',
  },
};

export const Loading: Story = {
  args: {
    label: 'Save Recipe',
    loadingLabel: 'Saving...',
    loading: true,
    showSpinner: true,
  },
};

export const Disabled: Story = {
  args: {
    label: 'Save Recipe',
    loadingLabel: 'Saving...',
    disabled: true,
  },
};

import { Meta, StoryObj } from '@storybook/angular';
import { LoadingSpinnerComponent } from './loading-spinner.component';

const meta: Meta<LoadingSpinnerComponent> = {
  title: 'Components/Loading Spinner',
  component: LoadingSpinnerComponent,
  argTypes: {
    label: { control: 'text' },
  },
};

export default meta;
type Story = StoryObj<LoadingSpinnerComponent>;

export const Default: Story = {
  args: {
    label: 'Loading recipes...',
  },
};

export const CustomLabel: Story = {
  args: {
    label: 'Extracting recipe from URL...',
  },
};

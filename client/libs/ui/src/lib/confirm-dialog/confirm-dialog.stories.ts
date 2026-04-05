import { Meta, StoryObj } from '@storybook/angular';
import { ConfirmDialogComponent } from './confirm-dialog.component';

const meta: Meta<ConfirmDialogComponent> = {
  title: 'Components/Confirm Dialog',
  component: ConfirmDialogComponent,
  argTypes: {
    message: { control: 'text' },
    confirmLabel: { control: 'text' },
    cancelLabel: { control: 'text' },
    confirmed: { action: 'confirmed' },
    cancelled: { action: 'cancelled' },
  },
};

export default meta;
type Story = StoryObj<ConfirmDialogComponent>;

export const Default: Story = {
  args: {
    message: 'Are you sure you want to delete this recipe?',
    confirmLabel: 'Delete',
    cancelLabel: 'Cancel',
  },
};

export const CustomLabels: Story = {
  args: {
    message: 'Discard unsaved changes?',
    confirmLabel: 'Yes, discard',
    cancelLabel: 'Keep editing',
  },
};

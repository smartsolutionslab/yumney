import { Meta, StoryObj } from '@storybook/angular';
import { expect, fn, userEvent, within } from 'storybook/test';
import { ConfirmDialogComponent } from './confirm-dialog.component';

const meta: Meta<ConfirmDialogComponent> = {
  title: 'Components/Confirm Dialog',
  component: ConfirmDialogComponent,
  argTypes: {
    message: { control: 'text' },
    confirmLabel: { control: 'text' },
    cancelLabel: { control: 'text' },
  },
  args: {
    confirmed: fn(),
    cancelled: fn(),
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

export const ClickingConfirm_EmitsConfirmed: Story = {
  args: {
    message: 'Delete this recipe?',
    confirmLabel: 'Delete',
    cancelLabel: 'Cancel',
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);
    const button = await canvas.findByRole('button', { name: 'Delete' });
    await userEvent.click(button);

    await expect(args.confirmed).toHaveBeenCalledTimes(1);
    await expect(args.cancelled).not.toHaveBeenCalled();
  },
};

export const ClickingCancel_EmitsCancelled: Story = {
  args: {
    message: 'Delete this recipe?',
    confirmLabel: 'Delete',
    cancelLabel: 'Cancel',
  },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);
    const button = await canvas.findByRole('button', { name: 'Cancel' });
    await userEvent.click(button);

    await expect(args.cancelled).toHaveBeenCalledTimes(1);
    await expect(args.confirmed).not.toHaveBeenCalled();
  },
};

export const PressingEscape_EmitsCancelled: Story = {
  args: {
    message: 'Delete this recipe?',
    confirmLabel: 'Delete',
    cancelLabel: 'Cancel',
  },
  play: async ({ args }) => {
    await userEvent.keyboard('{Escape}');

    await expect(args.cancelled).toHaveBeenCalledTimes(1);
    await expect(args.confirmed).not.toHaveBeenCalled();
  },
};

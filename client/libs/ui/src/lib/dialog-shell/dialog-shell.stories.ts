import { Meta, StoryObj } from '@storybook/angular';
import { DialogShellComponent } from './dialog-shell.component';

const meta: Meta<DialogShellComponent> = {
  title: 'Components/Dialog Shell',
  component: DialogShellComponent,
  argTypes: {
    size: { control: 'select', options: ['sm', 'md', 'lg'] },
    role: { control: 'select', options: ['dialog', 'alertdialog'] },
    labelledBy: { control: 'text' },
    testId: { control: 'text' },
    cancelOnBackdrop: { control: 'boolean' },
    cancelOnEscape: { control: 'boolean' },
  },
  render: (args) => ({
    props: args,
    template: `<yn-dialog-shell
      [size]="size"
      [role]="role"
      [labelledBy]="labelledBy"
      [testId]="testId"
      [cancelOnBackdrop]="cancelOnBackdrop"
      [cancelOnEscape]="cancelOnEscape"
    >
      <header><h2 id="dialog-title">Dialog title</h2><p>Dialog subtitle</p></header>
      <p>Dialog body content goes here.</p>
      <div style="display:flex;gap:.5rem;justify-content:flex-end">
        <button>Cancel</button>
        <button>Confirm</button>
      </div>
    </yn-dialog-shell>`,
  }),
};

export default meta;
type Story = StoryObj<DialogShellComponent>;

export const Small: Story = { args: { size: 'sm', labelledBy: 'dialog-title' } };
export const Medium: Story = { args: { size: 'md', labelledBy: 'dialog-title' } };
export const Large: Story = { args: { size: 'lg', labelledBy: 'dialog-title' } };
export const AlertDialog: Story = {
  args: { size: 'sm', role: 'alertdialog', labelledBy: 'dialog-title' },
};

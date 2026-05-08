import { Meta, StoryObj } from '@storybook/angular';
import { MessageBannerComponent } from './message-banner.component';

const meta: Meta<MessageBannerComponent> = {
  title: 'Components/Message Banner',
  component: MessageBannerComponent,
  argTypes: {
    tone: { control: 'select', options: ['error', 'success'] },
    message: { control: 'text' },
    testId: { control: 'text' },
  },
};

export default meta;
type Story = StoryObj<MessageBannerComponent>;

export const Error: Story = {
  args: {
    tone: 'error',
    message: 'errors.saveFailed',
  },
};

export const Success: Story = {
  args: {
    tone: 'success',
    message: 'success.saved',
  },
};

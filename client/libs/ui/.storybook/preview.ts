import type { Preview } from '@storybook/angular';

const preview: Preview = {
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
    backgrounds: {
      default: 'yumney',
      values: [
        { name: 'yumney', value: '#dcedc8' },
        { name: 'surface', value: '#f0f8f0' },
        { name: 'white', value: '#ffffff' },
        { name: 'dark', value: '#1a2e1a' },
      ],
    },
  },
};

export default preview;

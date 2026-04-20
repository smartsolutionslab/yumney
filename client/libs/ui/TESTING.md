# Testing `@yumney/ui`

Two layers of tests guard the UI library: plain Angular unit specs
(Vitest + Angular Testing Library) and Storybook interaction tests
driven by the `play` function. Pick the layer that matches what the
component needs to prove.

## Unit specs (`*.component.spec.ts`)

- Fast, no rendering to screen.
- Verify component logic in isolation: inputs/outputs, signals,
  pipes, host listeners that don't require a real DOM environment.
- Run with `yarn nx test ui`.

## Interaction tests (`*.stories.ts` with `play`)

- Render the real component in a browser (via Storybook's preview
  iframe), drive it with keyboard/mouse, and assert on spies attached
  to `@Output()` streams.
- Catch regressions that unit specs miss: focus handling, click-
  outside, escape key, ARIA role wiring, visible state transitions.
- Run interactively with `yarn storybook` — results appear in the
  _Interactions_ panel of each story.

### Pattern

```ts
import { Meta, StoryObj } from '@storybook/angular';
import { expect, fn, userEvent, within } from 'storybook/test';
import { FooComponent } from './foo.component';

const meta: Meta<FooComponent> = {
  title: 'Components/Foo',
  component: FooComponent,
  args: {
    // spy-able Output stubs
    confirmed: fn(),
    cancelled: fn(),
  },
};

export default meta;
type Story = StoryObj<FooComponent>;

export const ClickingConfirm_EmitsConfirmed: Story = {
  args: { message: 'Delete?', confirmLabel: 'Delete' },
  play: async ({ args, canvasElement }) => {
    const canvas = within(canvasElement);
    const button = await canvas.findByRole('button', { name: 'Delete' });
    await userEvent.click(button);

    await expect(args.confirmed).toHaveBeenCalledTimes(1);
    await expect(args.cancelled).not.toHaveBeenCalled();
  },
};
```

Naming convention: one story per user action, titled
`When<Action>_<ExpectedOutcome>`. Keeps the Interactions panel
readable and makes failures point at one specific behaviour.

### What to test

Good candidates for `play`:

- Output/event emission after a click, keypress, or focus change.
- Conditional rendering after a state change (assert via `findBy*`).
- Accessibility wiring: `findByRole('button', { name: ... })` fails
  if the label is missing or the role is wrong.

Skip for `play`:

- Pure Output mapping already covered by a unit spec.
- Rendering shape (snapshot) — brittle, little signal.

## Running in CI

Storybook interaction tests currently run only in the interactive
preview. Adding a headless runner (`@storybook/test-runner` +
Playwright) is tracked as a follow-up on the architecture-review
issue — once wired, `yarn test-storybook` will execute every `play`
function in a browser and fail CI on any failed `expect`.

## Current coverage

| Component      | Unit spec | Interaction test               |
| -------------- | --------- | ------------------------------ |
| ConfirmDialog  | ✅        | ✅ (confirm / cancel / escape) |
| FavoriteButton | ✅        | —                              |
| SubmitButton   | ✅        | —                              |
| LoadingSpinner | ✅        | —                              |
| UnitSelect     | ✅        | —                              |
| FormField      | ✅        | —                              |
| SortMenu       | ✅        | —                              |
| ToastHost      | —         | —                              |
| FilterPanel    | —         | —                              |
| AsyncState     | —         | —                              |
| …              | varies    | —                              |

Interaction tests are introduced component-by-component as new
behaviour lands; ConfirmDialog is the reference implementation.

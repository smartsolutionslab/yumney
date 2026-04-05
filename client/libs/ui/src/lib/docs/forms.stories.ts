import { Meta, StoryObj } from '@storybook/angular';
import { Component } from '@angular/core';

@Component({
  selector: 'yn-form-docs',
  standalone: true,
  template: `
    <div class="form-docs">
      <div class="form-field">
        <label for="title">Recipe Title</label>
        <input id="title" type="text" placeholder="Enter recipe title..." />
      </div>

      <div class="form-field">
        <label for="desc">Description</label>
        <textarea id="desc" placeholder="Describe your recipe..."></textarea>
      </div>

      <div class="form-field">
        <label for="servings">Servings</label>
        <select id="servings">
          <option>1</option>
          <option selected>4</option>
          <option>6</option>
          <option>8</option>
        </select>
      </div>

      <div class="form-field">
        <label for="err">Field with error</label>
        <input id="err" type="text" class="ng-invalid ng-touched" value="bad value" />
        <span class="field-error">This field is required</span>
      </div>
    </div>
  `,
  styles: [`
    .form-docs { max-width: 400px; }
  `],
})
class FormDocsComponent {}

const meta: Meta<FormDocsComponent> = {
  title: 'Components/Forms',
  component: FormDocsComponent,
};

export default meta;
type Story = StoryObj<FormDocsComponent>;

export const FormElements: Story = {};

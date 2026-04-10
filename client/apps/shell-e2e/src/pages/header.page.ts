import { type Page, type Locator } from '@playwright/test';
import { SELECTORS } from '../helpers/selectors';

export class HeaderPage {
  readonly langToggle: Locator;
  readonly logoutButton: Locator;

  constructor(private page: Page) {
    this.langToggle = page.locator(SELECTORS.header.langToggle);
    this.logoutButton = page.locator(SELECTORS.header.logout);
  }
}

import { type Page, type Locator } from '@playwright/test';

/**
 * Generic accessors that span every authenticated route — the page-level
 * heading, the app-shell root container, and the SW-managed document
 * metadata. Use this when a spec iterates URLs (a11y smoke, i18n parity)
 * and only needs the boilerplate-level signals.
 */
export class AppShellPage {
  readonly heading: Locator;
  readonly mainContent: Locator;
  readonly root: Locator;
  readonly manifestLink: Locator;
  readonly themeColorMeta: Locator;

  constructor(private page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.mainContent = page.locator('app-root, yn-root, main').first();
    this.root = page.locator('yn-root');
    this.manifestLink = page.locator('link[rel="manifest"]');
    this.themeColorMeta = page.locator('meta[name="theme-color"]');
  }

  navLink(label: string | RegExp): Locator {
    return this.page.locator('.nav-link').filter({ hasText: label });
  }
}

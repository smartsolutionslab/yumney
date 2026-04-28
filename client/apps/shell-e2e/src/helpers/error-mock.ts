import { type Page } from '@playwright/test';

export interface ProblemDetails {
  type?: string;
  title?: string;
  status: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

/**
 * Inject a synthetic ProblemDetails response on the next request matching
 * the given URL pattern. Used by error-path e2e tests to exercise the UI's
 * 5xx / 422 / 409 handling without orchestrating real backend chaos.
 *
 * The body shape mirrors what GlobalExceptionHandlerMiddleware and
 * ValidationExtensions produce in production (RFC 7807 problem+json), so
 * the frontend's error-mapping logic sees realistic input.
 */
export async function mockApiError(
  page: Page,
  urlPattern: string | RegExp,
  status: number,
  problem: Partial<Omit<ProblemDetails, 'status'>> = {},
): Promise<void> {
  await page.route(urlPattern, (route) =>
    route.fulfill({
      status,
      contentType: 'application/problem+json',
      body: JSON.stringify({
        type: problem.type ?? `https://tools.ietf.org/html/rfc7231#section-${status}`,
        title: problem.title ?? defaultTitleFor(status),
        status,
        ...(problem.detail !== undefined && { detail: problem.detail }),
        ...(problem.instance !== undefined && { instance: problem.instance }),
        ...(problem.errors !== undefined && { errors: problem.errors }),
      }),
    }),
  );
}

function defaultTitleFor(status: number): string {
  if (status === 409) return 'Conflict';
  if (status === 422) return 'Validation error(s) occurred.';
  if (status >= 500) return 'Internal Server Error';
  if (status >= 400) return 'Bad Request';
  return 'Error';
}

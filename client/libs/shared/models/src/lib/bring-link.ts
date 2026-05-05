/**
 * Builds a Bring! deep link from a list of shopping items (US-123).
 * Falls back to the universal getbring.com domain when the native scheme isn't
 * available. Items with empty names are skipped.
 *
 * The Bring! deep-link format is `bring://import?items=name1,name2,...`. We
 * include amount + unit inline in the name so the spec stays tolerant of
 * Bring!'s parser doing whatever it wants.
 */
export interface BringItem {
  name: string;
  amount?: number | string | null;
  unit?: string | null;
}

export function buildBringImportUrl(items: ReadonlyArray<BringItem>): string {
  const formatted = items
    .map(formatItem)
    .filter((value): value is string => value !== null);

  if (formatted.length === 0) {
    // Bring! gracefully handles an empty payload by opening to the import
    // landing screen — better UX than throwing.
    return 'bring://import';
  }

  const encoded = formatted.map((value) => encodeURIComponent(value)).join(',');
  return `bring://import?items=${encoded}`;
}

// Stable fallback URL that opens the Bring! marketing site / app store as a
// universal smart link. We surface this when the deep link doesn't resolve
// (the visibilitychange-based detection in the caller).
export const BRING_FALLBACK_URL = 'https://www.getbring.com/';

function formatItem(item: BringItem): string | null {
  const name = item.name?.trim();
  if (!name) return null;

  const amount = item.amount;
  const unit = item.unit?.trim();

  if (amount != null && amount !== '' && unit) return `${amount} ${unit} ${name}`;
  if (amount != null && amount !== '') return `${amount} ${name}`;
  return name;
}

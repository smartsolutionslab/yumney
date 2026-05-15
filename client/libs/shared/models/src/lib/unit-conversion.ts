// Cooking-grade unit conversion. Mirrors src/Yumney.Shared.Quantities/
// UnitConversion.cs — the backend uses the same factors when ingesting
// imperial recipes and normalising shopping-list ingredients. Keep the two in
// lockstep when adding new rules.

export type UnitSystem = 'metric' | 'imperial';

export interface ConvertedAmount {
  amount: number;
  unit: string;
}

interface ConversionRule {
  targetUnit: string;
  factor: number;
}

const metricToImperial = new Map<string, ConversionRule>([
  ['g', { targetUnit: 'oz', factor: 1 / 28.3495 }],
  ['kg', { targetUnit: 'lb', factor: 2.20462 }],
  ['ml', { targetUnit: 'fl oz', factor: 1 / 29.5735 }],
  ['cl', { targetUnit: 'fl oz', factor: 10 / 29.5735 }],
  ['dl', { targetUnit: 'cup', factor: 100 / 236.588 }],
  ['l', { targetUnit: 'cup', factor: 1000 / 236.588 }],
]);

const imperialToMetric = new Map<string, ConversionRule>([
  ['oz', { targetUnit: 'g', factor: 28.3495 }],
  ['lb', { targetUnit: 'g', factor: 453.592 }],
  ['fl oz', { targetUnit: 'ml', factor: 29.5735 }],
  ['cup', { targetUnit: 'ml', factor: 236.588 }],
  ['tsp', { targetUnit: 'ml', factor: 4.92892 }],
  ['tbsp', { targetUnit: 'ml', factor: 14.7868 }],
]);

export function toImperial(amount: number, unit: string | null): ConvertedAmount {
  return apply(amount, unit, metricToImperial);
}

export function toMetric(amount: number, unit: string | null): ConvertedAmount {
  return apply(amount, unit, imperialToMetric);
}

export function toSystem(amount: number, unit: string | null, target: UnitSystem): ConvertedAmount {
  return target === 'imperial' ? toImperial(amount, unit) : toMetric(amount, unit);
}

export function celsiusToFahrenheit(celsius: number): number {
  return Math.round((celsius * 9) / 5 + 32);
}

export function fahrenheitToCelsius(fahrenheit: number): number {
  return Math.round(((fahrenheit - 32) * 5) / 9);
}

// Coerce a converted amount to a number a cook can actually measure. Mirrors
// backend SmartRound — the thresholds are the same so a server-rendered
// ingredient list and a client-converted one agree to the rounded unit.
export function smartRound(value: number): number {
  const absolute = Math.abs(value);
  if (absolute < 1) return roundToStep(value, 0.25);
  if (absolute < 10) return roundToStep(value, 0.5);
  if (absolute < 100) return Math.round(value);
  if (absolute < 1000) return roundToStep(value, 5);
  return roundToStep(value, 10);
}

function apply(amount: number, unit: string | null, table: Map<string, ConversionRule>): ConvertedAmount {
  const normalized = (unit ?? '').trim();
  if (normalized.length === 0) {
    return { amount: smartRound(amount), unit: '' };
  }

  const rule = table.get(normalized.toLowerCase());
  if (!rule) return { amount, unit: normalized };

  return { amount: smartRound(amount * rule.factor), unit: rule.targetUnit };
}

function roundToStep(value: number, step: number): number {
  // Half-away-from-zero rounding matches the backend's
  // MidpointRounding.AwayFromZero so the two sides agree on edge cases like
  // 0.125 → 0.25, 0.375 → 0.5, 2.5 → 3.
  const sign = value < 0 ? -1 : 1;
  return sign * Math.round(Math.abs(value) / step + Number.EPSILON) * step;
}

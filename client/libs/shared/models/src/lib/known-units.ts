export type UnitGroup = 'volume' | 'weight' | 'count';

export interface KnownUnit {
  value: string;
  labelKey: string;
  group: UnitGroup;
}

export interface UnitGroupInfo {
  key: UnitGroup;
  labelKey: string;
  units: KnownUnit[];
}

export const UNIT_GROUPS: readonly { key: UnitGroup; labelKey: string }[] = [
  { key: 'volume', labelKey: 'units.group.volume' },
  { key: 'weight', labelKey: 'units.group.weight' },
  { key: 'count', labelKey: 'units.group.count' },
] as const;

export const KNOWN_UNITS: readonly KnownUnit[] = [
  // Volume
  { value: 'ml', labelKey: 'units.ml', group: 'volume' },
  { value: 'cl', labelKey: 'units.cl', group: 'volume' },
  { value: 'dl', labelKey: 'units.dl', group: 'volume' },
  { value: 'l', labelKey: 'units.l', group: 'volume' },
  { value: 'tsp', labelKey: 'units.tsp', group: 'volume' },
  { value: 'tbsp', labelKey: 'units.tbsp', group: 'volume' },
  { value: 'cup', labelKey: 'units.cup', group: 'volume' },
  { value: 'fl oz', labelKey: 'units.flOz', group: 'volume' },

  // Weight
  { value: 'g', labelKey: 'units.g', group: 'weight' },
  { value: 'kg', labelKey: 'units.kg', group: 'weight' },
  { value: 'oz', labelKey: 'units.oz', group: 'weight' },
  { value: 'lb', labelKey: 'units.lb', group: 'weight' },

  // Count / Misc
  { value: 'piece', labelKey: 'units.piece', group: 'count' },
  { value: 'slice', labelKey: 'units.slice', group: 'count' },
  { value: 'pinch', labelKey: 'units.pinch', group: 'count' },
  { value: 'bunch', labelKey: 'units.bunch', group: 'count' },
  { value: 'clove', labelKey: 'units.clove', group: 'count' },
  { value: 'can', labelKey: 'units.can', group: 'count' },
  { value: 'pack', labelKey: 'units.pack', group: 'count' },
  { value: 'handful', labelKey: 'units.handful', group: 'count' },
  { value: 'dash', labelKey: 'units.dash', group: 'count' },
  { value: 'drop', labelKey: 'units.drop', group: 'count' },
  { value: 'sprig', labelKey: 'units.sprig', group: 'count' },
  { value: 'stalk', labelKey: 'units.stalk', group: 'count' },
  { value: 'sheet', labelKey: 'units.sheet', group: 'count' },
] as const;

export function getGroupedUnits(): UnitGroupInfo[] {
  return UNIT_GROUPS.map(({ key, labelKey }) => ({
    key,
    labelKey,
    units: KNOWN_UNITS.filter((unit) => unit.group === key),
  }));
}

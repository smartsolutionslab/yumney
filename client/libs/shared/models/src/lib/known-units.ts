export interface KnownUnit {
  value: string;
  labelKey: string;
}

export const KNOWN_UNITS: readonly KnownUnit[] = [
  // Volume
  { value: 'ml', labelKey: 'units.ml' },
  { value: 'cl', labelKey: 'units.cl' },
  { value: 'dl', labelKey: 'units.dl' },
  { value: 'l', labelKey: 'units.l' },
  { value: 'tsp', labelKey: 'units.tsp' },
  { value: 'tbsp', labelKey: 'units.tbsp' },
  { value: 'cup', labelKey: 'units.cup' },
  { value: 'fl oz', labelKey: 'units.flOz' },

  // Weight
  { value: 'g', labelKey: 'units.g' },
  { value: 'kg', labelKey: 'units.kg' },
  { value: 'oz', labelKey: 'units.oz' },
  { value: 'lb', labelKey: 'units.lb' },

  // Count / Misc
  { value: 'piece', labelKey: 'units.piece' },
  { value: 'slice', labelKey: 'units.slice' },
  { value: 'pinch', labelKey: 'units.pinch' },
  { value: 'bunch', labelKey: 'units.bunch' },
  { value: 'clove', labelKey: 'units.clove' },
  { value: 'can', labelKey: 'units.can' },
  { value: 'pack', labelKey: 'units.pack' },
  { value: 'handful', labelKey: 'units.handful' },
  { value: 'dash', labelKey: 'units.dash' },
  { value: 'drop', labelKey: 'units.drop' },
  { value: 'sprig', labelKey: 'units.sprig' },
  { value: 'stalk', labelKey: 'units.stalk' },
  { value: 'sheet', labelKey: 'units.sheet' },
] as const;

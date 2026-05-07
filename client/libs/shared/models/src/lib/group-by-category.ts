export interface CategoryGroup<TItem, TCategory extends string = string> {
  category: TCategory;
  items: TItem[];
}

export interface GroupByCategoryOptions<TCategory extends string> {
  order?: readonly TCategory[];
}

export function groupByCategory<TItem, TCategory extends string = string>(
  items: readonly TItem[],
  getCategory: (item: TItem) => TCategory,
  options: GroupByCategoryOptions<TCategory> = {},
): CategoryGroup<TItem, TCategory>[] {
  const buckets = new Map<TCategory, TItem[]>();
  for (const item of items) {
    const category = getCategory(item);
    const bucket = buckets.get(category) ?? [];
    bucket.push(item);
    buckets.set(category, bucket);
  }

  const { order } = options;
  if (order) {
    return order
      .filter((category) => buckets.has(category))
      .map((category) => ({ category, items: buckets.get(category) ?? [] }));
  }

  return Array.from(buckets.entries()).map(([category, bucketItems]) => ({
    category,
    items: bucketItems,
  }));
}

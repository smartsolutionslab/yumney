const MS_PER_DAY = 86_400_000;

export const DAY_NAMES = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'] as const;

const JS_DAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'] as const;

/**
 * ISO-ish week number for the given date — Jan-4 is week 1 of its year, which
 * lines up with the way the API server numbers weeks. Used by the meal-planner
 * to pick the "current" week on first load and to label past/future weeks.
 */
export function getCurrentWeek(now: Date = new Date()): number {
  const jan4 = new Date(now.getFullYear(), 0, 4);
  const daysDiff = Math.floor((now.getTime() - jan4.getTime()) / MS_PER_DAY);
  return Math.ceil((daysDiff + jan4.getDay() + 1) / 7);
}

export function isToday(dayName: string, now: Date = new Date()): boolean {
  return JS_DAY_NAMES[now.getDay()] === dayName;
}

import { DAY_NAMES, getCurrentWeek, isToday } from './meal-planner-dates';

describe('DAY_NAMES', () => {
  it('starts on Monday and lists 7 days', () => {
    expect(DAY_NAMES).toEqual(['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']);
  });
});

describe('getCurrentWeek', () => {
  it('returns week 1 for Jan-4 of any year (ISO-ish boundary)', () => {
    expect(getCurrentWeek(new Date(2026, 0, 4))).toBe(1);
  });

  it('returns week 2 for Jan-11 (one week after Jan-4)', () => {
    expect(getCurrentWeek(new Date(2026, 0, 11))).toBe(2);
  });

  it('returns a value between 1 and 53 for any date in the year', () => {
    const dec31 = getCurrentWeek(new Date(2026, 11, 31));
    expect(dec31).toBeGreaterThanOrEqual(52);
    expect(dec31).toBeLessThanOrEqual(53);
  });
});

describe('isToday', () => {
  it('returns true for the day matching the supplied date', () => {
    // 2026-03-09 is a Monday.
    expect(isToday('Monday', new Date(2026, 2, 9))).toBe(true);
  });

  it('returns false for a non-matching day', () => {
    expect(isToday('Friday', new Date(2026, 2, 9))).toBe(false);
  });
});

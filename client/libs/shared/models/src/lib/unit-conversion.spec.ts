import { celsiusToFahrenheit, fahrenheitToCelsius, smartRound, toImperial, toMetric, toSystem } from './unit-conversion';

describe('unit-conversion', () => {
  describe('toImperial', () => {
    it('converts grams to ounces', () => {
      const result = toImperial(500, 'g');
      expect(result.unit).toBe('oz');
      // 500g ≈ 17.64 oz → SmartRound for <100 rounds to nearest whole
      expect(result.amount).toBe(18);
    });

    it('converts kilograms to pounds', () => {
      const result = toImperial(2, 'kg');
      expect(result.unit).toBe('lb');
      // 2kg = 4.41 lb → <10 rounds to 0.5 step
      expect(result.amount).toBe(4.5);
    });

    it('converts millilitres to fluid ounces', () => {
      const result = toImperial(200, 'ml');
      expect(result.unit).toBe('fl oz');
      // 200ml ≈ 6.76 fl oz → <10 rounds to 0.5 step
      expect(result.amount).toBe(7);
    });

    it('converts litres to cups', () => {
      const result = toImperial(1, 'l');
      expect(result.unit).toBe('cup');
      // 1L ≈ 4.23 cup → <10 rounds to 0.5 step
      expect(result.amount).toBe(4);
    });

    it('passes through unknown units unchanged', () => {
      const result = toImperial(3, 'pinch');
      expect(result.unit).toBe('pinch');
      expect(result.amount).toBe(3);
    });

    it('treats empty unit as countable and only smart-rounds', () => {
      // 1.7 falls in the absolute<10 bucket → rounds to nearest 0.5.
      const result = toImperial(1.7, null);
      expect(result.unit).toBe('');
      expect(result.amount).toBe(1.5);
    });
  });

  describe('toMetric', () => {
    it('converts ounces to grams', () => {
      const result = toMetric(8, 'oz');
      expect(result.unit).toBe('g');
      // 8oz = 226.79 g → 100..1000 rounds to 5 step
      expect(result.amount).toBe(225);
    });

    it('converts cups to millilitres', () => {
      const result = toMetric(1, 'cup');
      expect(result.unit).toBe('ml');
      // 1 cup = 236.59 ml → 100..1000 rounds to 5 step
      expect(result.amount).toBe(235);
    });
  });

  describe('toSystem', () => {
    it('routes to imperial when target is imperial', () => {
      expect(toSystem(500, 'g', 'imperial').unit).toBe('oz');
    });

    it('routes to metric when target is metric', () => {
      expect(toSystem(8, 'oz', 'metric').unit).toBe('g');
    });
  });

  describe('temperature', () => {
    it('converts 180°C → 356°F', () => {
      expect(celsiusToFahrenheit(180)).toBe(356);
    });

    it('converts 350°F → 177°C', () => {
      expect(fahrenheitToCelsius(350)).toBe(177);
    });
  });

  describe('smartRound', () => {
    it('rounds tiny values to nearest 0.25', () => {
      expect(smartRound(0.34)).toBe(0.25);
      expect(smartRound(0.4)).toBe(0.5);
    });

    it('rounds small values to nearest 0.5', () => {
      expect(smartRound(4.3)).toBe(4.5);
    });

    it('rounds mid values to nearest whole', () => {
      expect(smartRound(17.64)).toBe(18);
    });

    it('rounds large values to nearest 5', () => {
      expect(smartRound(127.4)).toBe(125);
    });

    it('rounds huge values to nearest 10', () => {
      expect(smartRound(1234)).toBe(1230);
    });
  });
});

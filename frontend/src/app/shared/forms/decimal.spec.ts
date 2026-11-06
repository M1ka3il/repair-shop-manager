import { FormControl } from '@angular/forms';
import { decimalInRange, parseDecimal } from './decimal';

describe('parseDecimal', () => {
  it.each([
    ['1,5', 1.5],
    ['89', 89],
    ['89.00', 89],
    [' 0,01 ', 0.01],
  ])('parses %j as %d', (raw, expected) => {
    expect(parseDecimal(raw)).toBe(expected);
  });

  it.each(['', 'abc', '1,234', '1.234,50', '-1', '1,5,0'])('rejects %j', (raw) => {
    expect(parseDecimal(raw)).toBeNull();
  });
});

describe('decimalInRange', () => {
  const validate = (value: string) =>
    new FormControl(value, { nonNullable: true, validators: decimalInRange(0.01, 100) }).errors;

  it('accepts values inside the range', () => {
    expect(validate('0,01')).toBeNull();
    expect(validate('100')).toBeNull();
  });

  it('names the violated bound', () => {
    expect(validate('')).toEqual({ required: true });
    expect(validate('x')).toEqual({ decimal: true });
    expect(validate('0')).toEqual({ min: { min: 0.01 } });
    expect(validate('100,01')).toEqual({ max: { max: 100 } });
  });
});

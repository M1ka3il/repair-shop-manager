import { PRESETS, isValidPeriod, iso, toInstants } from './period';

const preset = (id: string) => PRESETS.find((p) => p.id === id)!;
// 6 March 2026, local time
const today = new Date(2026, 2, 6, 15, 30);

describe('report period', () => {
  it('builds the quick picks relative to today', () => {
    expect(preset('this-month').period(today)).toEqual({ from: '2026-03-01', to: '2026-03-06' });
    expect(preset('last-month').period(today)).toEqual({ from: '2026-02-01', to: '2026-02-28' });
    expect(preset('this-year').period(today)).toEqual({ from: '2026-01-01', to: '2026-03-06' });
    expect(preset('last-12-months').period(today)).toEqual({
      from: '2025-03-07',
      to: '2026-03-06',
    });
  });

  it('handles the January edge of "last month"', () => {
    expect(preset('last-month').period(new Date(2026, 0, 15))).toEqual({
      from: '2025-12-01',
      to: '2025-12-31',
    });
  });

  it('sends the whole last day, not midnight at its start', () => {
    const { from, to } = toInstants({ from: '2026-03-01', to: '2026-03-06' });

    // Whatever the test machine's zone: the bounds are local start and end of day.
    expect(new Date(from)).toEqual(new Date(2026, 2, 1, 0, 0, 0, 0));
    expect(new Date(to)).toEqual(new Date(2026, 2, 6, 23, 59, 59, 999));
    expect(from.endsWith('Z') && to.endsWith('Z')).toBe(true);
  });

  it('formats local dates without shifting them to UTC', () => {
    expect(iso(new Date(2026, 0, 1, 0, 30))).toBe('2026-01-01');
  });

  it('rejects inverted or malformed periods', () => {
    expect(isValidPeriod({ from: '2026-03-01', to: '2026-03-01' })).toBe(true);
    expect(isValidPeriod({ from: '2026-03-02', to: '2026-03-01' })).toBe(false);
    expect(isValidPeriod({ from: '', to: '2026-03-01' })).toBe(false);
  });
});

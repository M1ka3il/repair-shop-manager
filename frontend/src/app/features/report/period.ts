/** A period as the user picks it: whole local days, both inclusive, as yyyy-MM-dd. */
export interface Period {
  readonly from: string;
  readonly to: string;
}

export interface Preset {
  readonly id: string;
  readonly label: string;
  readonly period: (today: Date) => Period;
}

/** Quick picks for the date range, relative to "today". */
export const PRESETS: readonly Preset[] = [
  {
    id: 'this-month',
    label: 'Dieser Monat',
    period: (t) => ({ from: iso(new Date(t.getFullYear(), t.getMonth(), 1)), to: iso(t) }),
  },
  {
    id: 'last-month',
    label: 'Letzter Monat',
    period: (t) => ({
      from: iso(new Date(t.getFullYear(), t.getMonth() - 1, 1)),
      to: iso(new Date(t.getFullYear(), t.getMonth(), 0)), // day 0 = last day of previous month
    }),
  },
  {
    id: 'this-year',
    label: 'Dieses Jahr',
    period: (t) => ({ from: iso(new Date(t.getFullYear(), 0, 1)), to: iso(t) }),
  },
  {
    id: 'last-12-months',
    label: 'Letzte 12 Monate',
    period: (t) => ({
      from: iso(new Date(t.getFullYear() - 1, t.getMonth(), t.getDate() + 1)),
      to: iso(t),
    }),
  },
];

export const DEFAULT_PRESET = 'this-year';

/**
 * The instants the API needs. The API's "to" is an inclusive point in time,
 * so sending just the date would mean midnight at the START of the last day
 * and silently drop that whole day. The user thinks in local days, so both
 * bounds are local start/end of day, converted to UTC.
 */
export function toInstants(period: Period): { from: string; to: string } {
  const [fy, fm, fd] = period.from.split('-').map(Number);
  const [ty, tm, td] = period.to.split('-').map(Number);
  return {
    from: new Date(fy, fm - 1, fd, 0, 0, 0, 0).toISOString(),
    to: new Date(ty, tm - 1, td, 23, 59, 59, 999).toISOString(),
  };
}

/** True for two valid yyyy-MM-dd dates with from <= to. */
export function isValidPeriod(period: Period): boolean {
  const pattern = /^\d{4}-\d{2}-\d{2}$/;
  return pattern.test(period.from) && pattern.test(period.to) && period.from <= period.to;
}

/** Local calendar date as yyyy-MM-dd (not toISOString, which would shift to UTC). */
export function iso(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

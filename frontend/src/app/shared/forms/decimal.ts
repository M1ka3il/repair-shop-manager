import type { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Parses a German-style decimal as typed in a workshop: "1,5", "89", "89.00".
 * At most two decimals — the database columns are decimal(…,2), so anything
 * finer would be rounded silently. Thousands separators are not accepted:
 * "1.234,50" is ambiguous, and an ambiguous price is worse than an error.
 */
export function parseDecimal(raw: string | null | undefined): number | null {
  const normalised = (raw ?? '').trim().replace(',', '.');
  return /^\d+(\.\d{1,2})?$/.test(normalised) ? Number(normalised) : null;
}

/** Validates a text control holding a decimal within [min, max]. */
export function decimalInRange(min: number, max: number): ValidatorFn {
  return (control: AbstractControl<string>): ValidationErrors | null => {
    if (control.value === '' || control.value == null) {
      return { required: true };
    }
    const value = parseDecimal(control.value);
    if (value === null) {
      return { decimal: true };
    }
    if (value < min) {
      return { min: { min } };
    }
    return value > max ? { max: { max } } : null;
  };
}

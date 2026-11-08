import type { AbstractControl } from '@angular/forms';

/** Display bounds for numeric errors, already formatted for German readers. */
export interface Bounds {
  readonly min?: string;
  readonly max?: string;
}

/**
 * The German message for a control's first error, or null when it should not
 * be shown yet: valid, disabled, or not yet touched and the form not yet
 * submitted. One wording for every form in the app.
 */
export function validationMessage(
  control: AbstractControl,
  submitted: boolean,
  bounds: Bounds = {},
): string | null {
  if (control.valid || control.disabled || !(control.touched || submitted)) {
    return null;
  }
  const errors = control.errors ?? {};
  if (errors['required']) return 'Pflichtfeld.';
  if (errors['minlength']) return `Mindestens ${errors['minlength'].requiredLength} Zeichen.`;
  if (errors['maxlength']) return `Höchstens ${errors['maxlength'].requiredLength} Zeichen.`;
  if (errors['email']) return 'Keine gültige E-Mail-Adresse.';
  if (errors['year']) return 'Vierstellige Jahreszahl, z. B. 2021.';
  if (errors['decimal']) return 'Zahl mit höchstens 2 Nachkommastellen, z. B. 89,50.';
  if (errors['min']) return `Mindestens ${bounds.min ?? errors['min'].min}.`;
  if (errors['max']) return `Höchstens ${bounds.max ?? errors['max'].max}.`;
  return 'Ungültig.';
}

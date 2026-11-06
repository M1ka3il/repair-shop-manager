import { FormControl, Validators } from '@angular/forms';
import type { SaveCustomerRequest, SaveVehicleRequest } from '../../core/api/models';

/**
 * Form controls for customer and vehicle master data, shared by every screen
 * that edits them. Client-side checks mirror only the request DTOs'
 * annotations (required, lengths, e-mail format); plate format and year range
 * are domain rules, decided by the server and shown from its answer.
 */
export function customerControls() {
  return {
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(2), Validators.maxLength(200)],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.email, Validators.maxLength(200)],
    }),
    phone: new FormControl('', { nonNullable: true, validators: Validators.maxLength(50) }),
  };
}

export function vehicleControls() {
  return {
    licensePlate: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(15)],
    }),
    make: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)],
    }),
    model: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)],
    }),
    // Only the shape is checked here ("a year"); the plausible range is the domain's call.
    year: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        (control) => (/^\d{4}$/.test(control.value) ? null : { year: true }),
      ],
    }),
  };
}

export function toCustomerRequest(value: {
  name: string;
  email: string;
  phone: string;
}): SaveCustomerRequest {
  return {
    name: value.name.trim(),
    email: value.email.trim() || null,
    phone: value.phone.trim() || null,
  };
}

export function toVehicleRequest(value: {
  licensePlate: string;
  make: string;
  model: string;
  year: string;
}): SaveVehicleRequest {
  return {
    licensePlate: value.licensePlate.trim(),
    make: value.make.trim(),
    model: value.model.trim(),
    year: Number(value.year),
  };
}

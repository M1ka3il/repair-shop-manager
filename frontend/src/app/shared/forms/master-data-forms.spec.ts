import { FormGroup } from '@angular/forms';
import {
  customerControls,
  toCustomerRequest,
  toVehicleRequest,
  vehicleControls,
} from './master-data-forms';
import { validationMessage } from './validation-message';

describe('master data forms', () => {
  it('turns blank optional contact fields into null, as the API stores "unknown"', () => {
    expect(toCustomerRequest({ name: ' Anna Becker ', email: ' ', phone: '' })).toEqual({
      name: 'Anna Becker',
      email: null,
      phone: null,
    });
  });

  it('sends the year as a number and leaves plate normalisation to the server', () => {
    expect(
      toVehicleRequest({ licensePlate: ' m-ab 1234 ', make: 'VW ', model: ' Golf', year: '2021' }),
    ).toEqual({
      licensePlate: 'm-ab 1234',
      make: 'VW',
      model: 'Golf',
      year: 2021,
    });
  });

  it('explains each failing field in German once submitted', () => {
    const form = new FormGroup({ ...customerControls(), ...vehicleControls() });
    form.patchValue({ name: 'A', email: 'keine-mail', year: '21' });

    expect(validationMessage(form.controls.name, false)).toBeNull(); // not touched, not submitted
    expect(validationMessage(form.controls.name, true)).toBe('Mindestens 2 Zeichen.');
    expect(validationMessage(form.controls.email, true)).toBe('Keine gültige E-Mail-Adresse.');
    expect(validationMessage(form.controls.year, true)).toBe(
      'Vierstellige Jahreszahl, z. B. 2021.',
    );
    expect(validationMessage(form.controls.licensePlate, true)).toBe('Pflichtfeld.');
    expect(validationMessage(form.controls.phone, true)).toBeNull(); // optional
  });
});

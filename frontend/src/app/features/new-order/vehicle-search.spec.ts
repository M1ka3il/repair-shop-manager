import type { Vehicle } from '../../core/api/models';
import { searchVehicles, toVehicleOptions } from './vehicle-search';

const vehicle = (
  id: number,
  licensePlate: string,
  customerId: number,
  make = 'VW',
  model = 'Golf',
): Vehicle => ({
  id,
  customerId,
  licensePlate,
  make,
  model,
  year: 2020,
});

const options = toVehicleOptions(
  [
    vehicle(1, 'M-SD 4711', 1, 'Opel', 'Corsa F'),
    vehicle(2, 'B-KL 512', 2),
    vehicle(3, 'X-Y 1', 99),
  ],
  [
    { id: 1, name: 'Stefan Dorn', email: null, phone: null, vehicles: [] },
    { id: 2, name: 'Kfz Lorenz GmbH', email: null, phone: null, vehicles: [] },
  ],
);

const ids = (query: string) => searchVehicles(options, query).map((o) => o.vehicle.id);

describe('vehicle search', () => {
  it('joins the owner name and falls back for an unknown owner', () => {
    expect(options.map((o) => o.customerName)).toEqual(['Stefan Dorn', 'Kfz Lorenz GmbH', '–']);
  });

  it('returns everything for an empty query', () => {
    expect(ids('  ')).toEqual([1, 2, 3]);
  });

  it('finds plates typed without separators or in lower case', () => {
    expect(ids('msd4711')).toEqual([1]);
    expect(ids('b-kl')).toEqual([2]);
  });

  it('finds by owner and by make/model', () => {
    expect(ids('lorenz')).toEqual([2]);
    expect(ids('opel corsa')).toEqual([1]);
  });
});

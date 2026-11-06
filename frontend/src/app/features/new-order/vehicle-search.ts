import type { Customer, Vehicle } from '../../core/api/models';

export interface VehicleOption {
  readonly vehicle: Vehicle;
  readonly customerName: string;
}

/** Joins vehicles with their owner's name; the vehicle DTO only carries the id. */
export function toVehicleOptions(
  vehicles: readonly Vehicle[],
  customers: readonly Customer[],
): VehicleOption[] {
  const names = new Map(customers.map((c) => [c.id, c.name]));
  return vehicles.map((vehicle) => ({
    vehicle,
    customerName: names.get(vehicle.customerId) ?? '–',
  }));
}

/**
 * Case- and whitespace-insensitive search over plate, owner, make and model.
 * "msd4711" finds "M-SD 4711": at the counter, people type plates without
 * separators.
 */
export function searchVehicles(options: readonly VehicleOption[], query: string): VehicleOption[] {
  const needle = normalise(query);
  if (!needle) {
    return [...options];
  }
  return options.filter(({ vehicle, customerName }) =>
    [vehicle.licensePlate, customerName, `${vehicle.make} ${vehicle.model}`].some((field) =>
      normalise(field).includes(needle),
    ),
  );
}

function normalise(value: string): string {
  return value.toLowerCase().replace(/[\s-]/g, '');
}

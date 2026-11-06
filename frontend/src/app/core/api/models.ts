/**
 * Friendly names for the generated OpenAPI types. Components import from here,
 * never from `schema.gen.ts` directly, so a regenerated schema changes one file.
 */
import type { components } from './schema.gen';

type Schemas = components['schemas'];

export type RepairOrder = Schemas['RepairOrderDto'];
export type RepairItem = Schemas['RepairItemDto'];
export type RepairOrderStatus = Schemas['RepairOrderStatus'];
export type RepairItemType = Schemas['RepairItemType'];
export type CreateRepairOrderRequest = Schemas['CreateRepairOrderRequest'];
export type UpdateRepairOrderRequest = Schemas['UpdateRepairOrderRequest'];
export type ChangeStatusRequest = Schemas['ChangeStatusRequest'];
export type AddRepairItemRequest = Schemas['AddRepairItemRequest'];

export type Customer = Schemas['CustomerDto'];
export type Vehicle = Schemas['VehicleDto'];
export type SaveCustomerRequest = Schemas['SaveCustomerRequest'];
export type SaveVehicleRequest = Schemas['SaveVehicleRequest'];

export type CustomerRevenue = Schemas['CustomerRevenueDto'];

import type { RepairOrder } from '../../core/api/models';
import { STATUSES, type StatusMeta } from '../../core/status/repair-order-status';

export interface BoardColumn extends StatusMeta {
  readonly orders: readonly RepairOrder[];
}

/**
 * One column per status, always all five and in lifecycle order — an empty
 * "Wartet auf Teil" column is information too. Within a column the API's order
 * (newest first) is kept.
 */
export function groupByStatus(orders: readonly RepairOrder[]): BoardColumn[] {
  return STATUSES.map((meta) => ({
    ...meta,
    orders: orders.filter((order) => order.status === meta.status),
  }));
}

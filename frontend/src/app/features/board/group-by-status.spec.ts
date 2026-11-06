import type { RepairOrder, RepairOrderStatus } from '../../core/api/models';
import { groupByStatus } from './group-by-status';

const order = (id: number, status: RepairOrderStatus): RepairOrder => ({
  id,
  vehicleId: 1,
  licensePlate: 'M-AB 1234',
  customerName: 'Anna Becker',
  status,
  receivedAtUtc: '2026-11-01T08:00:00Z',
  completedAtUtc: null,
  invoicedAtUtc: null,
  description: 'Test',
  totalPrice: 0,
  items: [],
  allowedTransitions: [],
});

describe('groupByStatus', () => {
  it('always returns all five columns in lifecycle order, even when empty', () => {
    const columns = groupByStatus([]);

    expect(columns.map((c) => c.status)).toEqual([
      'Received',
      'InProgress',
      'WaitingForParts',
      'Completed',
      'Invoiced',
    ]);
    expect(columns.every((c) => c.orders.length === 0)).toBe(true);
  });

  it('puts every order into its status column and keeps the API order', () => {
    const columns = groupByStatus([
      order(3, 'InProgress'),
      order(2, 'Invoiced'),
      order(1, 'InProgress'),
    ]);

    const inProgress = columns.find((c) => c.status === 'InProgress')!;
    expect(inProgress.orders.map((o) => o.id)).toEqual([3, 1]);
    expect(columns.find((c) => c.status === 'Invoiced')!.orders.map((o) => o.id)).toEqual([2]);
  });
});

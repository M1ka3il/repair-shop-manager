import type { RepairOrderStatus } from '../api/models';

export interface StatusMeta {
  readonly status: RepairOrderStatus;
  readonly label: string;
  /** CSS custom property holding the status colour, see styles.css. */
  readonly colorVar: string;
}

/**
 * Display metadata only — labels and colours, in lifecycle order. Which
 * transitions are allowed is deliberately NOT duplicated here: that rule lives
 * in the domain, and the UI should ask the API rather than keep a second copy.
 */
export const STATUSES: readonly StatusMeta[] = [
  { status: 'Received', label: 'Angenommen', colorVar: '--status-received' },
  { status: 'InProgress', label: 'In Arbeit', colorVar: '--status-in-progress' },
  { status: 'WaitingForParts', label: 'Wartet auf Teil', colorVar: '--status-waiting' },
  { status: 'Completed', label: 'Fertig', colorVar: '--status-completed' },
  { status: 'Invoiced', label: 'Abgerechnet', colorVar: '--status-invoiced' },
];

/** Work is finished: items are locked and the order cannot be deleted. */
export function isWorkFinished(status: RepairOrderStatus): boolean {
  return status === 'Completed' || status === 'Invoiced';
}

/** Display metadata for one status. */
export function statusMeta(status: RepairOrderStatus): StatusMeta {
  return STATUSES.find((meta) => meta.status === status)!;
}

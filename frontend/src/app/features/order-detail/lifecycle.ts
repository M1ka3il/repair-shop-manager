import type { RepairOrderStatus } from '../../core/api/models';
import { statusMeta } from '../../core/status/repair-order-status';

export interface LifecycleStep {
  readonly status: RepairOrderStatus;
  readonly label: string;
  readonly state: 'done' | 'current' | 'todo';
  /** Shown under the step, e.g. while the order waits for a part. */
  readonly note: string | null;
}

/**
 * The main path an order travels. "Waiting for parts" is a detour from
 * "In progress", not a step on the way: an order can be completed without ever
 * waiting, so a linear stepper would mark it "done" for orders that never were.
 */
const MAIN_PATH: readonly RepairOrderStatus[] = ['Received', 'InProgress', 'Completed', 'Invoiced'];

export function lifecycleSteps(status: RepairOrderStatus): LifecycleStep[] {
  const waiting = status === 'WaitingForParts';
  const position = waiting ? MAIN_PATH.indexOf('InProgress') : MAIN_PATH.indexOf(status);

  return MAIN_PATH.map((step, index) => ({
    status: step,
    label: statusMeta(step).label,
    state: index < position ? 'done' : index === position ? 'current' : 'todo',
    note: waiting && step === 'InProgress' ? statusMeta('WaitingForParts').label : null,
  }));
}

/** Verb for the button that performs a transition — what the user does, not the target name. */
export function actionLabel(from: RepairOrderStatus, to: RepairOrderStatus): string {
  switch (to) {
    case 'InProgress':
      return from === 'WaitingForParts' ? 'Teil ist da' : 'Arbeit beginnen';
    case 'WaitingForParts':
      return 'Wartet auf Teil';
    case 'Completed':
      return 'Fertig melden';
    case 'Invoiced':
      return 'Abrechnen';
    default:
      return statusMeta(to).label;
  }
}

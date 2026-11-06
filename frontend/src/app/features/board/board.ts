import { CurrencyPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import type { RepairOrder, RepairOrderStatus } from '../../core/api/models';
import { RepairOrdersApi } from '../../core/api/repair-orders.api';
import { toApiError } from '../../core/http/api-error';
import { STATUSES, isWorkFinished, statusMeta } from '../../core/status/repair-order-status';
import { groupByStatus } from './group-by-status';

interface Notice {
  readonly kind: 'success' | 'error';
  readonly title: string;
  readonly detail: string;
}

/**
 * Start page: all repair orders as a board, one column per status. Selecting a
 * card opens an action bar with exactly the moves the API reports as allowed
 * (`allowedTransitions`) — the lifecycle rules are not duplicated here.
 */
@Component({
  selector: 'app-board',
  imports: [RouterLink, CurrencyPipe, DatePipe],
  templateUrl: './board.html',
  styleUrl: './board.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Board {
  private readonly api = inject(RepairOrdersApi);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly orders = rxResource({ stream: () => this.api.list() });

  protected readonly columns = computed(() =>
    groupByStatus(this.orders.hasValue() ? this.orders.value() : []),
  );

  protected readonly error = computed(() => {
    const error = this.orders.error();
    return error ? toApiError(error) : null;
  });

  protected readonly selectedId = signal<number | null>(null);
  protected readonly pending = signal(false);
  protected readonly notice = signal<Notice | null>(null);

  protected readonly selected = computed(() => {
    const id = this.selectedId();
    return this.orders.hasValue() ? (this.orders.value().find((o) => o.id === id) ?? null) : null;
  });

  /** Allowed targets, in lifecycle order, with their display metadata. */
  protected readonly targets = computed(() => {
    const allowed = this.selected()?.allowedTransitions ?? [];
    return STATUSES.filter((meta) => allowed.includes(meta.status));
  });

  protected readonly isWorkFinished = isWorkFinished;
  protected readonly statusMeta = statusMeta;

  protected select(order: RepairOrder): void {
    this.selectedId.set(this.selectedId() === order.id ? null : order.id);
    this.notice.set(null);
  }

  protected clearSelection(): void {
    this.selectedId.set(null);
    this.notice.set(null);
  }

  protected move(target: RepairOrderStatus): void {
    const order = this.selected();
    if (!order || this.pending()) {
      return;
    }

    this.pending.set(true);
    this.api
      .changeStatus(order.id, target)
      .pipe(
        finalize(() => this.pending.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updated) => {
          this.orders.update((list) => list?.map((o) => (o.id === updated.id ? updated : o)));
          this.notice.set({
            kind: 'success',
            title: 'Status geändert',
            detail: `#${updated.id}: ${statusMeta(order.status).label} → ${statusMeta(updated.status).label}`,
          });
        },
        error: (error: unknown) => {
          const apiError = toApiError(error);
          this.notice.set({ kind: 'error', title: apiError.title, detail: apiError.detail });

          // The server knows better than this board: most likely someone else
          // changed the order meanwhile. Re-sync instead of guessing.
          if (apiError.status === 400 || apiError.status === 404 || apiError.status === 409) {
            this.orders.reload();
          }
        },
      });
  }
}

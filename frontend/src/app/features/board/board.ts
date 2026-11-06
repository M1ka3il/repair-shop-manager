import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { RepairOrdersApi } from '../../core/api/repair-orders.api';
import { toApiError } from '../../core/http/api-error';
import { isWorkFinished } from '../../core/status/repair-order-status';
import { groupByStatus } from './group-by-status';

/**
 * Start page: all repair orders as a board, one column per status.
 * Read-only in this step — moving cards (PATCH /status) comes next.
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

  protected readonly orders = rxResource({ stream: () => this.api.list() });

  protected readonly columns = computed(() =>
    groupByStatus(this.orders.hasValue() ? this.orders.value() : []),
  );

  protected readonly error = computed(() => {
    const error = this.orders.error();
    return error ? toApiError(error) : null;
  });

  protected readonly isWorkFinished = isWorkFinished;
}

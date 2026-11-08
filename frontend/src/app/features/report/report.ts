import { CurrencyPipe, DecimalPipe, PercentPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CustomersApi } from '../../core/api/customers.api';
import type { CustomerRevenue } from '../../core/api/models';
import { toApiError } from '../../core/http/api-error';
import { toCsv } from './csv';
import { DEFAULT_PRESET, PRESETS, type Period, isValidPeriod, toInstants } from './period';

type Part = 'labour' | 'parts';

interface Hovered {
  readonly row: CustomerRevenue;
  readonly part: Part;
}

/**
 * Revenue per customer over a period: totals, a stacked bar per customer
 * (labour | parts) and the same numbers as a table, exportable as CSV.
 *
 * The period lives in the URL (?from=&to=, yyyy-MM-dd), so a report can be
 * bookmarked or sent to someone. Without one, "this year" is shown.
 */
@Component({
  selector: 'app-report',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe, DecimalPipe, PercentPipe],
  templateUrl: './report.html',
  styleUrl: './report.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Report {
  /** Query params, bound via withComponentInputBinding. */
  readonly from = input<string>();
  readonly to = input<string>();

  private readonly api = inject(CustomersApi);
  private readonly router = inject(Router);

  protected readonly presets = PRESETS;

  /** The period being shown: the URL's, if valid, else the default preset. */
  protected readonly period = computed<Period>(() => {
    const fromUrl = { from: this.from() ?? '', to: this.to() ?? '' };
    return isValidPeriod(fromUrl)
      ? fromUrl
      : PRESETS.find((p) => p.id === DEFAULT_PRESET)!.period(new Date());
  });

  protected readonly report = rxResource({
    params: () => this.period(),
    stream: ({ params }) => {
      const { from, to } = toInstants(params);
      return this.api.revenueByCustomer(from, to);
    },
  });

  protected readonly rows = computed(() => (this.report.hasValue() ? this.report.value() : []));

  protected readonly error = computed(() => {
    const error = this.report.error();
    return error ? toApiError(error) : null;
  });

  protected readonly totals = computed(() => {
    const rows = this.rows();
    const labour = rows.reduce((sum, r) => sum + r.labourRevenue, 0);
    const parts = rows.reduce((sum, r) => sum + r.partsRevenue, 0);
    const total = rows.reduce((sum, r) => sum + r.totalRevenue, 0);
    return {
      labour,
      parts,
      total,
      orders: rows.reduce((sum, r) => sum + r.orderCount, 0),
      labourShare: total > 0 ? labour / total : 0,
      partsShare: total > 0 ? parts / total : 0,
    };
  });

  /** Bar geometry: the largest total spans the full track; segments are shares of it. */
  protected readonly bars = computed(() => {
    const rows = this.rows();
    const max = Math.max(0, ...rows.map((r) => r.totalRevenue));
    return rows.map((row) => ({
      row,
      labourPct: max > 0 ? (row.labourRevenue / max) * 100 : 0,
      partsPct: max > 0 ? (row.partsRevenue / max) * 100 : 0,
    }));
  });

  protected readonly hovered = signal<Hovered | null>(null);

  protected readonly form = new FormGroup({
    from: new FormControl('', { nonNullable: true }),
    to: new FormControl('', { nonNullable: true }),
  });

  protected readonly formInvalid = signal(false);

  constructor() {
    // The form always shows the period on screen, including the default.
    effect(() => {
      const { from, to } = this.period();
      this.form.setValue({ from, to }, { emitEvent: false });
      this.formInvalid.set(false);
    });
  }

  protected apply(): void {
    const period = this.form.getRawValue();
    if (!isValidPeriod(period)) {
      this.formInvalid.set(true);
      return;
    }
    this.show(period);
  }

  protected applyPreset(id: string): void {
    this.show(PRESETS.find((p) => p.id === id)!.period(new Date()));
  }

  protected isActivePreset(id: string): boolean {
    const period = PRESETS.find((p) => p.id === id)!.period(new Date());
    return period.from === this.period().from && period.to === this.period().to;
  }

  protected hover(row: CustomerRevenue, part: Part): void {
    this.hovered.set({ row, part });
  }

  protected share(row: CustomerRevenue, part: Part): number {
    const value = part === 'labour' ? row.labourRevenue : row.partsRevenue;
    return row.totalRevenue > 0 ? value / row.totalRevenue : 0;
  }

  protected downloadCsv(): void {
    const { from, to } = this.period();
    const blob = new Blob([toCsv(this.rows())], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `umsatz-nach-kunde_${from}_${to}.csv`;
    link.click();
    URL.revokeObjectURL(url);
  }

  private show(period: Period): void {
    void this.router.navigate([], { queryParams: { from: period.from, to: period.to } });
  }
}

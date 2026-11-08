import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  numberAttribute,
  signal,
  untracked,
} from '@angular/core';
import { rxResource, takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  type AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { type Observable, finalize } from 'rxjs';
import { CustomersApi } from '../../core/api/customers.api';
import { AuthService } from '../../core/auth/auth.service';
import type { RepairItemType, RepairOrder, RepairOrderStatus } from '../../core/api/models';
import { RepairOrdersApi } from '../../core/api/repair-orders.api';
import { toApiError } from '../../core/http/api-error';
import { STATUSES, isWorkFinished, statusMeta } from '../../core/status/repair-order-status';
import { decimalInRange, parseDecimal } from '../../shared/forms/decimal';
import { validationMessage } from '../../shared/forms/validation-message';
import { type Notice, NoticeView } from '../../shared/notice/notice';
import { actionLabel, lifecycleSteps } from './lifecycle';

/**
 * One repair order: lifecycle, description, line items, totals, vehicle and
 * customer. Every rule (allowed transitions, locking after completion) comes
 * from the API; this page only mirrors it so the user is not offered actions
 * the server would refuse — and still shows the server's message if it does.
 */
@Component({
  selector: 'app-order-detail',
  imports: [RouterLink, ReactiveFormsModule, CurrencyPipe, DatePipe, DecimalPipe, NoticeView],
  templateUrl: './order-detail.html',
  styleUrl: './order-detail.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderDetail {
  /** Route parameter `:id`, bound via withComponentInputBinding. */
  readonly id = input.required<number, unknown>({ transform: numberAttribute });

  private readonly api = inject(RepairOrdersApi);
  private readonly customersApi = inject(CustomersApi);
  private readonly router = inject(Router);
  /** Shapes the UI only; the API refuses office actions for mechanics anyway (403). */
  protected readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly order = rxResource({
    params: () => this.id(),
    stream: ({ params: id }) => this.api.get(id),
  });

  // Primitive params on purpose: an object would be a new value after every
  // order update and refetch vehicle and customer each time.
  protected readonly vehicle = rxResource({
    params: () => (this.order.hasValue() ? this.order.value().vehicleId : undefined),
    stream: ({ params: vehicleId }) => this.customersApi.getVehicle(vehicleId),
  });

  protected readonly customer = rxResource({
    params: () => (this.vehicle.hasValue() ? this.vehicle.value().customerId : undefined),
    stream: ({ params: customerId }) => this.customersApi.getCustomer(customerId),
  });

  protected readonly current = computed<RepairOrder | null>(() =>
    this.order.hasValue() ? this.order.value() : null,
  );

  protected readonly loadError = computed(() => {
    const error = this.order.error();
    return error ? toApiError(error) : null;
  });

  protected readonly steps = computed(() => {
    const order = this.current();
    return order ? lifecycleSteps(order.status) : [];
  });

  protected readonly actions = computed(() => {
    const order = this.current();
    if (!order) {
      return [];
    }
    return STATUSES.filter((meta) => order.allowedTransitions.includes(meta.status)).map(
      (meta) => ({ status: meta.status, label: actionLabel(order.status, meta.status) }),
    );
  });

  protected readonly finished = computed(() => {
    const order = this.current();
    return order ? isWorkFinished(order.status) : false;
  });

  protected readonly invoiced = computed(() => this.current()?.status === 'Invoiced');

  protected readonly labourTotal = computed(() => this.sumOf('Labour'));
  protected readonly partsTotal = computed(() => this.sumOf('Part'));

  protected readonly pending = signal(false);
  protected readonly notice = signal<Notice | null>(null);
  protected readonly confirmingDelete = signal(false);
  protected readonly itemSubmitted = signal(false);

  protected readonly description = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(3), Validators.maxLength(1000)],
  });

  /**
   * Wrapper so the form element gets a FormGroupDirective: that is what makes
   * (ngSubmit) an Angular event. A bare <form (ngSubmit)> with only a standalone
   * [formControl] inside is submitted natively by the browser — a full reload.
   */
  protected readonly descriptionForm = new FormGroup({ description: this.description });

  protected readonly itemForm = new FormGroup({
    type: new FormControl<RepairItemType>('Labour', { nonNullable: true }),
    designation: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(2), Validators.maxLength(200)],
    }),
    quantity: new FormControl('1', {
      nonNullable: true,
      validators: decimalInRange(0.01, 100_000),
    }),
    unitPrice: new FormControl('', { nonNullable: true, validators: decimalInRange(0, 1_000_000) }),
  });

  protected readonly itemType = toSignal(this.itemForm.controls.type.valueChanges, {
    initialValue: this.itemForm.controls.type.value,
  });

  protected readonly statusMeta = statusMeta;

  constructor() {
    // Keep the forms in step with the order: fill the description unless the
    // user is editing it, and lock what the domain locks.
    effect(() => {
      const order = this.current();
      if (!order) {
        return;
      }
      untracked(() => {
        if (!this.description.dirty) {
          this.description.setValue(order.description);
        }
        // Editing the description and deleting are office actions (PUT/DELETE).
        setEnabled(this.description, order.status !== 'Invoiced' && this.auth.isOffice());
        setEnabled(this.itemForm, !isWorkFinished(order.status));
      });
    });
  }

  protected changeStatus(target: RepairOrderStatus): void {
    const order = this.current();
    if (!order) {
      return;
    }
    this.run(this.api.changeStatus(order.id, target), (updated) => {
      this.order.set(updated);
      this.notice.set({
        kind: 'success',
        title: 'Status geändert',
        detail: `${statusMeta(order.status).label} → ${statusMeta(updated.status).label}`,
      });
    });
  }

  protected saveDescription(): void {
    if (this.description.invalid) {
      this.description.markAsTouched();
      return;
    }
    this.run(
      this.api.update(this.id(), { description: this.description.value.trim() }),
      (updated) => {
        this.order.set(updated);
        this.description.reset(updated.description);
        this.notice.set({ kind: 'success', title: 'Beschreibung gespeichert', detail: '' });
      },
    );
  }

  protected addItem(): void {
    this.itemSubmitted.set(true);
    if (this.itemForm.invalid) {
      this.itemForm.markAllAsTouched();
      return;
    }
    const value = this.itemForm.getRawValue();
    const request = {
      type: value.type,
      designation: value.designation.trim(),
      quantity: parseDecimal(value.quantity)!,
      unitPrice: parseDecimal(value.unitPrice)!,
    };
    this.run(this.api.addItem(this.id(), request), (updated) => {
      this.order.set(updated);
      this.itemForm.reset({ type: value.type, designation: '', quantity: '1', unitPrice: '' });
      this.itemSubmitted.set(false);
      this.notice.set({
        kind: 'success',
        title: 'Position hinzugefügt',
        detail: request.designation,
      });
    });
  }

  protected removeItem(itemId: number, designation: string): void {
    this.run(this.api.removeItem(this.id(), itemId), (updated) => {
      this.order.set(updated);
      this.notice.set({ kind: 'success', title: 'Position entfernt', detail: designation });
    });
  }

  protected deleteOrder(): void {
    this.run(this.api.delete(this.id()), () => {
      void this.router.navigate(['/board']);
    });
  }

  /** Validation message for a control, once the user has had a chance to fill it in. */
  protected errorFor(control: AbstractControl, min = '', max = ''): string | null {
    return validationMessage(control, this.itemSubmitted(), { min, max });
  }

  /**
   * One place for every write: one request at a time, feedback either way, and
   * a reload when the server says the page is out of date (400/404/409 — most
   * likely someone else changed the order).
   */
  private run<T>(request: Observable<T>, onSuccess: (value: T) => void): void {
    if (this.pending()) {
      return;
    }
    this.pending.set(true);
    this.notice.set(null);
    request
      .pipe(
        finalize(() => this.pending.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: onSuccess,
        error: (error: unknown) => {
          const apiError = toApiError(error);
          this.confirmingDelete.set(false);
          this.notice.set({ kind: 'error', title: apiError.title, detail: apiError.detail });
          if ([400, 404, 409].includes(apiError.status)) {
            this.order.reload();
          }
        },
      });
  }

  private sumOf(type: RepairItemType): number {
    return (this.current()?.items ?? [])
      .filter((item) => item.type === type)
      .reduce((sum, item) => sum + item.lineTotal, 0);
  }
}

function setEnabled(control: AbstractControl, enabled: boolean): void {
  if (enabled && control.disabled) {
    control.enable({ emitEvent: false });
  } else if (!enabled && control.enabled) {
    control.disable({ emitEvent: false });
  }
}

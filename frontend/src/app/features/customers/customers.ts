import { NgTemplateOutlet } from '@angular/common';
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
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type AbstractControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { type Observable, finalize } from 'rxjs';
import { CustomersApi } from '../../core/api/customers.api';
import type { Customer, Vehicle } from '../../core/api/models';
import { toApiError } from '../../core/http/api-error';
import {
  customerControls,
  toCustomerRequest,
  toVehicleRequest,
  vehicleControls,
} from '../../shared/forms/master-data-forms';
import { validationMessage } from '../../shared/forms/validation-message';
import { type Notice, NoticeView } from '../../shared/notice/notice';

/** `:id` from the route: a customer id, `'new'`, or nothing (list only). */
type CustomerKey = number | 'new' | undefined;

function toCustomerKey(value: unknown): CustomerKey {
  if (value === 'new') return 'new';
  if (value === undefined || value === null || value === '') return undefined;
  return numberAttribute(value);
}

/**
 * Master data: customers on the left, the selected one on the right with their
 * contact details and vehicles. The selection lives in the URL
 * (/customers/:id, /customers/new), so it survives reloads and can be linked.
 *
 * Create and correct only — no delete, by design (see the README): a vehicle
 * with repair history must not vanish.
 */
@Component({
  selector: 'app-customers',
  imports: [RouterLink, ReactiveFormsModule, NgTemplateOutlet, NoticeView],
  templateUrl: './customers.html',
  styleUrl: './customers.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Customers {
  readonly id = input<CustomerKey, unknown>(undefined, { transform: toCustomerKey });

  private readonly api = inject(CustomersApi);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly data = rxResource({ stream: () => this.api.listCustomers() });

  protected readonly loadError = computed(() => {
    const error = this.data.error();
    return error ? toApiError(error) : null;
  });

  protected readonly query = signal('');

  protected readonly filtered = computed(() => {
    const customers = this.data.hasValue() ? this.data.value() : [];
    const needle = normalise(this.query());
    return needle
      ? customers.filter(
          (c) =>
            normalise(c.name).includes(needle) ||
            c.vehicles.some((v) => normalise(v.licensePlate).includes(needle)),
        )
      : customers;
  });

  protected readonly creating = computed(() => this.id() === 'new');

  protected readonly selected = computed<Customer | null>(() => {
    const id = this.id();
    if (typeof id !== 'number' || !this.data.hasValue()) {
      return null;
    }
    return this.data.value().find((c) => c.id === id) ?? null;
  });

  protected readonly notFound = computed(
    () => typeof this.id() === 'number' && this.data.hasValue() && this.selected() === null,
  );

  protected readonly pending = signal(false);
  protected readonly notice = signal<Notice | null>(null);
  protected readonly customerSubmitted = signal(false);
  protected readonly newVehicleSubmitted = signal(false);
  protected readonly editSubmitted = signal(false);
  protected readonly newVehicleConflict = signal<string | null>(null);
  protected readonly newVehicleError = signal<string | null>(null);
  protected readonly editConflict = signal<string | null>(null);
  protected readonly editError = signal<string | null>(null);
  protected readonly editingVehicleId = signal<number | null>(null);

  protected readonly customerForm = new FormGroup(customerControls());
  protected readonly newVehicleForm = new FormGroup(vehicleControls());
  protected readonly editVehicleForm = new FormGroup(vehicleControls());

  /**
   * Fill the form when a different customer is opened — and only then. A list
   * reload after adding a vehicle yields a new object for the same customer and
   * must not overwrite contact details the user is still typing.
   */
  private readonly formSource = computed(() => ({ key: this.id(), customer: this.selected() }), {
    equal: (a, b) => a.key === b.key && (a.customer === null) === (b.customer === null),
  });

  constructor() {
    effect(() => {
      const { key, customer } = this.formSource();
      untracked(() => {
        this.customerSubmitted.set(false);
        this.customerForm.reset(
          customer
            ? { name: customer.name, email: customer.email ?? '', phone: customer.phone ?? '' }
            : undefined,
        );
        if (key !== undefined) {
          this.resetVehicleForms();
        }
      });
    });
  }

  protected saveCustomer(): void {
    this.customerSubmitted.set(true);
    if (this.customerForm.invalid) {
      this.customerForm.markAllAsTouched();
      return;
    }
    const request = toCustomerRequest(this.customerForm.getRawValue());
    const id = this.id();

    if (id === 'new') {
      this.run(this.api.createCustomer(request), (created) => {
        this.data.reload();
        this.notice.set({ kind: 'success', title: 'Kunde angelegt', detail: created.name });
        void this.router.navigate(['/customers', created.id]);
      });
    } else if (typeof id === 'number') {
      this.run(this.api.updateCustomer(id, request), (updated) => {
        this.data.reload();
        this.customerForm.reset({
          name: updated.name,
          email: updated.email ?? '',
          phone: updated.phone ?? '',
        });
        this.notice.set({ kind: 'success', title: 'Kunde gespeichert', detail: updated.name });
      });
    }
  }

  protected addVehicle(): void {
    const customer = this.selected();
    this.newVehicleSubmitted.set(true);
    if (!customer || this.newVehicleForm.invalid) {
      this.newVehicleForm.markAllAsTouched();
      return;
    }
    this.newVehicleConflict.set(null);
    this.newVehicleError.set(null);
    this.run(
      this.api.addVehicle(customer.id, toVehicleRequest(this.newVehicleForm.getRawValue())),
      (vehicle) => {
        this.data.reload();
        this.newVehicleForm.reset();
        this.newVehicleSubmitted.set(false);
        this.notice.set({ kind: 'success', title: 'Fahrzeug angelegt', detail: describe(vehicle) });
      },
      (status, detail) =>
        status === 409 ? this.newVehicleConflict.set(detail) : this.newVehicleError.set(detail),
    );
  }

  protected startEdit(vehicle: Vehicle): void {
    this.editingVehicleId.set(vehicle.id);
    this.editSubmitted.set(false);
    this.editConflict.set(null);
    this.editError.set(null);
    this.editVehicleForm.reset({
      licensePlate: vehicle.licensePlate,
      make: vehicle.make,
      model: vehicle.model,
      year: String(vehicle.year),
    });
  }

  protected cancelEdit(): void {
    this.editingVehicleId.set(null);
  }

  protected saveVehicle(): void {
    const vehicleId = this.editingVehicleId();
    this.editSubmitted.set(true);
    if (vehicleId === null || this.editVehicleForm.invalid) {
      this.editVehicleForm.markAllAsTouched();
      return;
    }
    this.editConflict.set(null);
    this.editError.set(null);
    this.run(
      this.api.updateVehicle(vehicleId, toVehicleRequest(this.editVehicleForm.getRawValue())),
      (vehicle) => {
        this.data.reload();
        this.editingVehicleId.set(null);
        this.notice.set({
          kind: 'success',
          title: 'Fahrzeug gespeichert',
          detail: describe(vehicle),
        });
      },
      (status, detail) =>
        status === 409 ? this.editConflict.set(detail) : this.editError.set(detail),
    );
  }

  protected errorFor(control: AbstractControl, submitted: boolean): string | null {
    return validationMessage(control, submitted);
  }

  /**
   * One request at a time, feedback either way. Field-level rejections (400 on
   * a vehicle, 409 for a taken plate) go to `onRejected` so they can be shown
   * next to the form; everything else becomes a notice.
   */
  private run<T>(
    request: Observable<T>,
    onSuccess: (value: T) => void,
    onRejected?: (status: number, detail: string) => void,
  ): void {
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
          if (onRejected && (apiError.status === 400 || apiError.status === 409)) {
            onRejected(apiError.status, apiError.detail || apiError.title);
          } else {
            this.notice.set({ kind: 'error', title: apiError.title, detail: apiError.detail });
          }
        },
      });
  }

  private resetVehicleForms(): void {
    this.editingVehicleId.set(null);
    this.newVehicleForm.reset();
    this.newVehicleSubmitted.set(false);
    this.newVehicleConflict.set(null);
    this.newVehicleError.set(null);
  }
}

function normalise(value: string): string {
  return value.toLowerCase().replace(/[\s-]/g, '');
}

function describe(vehicle: Vehicle): string {
  return `${vehicle.licensePlate} · ${vehicle.make} ${vehicle.model}`;
}

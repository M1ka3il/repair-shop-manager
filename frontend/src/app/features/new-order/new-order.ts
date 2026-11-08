import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  linkedSignal,
  numberAttribute,
  signal,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  type AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, forkJoin, map, of, switchMap, tap } from 'rxjs';
import { CustomersApi } from '../../core/api/customers.api';
import { RepairOrdersApi } from '../../core/api/repair-orders.api';
import { toApiError } from '../../core/http/api-error';
import {
  customerControls,
  toCustomerRequest,
  toVehicleRequest,
  vehicleControls,
} from '../../shared/forms/master-data-forms';
import { validationMessage } from '../../shared/forms/validation-message';
import { type Notice, NoticeView } from '../../shared/notice/notice';
import { searchVehicles, toVehicleOptions } from './vehicle-search';

/** How many matches are listed before the user is asked to narrow the search. */
const MAX_RESULTS = 10;

/**
 * Opening a repair order at the counter: find the vehicle (or register it, with
 * a new customer if needed), describe the problem, done — the order opens in
 * its detail view.
 *
 * Registering is a separate step on purpose. Done in one go with the order, a
 * plate conflict (409) after the customer was already created would leave the
 * user retrying into a duplicate customer. Here, a customer created by a failed
 * attempt is remembered and reused.
 */
@Component({
  selector: 'app-new-order',
  imports: [RouterLink, ReactiveFormsModule, NoticeView],
  templateUrl: './new-order.html',
  styleUrl: './new-order.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewOrder {
  /** Optional `?vehicleId=` to preselect, e.g. coming from the customer page. */
  readonly vehicleId = input<number | undefined, unknown>(undefined, {
    transform: (value: unknown) =>
      value === undefined || value === null || value === '' ? undefined : numberAttribute(value),
  });

  private readonly customersApi = inject(CustomersApi);
  private readonly ordersApi = inject(RepairOrdersApi);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly data = rxResource({
    stream: () =>
      forkJoin({
        vehicles: this.customersApi.listVehicles(),
        customers: this.customersApi.listCustomers(),
      }),
  });

  protected readonly customers = computed(() =>
    this.data.hasValue() ? this.data.value().customers : [],
  );

  private readonly options = computed(() =>
    this.data.hasValue()
      ? toVehicleOptions(this.data.value().vehicles, this.data.value().customers)
      : [],
  );

  protected readonly loadError = computed(() => {
    const error = this.data.error();
    return error ? toApiError(error) : null;
  });

  protected readonly query = signal('');
  protected readonly matches = computed(() => searchVehicles(this.options(), this.query()));
  protected readonly visibleMatches = computed(() => this.matches().slice(0, MAX_RESULTS));
  protected readonly hiddenMatches = computed(() =>
    Math.max(0, this.matches().length - MAX_RESULTS),
  );

  /** Follows `?vehicleId=` until the user picks something else. */
  protected readonly selectedId = linkedSignal<number | null>(() => this.vehicleId() ?? null);

  protected readonly selected = computed(
    () => this.options().find((o) => o.vehicle.id === this.selectedId()) ?? null,
  );

  protected readonly newVehicleOpen = signal(false);
  protected readonly pending = signal(false);
  protected readonly notice = signal<Notice | null>(null);
  protected readonly vehicleSubmitted = signal(false);
  protected readonly orderSubmitted = signal(false);
  /** 409 from the server, shown at the plate field. */
  protected readonly plateConflict = signal<string | null>(null);
  /** Any other rejection of the vehicle (e.g. malformed plate, year out of range). */
  protected readonly vehicleError = signal<string | null>(null);

  protected readonly vehicleForm = new FormGroup({
    customer: new FormControl<number | 'new'>('new', { nonNullable: true }),
    ...customerControls(),
    ...vehicleControls(),
  });

  protected readonly orderForm = new FormGroup({
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(3), Validators.maxLength(1000)],
    }),
  });

  constructor() {
    // Customer fields only count when a new customer is being created.
    this.vehicleForm.controls.customer.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((customer) => this.toggleCustomerFields(customer === 'new'));
  }

  protected select(id: number): void {
    this.selectedId.set(id);
    this.notice.set(null);
  }

  protected openNewVehicle(): void {
    this.newVehicleOpen.set(true);
    this.selectedId.set(null);
  }

  protected closeNewVehicle(): void {
    this.newVehicleOpen.set(false);
    this.vehicleError.set(null);
    this.plateConflict.set(null);
  }

  protected registerVehicle(): void {
    this.vehicleSubmitted.set(true);
    if (this.vehicleForm.invalid || this.pending()) {
      this.vehicleForm.markAllAsTouched();
      return;
    }

    const value = this.vehicleForm.getRawValue();
    const vehicleRequest = toVehicleRequest(value);

    const customerId$ =
      value.customer === 'new'
        ? this.customersApi.createCustomer(toCustomerRequest(value)).pipe(
            // Remember the customer right away: if the vehicle is rejected,
            // the retry must not create them a second time.
            tap((customer) => {
              this.vehicleForm.controls.customer.setValue(customer.id);
              this.data.reload();
            }),
            map((customer) => customer.id),
          )
        : of(value.customer);

    this.pending.set(true);
    this.vehicleError.set(null);
    this.plateConflict.set(null);
    customerId$
      .pipe(
        switchMap((customerId) => this.customersApi.addVehicle(customerId, vehicleRequest)),
        finalize(() => this.pending.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (vehicle) => {
          this.data.reload();
          this.query.set(vehicle.licensePlate);
          this.selectedId.set(vehicle.id);
          this.newVehicleOpen.set(false);
          this.vehicleSubmitted.set(false);
          this.vehicleForm.reset();
          this.toggleCustomerFields(true);
          this.notice.set({
            kind: 'success',
            title: 'Fahrzeug angelegt',
            detail: `${vehicle.licensePlate} · ${vehicle.make} ${vehicle.model}`,
          });
        },
        error: (error: unknown) => {
          const apiError = toApiError(error);
          if (apiError.status === 409) {
            this.plateConflict.set(apiError.detail);
          } else {
            this.vehicleError.set(apiError.detail || apiError.title);
          }
        },
      });
  }

  protected createOrder(): void {
    this.orderSubmitted.set(true);
    const vehicleId = this.selectedId();
    if (vehicleId === null || this.orderForm.invalid || this.pending()) {
      this.orderForm.markAllAsTouched();
      return;
    }

    this.pending.set(true);
    this.notice.set(null);
    this.ordersApi
      .create({ vehicleId, description: this.orderForm.controls.description.value.trim() })
      .pipe(
        finalize(() => this.pending.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (order) => void this.router.navigate(['/orders', order.id]),
        error: (error: unknown) => {
          const apiError = toApiError(error);
          this.notice.set({ kind: 'error', title: apiError.title, detail: apiError.detail });
        },
      });
  }

  protected onPlateInput(): void {
    this.plateConflict.set(null);
  }

  protected errorFor(control: AbstractControl, submitted: boolean): string | null {
    return validationMessage(control, submitted);
  }

  private toggleCustomerFields(enabled: boolean): void {
    for (const name of ['name', 'email', 'phone'] as const) {
      const control = this.vehicleForm.controls[name];
      if (enabled) {
        control.enable({ emitEvent: false });
      } else {
        control.disable({ emitEvent: false });
      }
    }
  }
}

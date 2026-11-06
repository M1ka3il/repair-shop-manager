import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import localeDe from '@angular/common/locales/de';
import { LOCALE_ID } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import type { RepairOrder } from '../../core/api/models';
import { apiErrorInterceptor } from '../../core/http/api-error.interceptor';
import { OrderDetail } from './order-detail';

const order = (overrides: Partial<RepairOrder> = {}): RepairOrder => ({
  id: 7,
  vehicleId: 1,
  licensePlate: 'M-SD 4711',
  customerName: 'Stefan Dorn',
  status: 'InProgress',
  receivedAtUtc: '2026-11-05T08:00:00Z',
  completedAtUtc: null,
  invoicedAtUtc: null,
  description: 'Kupplung rutscht',
  totalPrice: 534,
  items: [
    {
      id: 1,
      type: 'Labour',
      designation: 'Kupplung wechseln',
      unitPrice: 89,
      quantity: 2,
      lineTotal: 178,
    },
    {
      id: 2,
      type: 'Part',
      designation: 'Kupplungssatz',
      unitPrice: 356,
      quantity: 1,
      lineTotal: 356,
    },
  ],
  allowedTransitions: ['WaitingForParts', 'Completed'],
  ...overrides,
});

describe('OrderDetail', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<OrderDetail>;
  let el: HTMLElement;

  beforeEach(() => {
    registerLocaleData(localeDe);
    TestBed.configureTestingModule({
      imports: [OrderDetail],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([apiErrorInterceptor])),
        provideHttpClientTesting(),
        { provide: LOCALE_ID, useValue: 'de-DE' },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(OrderDetail);
    fixture.componentRef.setInput('id', '7'); // route params arrive as strings
    el = fixture.nativeElement as HTMLElement;
    TestBed.tick();
  });

  afterEach(() => http.verify());

  /**
   * Lets dependent resources start their requests: a resource subscribes to its
   * stream asynchronously, so a follow-up request appears only after a macrotask
   * plus an effect run. whenStable() cannot be used here — it would wait for
   * exactly the request the test still has to answer.
   */
  async function settle(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  /** Answers the order request and the vehicle/customer lookups it triggers. */
  async function load(value: RepairOrder = order()): Promise<void> {
    http.expectOne('/api/repair-orders/7').flush(value);
    await settle();
    http.expectOne('/api/vehicles/1').flush({
      id: 1,
      customerId: 3,
      licensePlate: 'M-SD 4711',
      make: 'Opel',
      model: 'Corsa F',
      year: 2021,
    });
    await settle();
    http.expectOne('/api/customers/3').flush({
      id: 3,
      name: 'Stefan Dorn',
      email: null,
      phone: '+49 89 5551 234',
      vehicles: [],
    });
    await fixture.whenStable();
  }

  async function click(element: Element | null | undefined): Promise<void> {
    (element as HTMLElement).click();
    await fixture.whenStable();
  }

  function type(selector: string, value: string): void {
    const input = el.querySelector(selector) as HTMLInputElement | HTMLSelectElement;
    input.value = value;
    input.dispatchEvent(new Event(input instanceof HTMLSelectElement ? 'change' : 'input'));
  }

  const buttons = (selector: string) =>
    Array.from(el.querySelectorAll(selector)).map((b) => b.textContent?.trim());
  const button = (text: string) =>
    Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.trim() === text);

  it('shows header, allowed actions, items, totals, vehicle and customer', async () => {
    await load();

    expect(el.querySelector('h1')?.textContent).toContain('Auftrag #7');
    expect(buttons('.head-actions button')).toEqual(['Wartet auf Teil', 'Fertig melden']);
    expect(el.querySelectorAll('tbody tr')).toHaveLength(2);
    expect(el.querySelector('.sum')?.textContent).toContain('178,00');
    expect(el.querySelector('dd.total')?.textContent).toContain('534,00');
    expect(el.querySelector('.facts')?.textContent).toContain('Opel Corsa F · 2021');
    expect(el.querySelector('.facts')?.textContent).toContain('+49 89 5551 234 · –');
  });

  it('changes the status and locks the order once work is finished', async () => {
    await load();

    await click(button('Fertig melden'));
    const request = http.expectOne('/api/repair-orders/7/status');
    expect(request.request.body).toEqual({ targetStatus: 'Completed' });
    request.flush(order({ status: 'Completed', allowedTransitions: ['Invoiced'] }));
    await fixture.whenStable();

    expect(buttons('.head-actions button')).toEqual(['Abrechnen']);
    expect(el.querySelector('.item-form')).toBeNull();
    expect(button('Auftrag löschen')).toBeUndefined();
    expect(el.querySelector('.locked')?.textContent).toContain('Arbeit abgeschlossen');
    expect(el.querySelector('[role="status"]')?.textContent).toContain('In Arbeit → Fertig');
  });

  it('books an item with German decimals converted to numbers', async () => {
    await load();

    type('select[formControlName="type"]', 'Part');
    type('input[formControlName="designation"]', '  Ausrücklager ');
    type('input[formControlName="quantity"]', '1');
    type('input[formControlName="unitPrice"]', '74,90');
    await click(button('Hinzufügen'));

    const request = http.expectOne('/api/repair-orders/7/items');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      type: 'Part',
      designation: 'Ausrücklager',
      quantity: 1,
      unitPrice: 74.9,
    });
    request.flush(order({ totalPrice: 608.9 }));
    await fixture.whenStable();

    expect(
      (el.querySelector('input[formControlName="designation"]') as HTMLInputElement).value,
    ).toBe('');
    expect(el.querySelector('[role="status"]')?.textContent).toContain('Position hinzugefügt');
  });

  it('does not send an invalid item and says what is wrong', async () => {
    await load();

    type('input[formControlName="unitPrice"]', '1.234,50');
    await click(button('Hinzufügen'));

    const messages = buttons('.item-form .field-error');
    expect(messages).toContain('Pflichtfeld.');
    expect(messages).toContain('Zahl mit höchstens 2 Nachkommastellen, z. B. 89,50.');
    http.expectNone('/api/repair-orders/7/items');
  });

  it('saves the description through Angular, without a native form submit', async () => {
    await load();
    const form = el.querySelector('.desc-form') as HTMLFormElement;
    const submit = new Event('submit', { cancelable: true });

    type('#description', 'Kupplung rutscht, Ausrücklager geräuschvoll');
    await fixture.whenStable();
    form.dispatchEvent(submit);
    await fixture.whenStable();

    // A plain <form (ngSubmit)> would let the browser submit and reload the page.
    expect(submit.defaultPrevented).toBe(true);
    const request = http.expectOne('/api/repair-orders/7');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      description: 'Kupplung rutscht, Ausrücklager geräuschvoll',
    });
    request.flush(order({ description: 'Kupplung rutscht, Ausrücklager geräuschvoll' }));
    await fixture.whenStable();
    expect(el.querySelector('[role="status"]')?.textContent).toContain('Beschreibung gespeichert');
  });

  it('removes an item', async () => {
    await load();

    await click(el.querySelector('[aria-label="Position Kupplungssatz entfernen"]'));

    const request = http.expectOne('/api/repair-orders/7/items/2');
    expect(request.request.method).toBe('DELETE');
    request.flush(order({ items: [order().items[0]], totalPrice: 178 }));
    await fixture.whenStable();
    expect(el.querySelectorAll('tbody tr')).toHaveLength(1);
  });

  it('asks before deleting, then returns to the board', async () => {
    await load();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    await click(button('Auftrag löschen'));
    http.expectNone('/api/repair-orders/7');
    await click(button('Ja, löschen'));

    const request = http.expectOne('/api/repair-orders/7');
    expect(request.request.method).toBe('DELETE');
    request.flush(null, { status: 204, statusText: 'No Content' });
    await fixture.whenStable();
    expect(navigate).toHaveBeenCalledWith(['/board']);
  });

  it('shows the server message on a rejected change and reloads the order', async () => {
    await load();

    await click(button('Fertig melden'));
    http.expectOne('/api/repair-orders/7/status').flush(
      {
        title: 'Business rule violated',
        detail: "Cannot change status from 'Invoiced' to 'Completed'.",
      },
      { status: 400, statusText: 'Bad Request' },
    );
    TestBed.tick();
    http
      .expectOne('/api/repair-orders/7')
      .flush(order({ status: 'Invoiced', allowedTransitions: [] }));
    await fixture.whenStable();

    expect(el.querySelector('.notice[role="alert"]')?.textContent).toContain(
      "Cannot change status from 'Invoiced'",
    );
    expect(buttons('.head-actions button')).toEqual([]);
    expect((el.querySelector('#description') as HTMLTextAreaElement).disabled).toBe(true);
  });

  it('shows when an invoiced order was completed and billed', async () => {
    await load(
      order({
        status: 'Invoiced',
        allowedTransitions: [],
        completedAtUtc: '2026-11-03T15:00:00Z',
        invoicedAtUtc: '2026-11-06T09:30:00Z',
      }),
    );

    // Displayed in the browser's zone; the assertion builds the same local value.
    const expected = new Date('2026-11-06T09:30:00Z');
    const pad = (n: number) => String(n).padStart(2, '0');
    expect(el.querySelector('.facts')?.textContent).toContain(
      `Abgerechnet ${pad(expected.getDate())}.${pad(expected.getMonth() + 1)}.${expected.getFullYear()}, ${pad(expected.getHours())}:${pad(expected.getMinutes())}`,
    );
  });

  it('shows a not-found page for an unknown order', async () => {
    http
      .expectOne('/api/repair-orders/7')
      .flush({ title: 'Resource not found' }, { status: 404, statusText: 'Not Found' });
    await fixture.whenStable();

    expect(el.querySelector('h1')?.textContent).toContain('Auftrag #7 nicht gefunden');
  });
});

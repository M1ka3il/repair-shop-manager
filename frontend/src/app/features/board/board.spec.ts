import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { registerLocaleData } from '@angular/common';
import localeDe from '@angular/common/locales/de';
import { LOCALE_ID } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import type { RepairOrder } from '../../core/api/models';
import { apiErrorInterceptor } from '../../core/http/api-error.interceptor';
import { Board } from './board';

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
  items: [],
  allowedTransitions: ['WaitingForParts', 'Completed'],
  ...overrides,
});

describe('Board', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<Board>;
  let el: HTMLElement;

  beforeEach(() => {
    registerLocaleData(localeDe);
    TestBed.configureTestingModule({
      imports: [Board],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([apiErrorInterceptor])),
        provideHttpClientTesting(),
        { provide: LOCALE_ID, useValue: 'de-DE' },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Board);
    el = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  async function load(orders: RepairOrder[]): Promise<void> {
    http.expectOne('/api/repair-orders').flush(orders);
    await fixture.whenStable();
  }

  async function click(element: Element | null | undefined): Promise<void> {
    (element as HTMLElement).click();
    await fixture.whenStable();
  }

  const column = (status: string) => el.querySelector(`[aria-labelledby="col-${status}"]`)!;
  const targetLabels = () =>
    Array.from(el.querySelectorAll('.action-bar .btn-target')).map((b) => b.textContent?.trim());

  it('renders each order as a card in its status column', async () => {
    await load([order()]);

    const inProgress = column('InProgress');
    expect(inProgress.querySelector('.plate')?.textContent).toBe('M-SD 4711');
    expect(inProgress.querySelector('.card-foot strong')?.textContent).toContain('534,00');
    expect(inProgress.querySelector('.card-foot a')?.getAttribute('href')).toBe('/orders/7');
    expect(el.querySelectorAll('section.column')).toHaveLength(5);
  });

  it('shows the API error and offers a retry', async () => {
    http
      .expectOne('/api/repair-orders')
      .flush(
        { title: 'Unexpected error', detail: 'See the server log.' },
        { status: 500, statusText: 'Server Error' },
      );
    await fixture.whenStable();

    const alert = el.querySelector('[role="alert"]');
    expect(alert?.textContent).toContain('Unexpected error');
    expect(alert?.querySelector('button')?.textContent).toContain('Erneut versuchen');
  });

  it('offers exactly the transitions the API allows, in lifecycle order', async () => {
    await load([order()]);

    await click(el.querySelector('.card-select'));

    expect(targetLabels()).toEqual(['Wartet auf Teil', 'Fertig']);
  });

  it('offers no move for a terminal order', async () => {
    await load([order({ status: 'Invoiced', allowedTransitions: [] })]);

    await click(el.querySelector('.card-select'));

    expect(targetLabels()).toEqual([]);
    expect(el.querySelector('.action-bar')?.textContent).toContain('Endstatus');
  });

  it('moves the card after a successful PATCH and confirms it', async () => {
    await load([order()]);
    await click(el.querySelector('.card-select'));

    await click(el.querySelectorAll('.action-bar .btn-target')[1]); // "Fertig"

    const request = http.expectOne('/api/repair-orders/7/status');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ targetStatus: 'Completed' });
    request.flush(order({ status: 'Completed', allowedTransitions: ['Invoiced'] }));
    await fixture.whenStable();

    expect(column('Completed').querySelector('.plate')?.textContent).toBe('M-SD 4711');
    expect(column('InProgress').querySelector('.card')).toBeNull();
    expect(el.querySelector('[role="status"]')?.textContent).toContain('In Arbeit → Fertig');
    expect(targetLabels()).toEqual(['Abgerechnet']);
  });

  it('shows the domain message on a rejected move and re-syncs the board', async () => {
    await load([order()]);
    await click(el.querySelector('.card-select'));
    await click(el.querySelectorAll('.action-bar .btn-target')[1]);

    http.expectOne('/api/repair-orders/7/status').flush(
      {
        title: 'Business rule violated',
        detail: "Cannot change status from 'Invoiced' to 'Completed'.",
      },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();

    // Someone else moved it meanwhile: the board reloads instead of guessing.
    await load([order({ status: 'Invoiced', allowedTransitions: [] })]);

    expect(el.querySelector('.notice[role="alert"]')?.textContent).toContain(
      "Cannot change status from 'Invoiced' to 'Completed'.",
    );
    expect(column('Invoiced').querySelector('.plate')?.textContent).toBe('M-SD 4711');
    expect(targetLabels()).toEqual([]);
  });
});

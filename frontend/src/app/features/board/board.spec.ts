import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { registerLocaleData } from '@angular/common';
import localeDe from '@angular/common/locales/de';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { apiErrorInterceptor } from '../../core/http/api-error.interceptor';
import { Board } from './board';

describe('Board', () => {
  let http: HttpTestingController;

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
  });

  afterEach(() => http.verify());

  it('renders each order as a card in its status column', async () => {
    const fixture = TestBed.createComponent(Board);
    fixture.detectChanges();

    http.expectOne('/api/repair-orders').flush([
      {
        id: 7,
        vehicleId: 1,
        licensePlate: 'M-SD 4711',
        customerName: 'Stefan Dorn',
        status: 'InProgress',
        receivedAtUtc: '2026-11-05T08:00:00Z',
        completedAtUtc: null,
        description: 'Kupplung rutscht',
        totalPrice: 534,
        items: [],
      },
    ]);
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    const column = el.querySelector('[aria-labelledby="col-InProgress"]')!;
    expect(column.querySelector('.plate')?.textContent).toBe('M-SD 4711');
    expect(column.querySelector('.card-foot strong')?.textContent).toContain('534,00');
    expect(column.querySelector('a.card')?.getAttribute('href')).toBe('/orders/7');
    expect(el.querySelectorAll('section.column')).toHaveLength(5);
  });

  it('shows the API error and offers a retry', async () => {
    const fixture = TestBed.createComponent(Board);
    fixture.detectChanges();

    http
      .expectOne('/api/repair-orders')
      .flush(
        { title: 'Unexpected error', detail: 'See the server log.' },
        { status: 500, statusText: 'Server Error' },
      );
    await fixture.whenStable();

    const alert = (fixture.nativeElement as HTMLElement).querySelector('[role="alert"]');
    expect(alert?.textContent).toContain('Unexpected error');
    expect(alert?.querySelector('button')?.textContent).toContain('Erneut versuchen');
  });
});

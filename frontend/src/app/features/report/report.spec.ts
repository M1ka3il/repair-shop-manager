import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import localeDe from '@angular/common/locales/de';
import { LOCALE_ID } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import type { CustomerRevenue } from '../../core/api/models';
import { apiErrorInterceptor } from '../../core/http/api-error.interceptor';
import { Report } from './report';

const ROWS: CustomerRevenue[] = [
  {
    customerId: 2,
    customerName: 'Kfz Lorenz GmbH',
    orderCount: 1,
    labourRevenue: 445,
    partsRevenue: 367.5,
    totalRevenue: 812.5,
  },
  {
    customerId: 1,
    customerName: 'Anna Becker',
    orderCount: 1,
    labourRevenue: 44.5,
    partsRevenue: 85.4,
    totalRevenue: 129.9,
  },
];

describe('Report', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<Report>;
  let el: HTMLElement;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    registerLocaleData(localeDe);
    TestBed.configureTestingModule({
      imports: [Report],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([apiErrorInterceptor])),
        provideHttpClientTesting(),
        { provide: LOCALE_ID, useValue: 'de-DE' },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(Report);
    el = fixture.nativeElement as HTMLElement;
  });

  afterEach(() => http.verify());

  async function settle(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  /** Opens the page for a period from the URL and answers the report request. */
  async function open(from: string | undefined, to: string | undefined, rows = ROWS) {
    if (from) fixture.componentRef.setInput('from', from);
    if (to) fixture.componentRef.setInput('to', to);
    TestBed.tick();
    await settle();
    const request = http.expectOne((req) => req.url === '/api/reports/revenue-by-customer');
    request.flush(structuredClone(rows));
    await fixture.whenStable();
    return request.request;
  }

  const text = (selector: string) =>
    el.querySelector(selector)?.textContent?.replace(/\s+/g, ' ').trim();

  it('requests the whole period from local start of the first day to the end of the last', async () => {
    const request = await open('2026-03-01', '2026-03-31');

    expect(new Date(request.params.get('from')!)).toEqual(new Date(2026, 2, 1, 0, 0, 0, 0));
    expect(new Date(request.params.get('to')!)).toEqual(new Date(2026, 2, 31, 23, 59, 59, 999));
    expect((el.querySelector('input[formControlName="from"]') as HTMLInputElement).value).toBe(
      '2026-03-01',
    );
  });

  it('falls back to "this year" for a missing or invalid period in the URL', async () => {
    const request = await open('2026-03-31', '2026-03-01'); // inverted

    const year = new Date().getFullYear();
    expect(new Date(request.params.get('from')!)).toEqual(new Date(year, 0, 1));
    expect(el.querySelector('.chip-active')?.textContent?.trim()).toBe('Dieses Jahr');
  });

  it('shows totals, shares and one bar per customer scaled to the largest', async () => {
    await open('2026-01-01', '2026-12-31');

    expect(text('.tile-value')).toBe('942,40 €');
    expect(text('.tiles')).toContain('2 abgerechnete Aufträge');
    expect(text('.tiles')).toContain('52 % vom Umsatz'); // 489,50 / 942,40

    const rows = el.querySelectorAll('.bar-row');
    expect(rows).toHaveLength(2);
    // The largest customer spans the full track: labour + parts = 100 %.
    const [labour, parts] = Array.from(rows[0].querySelectorAll<HTMLElement>('.seg'));
    expect(parseFloat(labour.style.width) + parseFloat(parts.style.width)).toBeCloseTo(100);
    expect(rows[1].querySelector('.bar-total')?.textContent).toContain('129,90');
  });

  it('explains a hovered segment in the readout line', async () => {
    await open('2026-01-01', '2026-12-31');

    el.querySelector('.bar-row .seg-parts')!.dispatchEvent(new Event('mouseenter'));
    await fixture.whenStable();

    expect(text('.readout')).toBe('Kfz Lorenz GmbH · Teile 367,50 € (45 % des Kundenumsatzes)');
  });

  it('has a table with a sum row as the accessible version of the chart', async () => {
    await open('2026-01-01', '2026-12-31');

    expect(el.querySelector('.bars')?.getAttribute('aria-hidden')).toBe('true');
    const sumRow = Array.from(el.querySelectorAll('tfoot th, tfoot td')).map((c) =>
      c.textContent?.trim(),
    );
    expect(sumRow).toEqual([
      'Summe',
      '2',
      // de-DE puts a no-break space between amount and currency sign.
      '489,50\u00a0€',
      '452,90\u00a0€',
      '942,40\u00a0€',
    ]);
    expect(el.querySelector('tbody a')?.getAttribute('href')).toBe('/customers/2');
  });

  it('puts a chosen period into the URL, and refuses an inverted one', async () => {
    await open('2026-01-01', '2026-12-31');
    const from = el.querySelector('input[formControlName="from"]') as HTMLInputElement;
    const to = el.querySelector('input[formControlName="to"]') as HTMLInputElement;

    from.value = '2026-06-01';
    from.dispatchEvent(new Event('input'));
    to.value = '2026-05-01';
    to.dispatchEvent(new Event('input'));
    (el.querySelector('button[type="submit"]') as HTMLButtonElement).click();
    await fixture.whenStable();
    expect(el.querySelector('[role="alert"]')?.textContent).toContain('„Von“ muss vor');
    expect(navigate).not.toHaveBeenCalled();

    to.value = '2026-06-30';
    to.dispatchEvent(new Event('input'));
    (el.querySelector('button[type="submit"]') as HTMLButtonElement).click();
    await fixture.whenStable();
    expect(navigate).toHaveBeenCalledWith([], {
      queryParams: { from: '2026-06-01', to: '2026-06-30' },
    });
  });

  it('says so when nothing was invoiced in the period', async () => {
    await open('2026-01-01', '2026-01-31', []);

    expect(text('.empty')).toContain('Keine abgerechneten Aufträge in diesem Zeitraum.');
    expect(el.querySelector('.bars')).toBeNull();
  });
});

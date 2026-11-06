import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import type { Customer } from '../../core/api/models';
import { apiErrorInterceptor } from '../../core/http/api-error.interceptor';
import { Customers } from './customers';

const CUSTOMERS: Customer[] = [
  {
    id: 1,
    name: 'Anna Becker',
    email: 'anna@example.com',
    phone: null,
    vehicles: [
      {
        id: 3,
        customerId: 1,
        licensePlate: 'M-AB 1234',
        make: 'Toyota',
        model: 'Yaris',
        year: 2019,
      },
    ],
  },
  {
    id: 2,
    name: 'Kfz Lorenz GmbH',
    email: null,
    phone: '+49 30 555 0101',
    vehicles: [
      {
        id: 4,
        customerId: 2,
        licensePlate: 'B-KL 512',
        make: 'Mercedes',
        model: 'Vito',
        year: 2018,
      },
    ],
  },
];

describe('Customers', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<Customers>;
  let el: HTMLElement;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Customers],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([apiErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(Customers);
    el = fixture.nativeElement as HTMLElement;
  });

  afterEach(() => http.verify());

  async function settle(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  async function open(id: string | undefined, customers = CUSTOMERS): Promise<void> {
    if (id !== undefined) {
      fixture.componentRef.setInput('id', id);
    }
    TestBed.tick();
    await settle();
    await respondWithList(customers);
  }

  async function respondWithList(customers = CUSTOMERS): Promise<void> {
    // A copy, like a real JSON response: flush() would otherwise hand back the
    // very same objects, and a reload would look like "nothing changed".
    http
      .expectOne((req) => req.method === 'GET' && req.url === '/api/customers')
      .flush(structuredClone(customers));
    await fixture.whenStable();
  }

  async function type(selector: string, value: string, root: ParentNode = el): Promise<void> {
    const input = root.querySelector(selector) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  async function click(element: Element | null | undefined): Promise<void> {
    (element as HTMLElement).click();
    await fixture.whenStable();
  }

  const button = (text: string, root: ParentNode = el) =>
    Array.from(root.querySelectorAll('button')).find((b) => b.textContent?.trim() === text);
  const names = () => Array.from(el.querySelectorAll('.entry-name')).map((n) => n.textContent);
  const value = (selector: string) => (el.querySelector(selector) as HTMLInputElement).value;

  it('lists customers and finds them by plate as well as by name', async () => {
    await open(undefined);
    expect(names()).toEqual(['Anna Becker', 'Kfz Lorenz GmbH']);
    expect(el.textContent).toContain('Kunden links auswählen');

    await type('#customer-search', 'bkl512');

    expect(names()).toEqual(['Kfz Lorenz GmbH']);
  });

  it('opens the customer from the URL with contact details and vehicles', async () => {
    await open('2');

    expect(el.querySelector('.entry-current .entry-name')?.textContent).toBe('Kfz Lorenz GmbH');
    expect(value('[formControlName="name"]')).toBe('Kfz Lorenz GmbH');
    expect(value('[formControlName="email"]')).toBe(''); // null shown as empty
    expect(el.querySelector('tbody .plate')?.textContent).toBe('B-KL 512');
    expect(
      el.querySelector('[aria-label="Auftrag für B-KL 512 anlegen"]')?.getAttribute('href'),
    ).toBe('/orders/new?vehicleId=4');
  });

  it('saves corrected contact details', async () => {
    await open('1');

    await type('[formControlName="phone"]', ' +49 89 1234 567 ');
    await click(button('Speichern'));

    const request = http.expectOne('/api/customers/1');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      name: 'Anna Becker',
      email: 'anna@example.com',
      phone: '+49 89 1234 567',
    });
    request.flush({ ...CUSTOMERS[0], phone: '+49 89 1234 567' });
    await settle();
    await respondWithList();

    expect(el.querySelector('[role="status"]')?.textContent).toContain('Kunde gespeichert');
  });

  it('creates a customer and opens it', async () => {
    await open('new');
    expect(el.querySelector('#customer-h')?.textContent).toContain('Neuer Kunde');

    await click(button('Kunde anlegen'));
    expect(el.textContent).toContain('Pflichtfeld.');
    http.expectNone((req) => req.method === 'POST');

    await type('[formControlName="name"]', 'Laufkundschaft GmbH');
    await click(button('Kunde anlegen'));

    const request = http.expectOne((req) => req.method === 'POST' && req.url === '/api/customers');
    expect(request.request.body).toEqual({ name: 'Laufkundschaft GmbH', email: null, phone: null });
    request.flush({ id: 9, name: 'Laufkundschaft GmbH', email: null, phone: null, vehicles: [] });
    await settle();
    await respondWithList();

    expect(navigate).toHaveBeenCalledWith(['/customers', 9]);
  });

  it('adds a vehicle and shows a taken plate at the field', async () => {
    await open('1');
    const form = el.querySelector('form.add')!;

    await type('[formControlName="licensePlate"]', 'B-KL 512', form);
    await type('[formControlName="make"]', 'Skoda', form);
    await type('[formControlName="model"]', 'Fabia', form);
    await type('[formControlName="year"]', '2015', form);
    await click(button('Fahrzeug hinzufügen'));

    http
      .expectOne('/api/customers/1/vehicles')
      .flush(
        { title: 'Conflict', detail: "License plate 'B-KL 512' is already registered." },
        { status: 409, statusText: 'Conflict' },
      );
    await fixture.whenStable();

    const plate = form.querySelector('[formControlName="licensePlate"]')!;
    expect(plate.getAttribute('aria-invalid')).toBe('true');
    expect(plate.parentElement?.textContent).toContain('already registered');
  });

  it('edits a vehicle in place', async () => {
    await open('1');

    await click(el.querySelector('[aria-label="Fahrzeug M-AB 1234 bearbeiten"]'));
    const form = el.querySelector('form[aria-label="Fahrzeug M-AB 1234 bearbeiten"]')!;
    expect((form.querySelector('[formControlName="year"]') as HTMLInputElement).value).toBe('2019');

    await type('[formControlName="model"]', 'Yaris Hybrid', form);
    await click(button('Speichern', form));

    const request = http.expectOne('/api/vehicles/3');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      licensePlate: 'M-AB 1234',
      make: 'Toyota',
      model: 'Yaris Hybrid',
      year: 2019,
    });
    request.flush({ ...CUSTOMERS[0].vehicles[0], model: 'Yaris Hybrid' });
    await settle();
    await respondWithList();

    expect(el.querySelector('form[aria-label="Fahrzeug M-AB 1234 bearbeiten"]')).toBeNull();
    expect(el.querySelector('[role="status"]')?.textContent).toContain('Fahrzeug gespeichert');
  });

  it('does not overwrite contact details being typed when the list reloads', async () => {
    await open('1');
    await type('[formControlName="phone"]', '0171 2345');

    // A vehicle added meanwhile reloads the list: same customer, new object.
    const form = el.querySelector('form.add')!;
    await type('[formControlName="licensePlate"]', 'M-AB 2', form);
    await type('[formControlName="make"]', 'VW', form);
    await type('[formControlName="model"]', 'Up', form);
    await type('[formControlName="year"]', '2020', form);
    await click(button('Fahrzeug hinzufügen'));
    http
      .expectOne('/api/customers/1/vehicles')
      .flush({ id: 5, customerId: 1, licensePlate: 'M-AB 2', make: 'VW', model: 'Up', year: 2020 });
    await settle();
    await respondWithList();

    expect(value('[formControlName="phone"]')).toBe('0171 2345');
  });

  it('says so when the customer in the URL does not exist', async () => {
    await open('99');

    expect(el.textContent).toContain('Kunde #99 nicht gefunden');
  });
});

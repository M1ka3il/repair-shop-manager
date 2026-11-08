import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import type { Customer, Vehicle } from '../../core/api/models';
import { apiErrorInterceptor } from '../../core/http/api-error.interceptor';
import { NewOrder } from './new-order';

const VEHICLES: Vehicle[] = [
  { id: 1, customerId: 1, licensePlate: 'M-SD 4711', make: 'Opel', model: 'Corsa F', year: 2021 },
  { id: 2, customerId: 2, licensePlate: 'B-KL 512', make: 'Mercedes', model: 'Vito', year: 2018 },
];
const CUSTOMERS: Customer[] = [
  { id: 1, name: 'Stefan Dorn', email: null, phone: null, vehicles: [] },
  { id: 2, name: 'Kfz Lorenz GmbH', email: null, phone: null, vehicles: [] },
];

describe('NewOrder', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<NewOrder>;
  let el: HTMLElement;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [NewOrder],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([apiErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(NewOrder);
    el = fixture.nativeElement as HTMLElement;
  });

  afterEach(() => http.verify());

  /** See order-detail.spec: lets a resource start its request before answering it. */
  async function settle(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  async function load(vehicles = VEHICLES, customers = CUSTOMERS): Promise<void> {
    TestBed.tick();
    await settle();
    http.expectOne('/api/vehicles').flush(vehicles);
    http.expectOne('/api/customers').flush(customers);
    await fixture.whenStable();
  }

  async function click(element: Element | null | undefined): Promise<void> {
    (element as HTMLElement).click();
    await fixture.whenStable();
  }

  async function type(selector: string, value: string): Promise<void> {
    const input = el.querySelector(selector) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  async function choose(selector: string, index: number): Promise<void> {
    const select = el.querySelector(selector) as HTMLSelectElement;
    select.selectedIndex = index;
    select.dispatchEvent(new Event('change'));
    await fixture.whenStable();
  }

  const button = (text: string) =>
    Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.trim().startsWith(text));
  const plates = () => Array.from(el.querySelectorAll('.result .plate')).map((p) => p.textContent);

  async function fillVehicle(plate: string): Promise<void> {
    await type('[formControlName="licensePlate"]', plate);
    await type('[formControlName="make"]', 'Skoda');
    await type('[formControlName="model"]', 'Octavia');
    await type('[formControlName="year"]', '2019');
  }

  it('lists vehicles with their owner and filters as the user types', async () => {
    await load();
    expect(plates()).toEqual(['M-SD 4711', 'B-KL 512']);
    expect(el.querySelector('.result .owner')?.textContent).toBe('Stefan Dorn');

    await type('#search', 'msd4711');

    expect(plates()).toEqual(['M-SD 4711']);
  });

  it('preselects the vehicle from ?vehicleId=', async () => {
    fixture.componentRef.setInput('vehicleId', '2');
    await load();

    expect(el.querySelector('.result-selected .plate')?.textContent).toBe('B-KL 512');
    expect(el.querySelector('.chosen')?.textContent).toContain('Kfz Lorenz GmbH');
  });

  it('creates the order and opens it', async () => {
    await load();

    await click(el.querySelector('.result input'));
    await type('#description', '  Kupplung rutscht ');
    await click(button('Auftrag anlegen'));

    const request = http.expectOne('/api/repair-orders');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ vehicleId: 1, description: 'Kupplung rutscht' });
    request.flush({ id: 13 });
    await fixture.whenStable();

    expect(navigate).toHaveBeenCalledWith(['/orders', 13]);
  });

  it('asks for a vehicle and a description before sending anything', async () => {
    await load();

    await click(button('Auftrag anlegen'));

    expect(el.textContent).toContain('Bitte ein Fahrzeug wählen');
    expect(el.textContent).toContain('Pflichtfeld.');
    http.expectNone('/api/repair-orders');
  });

  it('registers a vehicle for an existing customer and selects it', async () => {
    await load();
    await click(button('+ Neues Fahrzeug'));
    await choose('[formControlName="customer"]', 2); // "Kfz Lorenz GmbH"
    expect(el.querySelector('[formControlName="name"]')).toBeNull();

    await fillVehicle('hh-qa 1');
    await click(button('Fahrzeug anlegen'));

    const request = http.expectOne('/api/customers/2/vehicles');
    expect(request.request.body).toEqual({
      licensePlate: 'hh-qa 1',
      make: 'Skoda',
      model: 'Octavia',
      year: 2019,
    });
    request.flush({
      id: 3,
      customerId: 2,
      licensePlate: 'HH-QA 1',
      make: 'Skoda',
      model: 'Octavia',
      year: 2019,
    });
    await settle();
    await load([
      ...VEHICLES,
      {
        id: 3,
        customerId: 2,
        licensePlate: 'HH-QA 1',
        make: 'Skoda',
        model: 'Octavia',
        year: 2019,
      },
    ]);

    expect(plates()).toEqual(['HH-QA 1']); // the search now shows the new plate
    expect(el.querySelector('.result-selected .plate')?.textContent).toBe('HH-QA 1');
    expect(el.querySelector('[role="status"]')?.textContent).toContain('Fahrzeug angelegt');
  });

  it('shows a taken plate at the field and does not create the new customer twice on retry', async () => {
    await load();
    await click(button('+ Neues Fahrzeug'));
    await type('[formControlName="name"]', 'Laufkundschaft GmbH');
    await fillVehicle('M-SD 4711');
    await click(button('Fahrzeug anlegen'));

    http
      .expectOne('/api/customers')
      .flush({ id: 9, name: 'Laufkundschaft GmbH', email: null, phone: null, vehicles: [] });
    await settle();
    http
      .expectOne('/api/customers/9/vehicles')
      .flush(
        { title: 'Conflict', detail: "License plate 'M-SD 4711' is already registered." },
        { status: 409, statusText: 'Conflict' },
      );
    // The customer list reloads so the new customer shows up in the select.
    await load(VEHICLES, [
      ...CUSTOMERS,
      { id: 9, name: 'Laufkundschaft GmbH', email: null, phone: null, vehicles: [] },
    ]);

    const plateField = el.querySelector('[formControlName="licensePlate"]')!;
    expect(plateField.getAttribute('aria-invalid')).toBe('true');
    expect(plateField.parentElement?.textContent).toContain('already registered');

    // Retry with another plate: goes straight to the customer created before.
    await type('[formControlName="licensePlate"]', 'M-SD 4712');
    await click(button('Fahrzeug anlegen'));

    http.expectNone((req) => req.method === 'POST' && req.url === '/api/customers');
    http.expectOne('/api/customers/9/vehicles').flush({
      id: 4,
      customerId: 9,
      licensePlate: 'M-SD 4712',
      make: 'Skoda',
      model: 'Octavia',
      year: 2019,
    });
    await settle();
    await load();
  });

  it('shows any other rejection of the vehicle in the form', async () => {
    await load();
    await click(button('+ Neues Fahrzeug'));
    await choose('[formControlName="customer"]', 1);
    await fillVehicle('XYZ');
    await click(button('Fahrzeug anlegen'));

    http.expectOne('/api/customers/1/vehicles').flush(
      {
        title: 'Business rule violated',
        detail: "'XYZ' is not a valid German license plate (expected e.g. 'M-AB 1234').",
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await fixture.whenStable();

    expect(el.querySelector('.new-vehicle [role="alert"]')?.textContent).toContain(
      'not a valid German license plate',
    );
  });
});

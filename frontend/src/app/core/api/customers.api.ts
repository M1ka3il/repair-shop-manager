import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';
import type {
  Customer,
  CustomerRevenue,
  SaveCustomerRequest,
  SaveVehicleRequest,
  Vehicle,
} from './models';

/** Typed wrapper over the customer, vehicle and report endpoints. */
@Injectable({ providedIn: 'root' })
export class CustomersApi {
  private readonly http = inject(HttpClient);

  listCustomers(): Observable<Customer[]> {
    return this.http.get<Customer[]>('/api/customers');
  }

  getCustomer(id: number): Observable<Customer> {
    return this.http.get<Customer>(`/api/customers/${id}`);
  }

  createCustomer(request: SaveCustomerRequest): Observable<Customer> {
    return this.http.post<Customer>('/api/customers', request);
  }

  updateCustomer(id: number, request: SaveCustomerRequest): Observable<Customer> {
    return this.http.put<Customer>(`/api/customers/${id}`, request);
  }

  listVehicles(): Observable<Vehicle[]> {
    return this.http.get<Vehicle[]>('/api/vehicles');
  }

  getVehicle(id: number): Observable<Vehicle> {
    return this.http.get<Vehicle>(`/api/vehicles/${id}`);
  }

  addVehicle(customerId: number, request: SaveVehicleRequest): Observable<Vehicle> {
    return this.http.post<Vehicle>(`/api/customers/${customerId}/vehicles`, request);
  }

  updateVehicle(id: number, request: SaveVehicleRequest): Observable<Vehicle> {
    return this.http.put<Vehicle>(`/api/vehicles/${id}`, request);
  }

  /** Dates as `yyyy-MM-dd`; the backend rejects an inverted period with 400. */
  revenueByCustomer(from: string, to: string): Observable<CustomerRevenue[]> {
    return this.http.get<CustomerRevenue[]>('/api/reports/revenue-by-customer', {
      params: { from, to },
    });
  }
}

import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';
import type {
  AddRepairItemRequest,
  CreateRepairOrderRequest,
  RepairOrder,
  RepairOrderStatus,
  UpdateRepairOrderRequest,
} from './models';

/**
 * Thin, typed wrapper over `/api/repair-orders`. No logic lives here: business
 * rules stay in the backend, and errors arrive as `ApiError` via the interceptor.
 */
@Injectable({ providedIn: 'root' })
export class RepairOrdersApi {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/repair-orders';

  list(status?: RepairOrderStatus): Observable<RepairOrder[]> {
    const params = status ? new HttpParams().set('status', status) : undefined;
    return this.http.get<RepairOrder[]>(this.base, { params });
  }

  get(id: number): Observable<RepairOrder> {
    return this.http.get<RepairOrder>(`${this.base}/${id}`);
  }

  create(request: CreateRepairOrderRequest): Observable<RepairOrder> {
    return this.http.post<RepairOrder>(this.base, request);
  }

  update(id: number, request: UpdateRepairOrderRequest): Observable<RepairOrder> {
    return this.http.put<RepairOrder>(`${this.base}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  changeStatus(id: number, targetStatus: RepairOrderStatus): Observable<RepairOrder> {
    return this.http.patch<RepairOrder>(`${this.base}/${id}/status`, { targetStatus });
  }

  addItem(id: number, request: AddRepairItemRequest): Observable<RepairOrder> {
    return this.http.post<RepairOrder>(`${this.base}/${id}/items`, request);
  }

  removeItem(id: number, itemId: number): Observable<RepairOrder> {
    return this.http.delete<RepairOrder>(`${this.base}/${id}/items/${itemId}`);
  }
}

import type { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/auth/auth.guards';

const customers = () => import('./features/customers/customers').then((m) => m.Customers);

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'board' },
  {
    path: 'login',
    title: 'Anmelden · Werkstatt',
    loadComponent: () => import('./features/login/login').then((m) => m.Login),
  },
  {
    path: 'board',
    canActivate: [authGuard],
    title: 'Board · Werkstatt',
    loadComponent: () => import('./features/board/board').then((m) => m.Board),
  },
  {
    path: 'orders/new',
    canActivate: [roleGuard('Office')],
    title: 'Neuer Auftrag · Werkstatt',
    loadComponent: () => import('./features/new-order/new-order').then((m) => m.NewOrder),
  },
  {
    path: 'orders/:id',
    canActivate: [authGuard],
    title: 'Auftrag · Werkstatt',
    loadComponent: () => import('./features/order-detail/order-detail').then((m) => m.OrderDetail),
  },
  {
    // One component for the list, a selected customer and a new one; ":id" may
    // be "new". The selection lives in the URL so it can be linked and reloaded.
    path: 'customers',
    canActivate: [roleGuard('Office')],
    title: 'Kunden · Werkstatt',
    loadComponent: customers,
  },
  {
    path: 'customers/:id',
    canActivate: [roleGuard('Office')],
    title: 'Kunden · Werkstatt',
    loadComponent: customers,
  },
  {
    path: 'reports/revenue',
    canActivate: [roleGuard('Office')],
    title: 'Umsatz · Werkstatt',
    loadComponent: () => import('./features/report/report').then((m) => m.Report),
  },
  { path: '**', redirectTo: 'board' },
];

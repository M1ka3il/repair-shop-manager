import type { Routes } from '@angular/router';

const placeholder = () => import('./features/placeholder/placeholder').then((m) => m.Placeholder);

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'board' },
  {
    path: 'board',
    title: 'Board · Werkstatt',
    loadComponent: () => import('./features/board/board').then((m) => m.Board),
  },
  {
    path: 'orders/new',
    title: 'Neuer Auftrag · Werkstatt',
    loadComponent: placeholder,
    data: { title: 'Neuer Auftrag', planned: 'Fahrzeugsuche, Fahrzeug anlegen, Beschreibung.' },
  },
  {
    path: 'orders/:id',
    title: 'Auftrag · Werkstatt',
    loadComponent: placeholder,
    data: { title: 'Auftragsdetail', planned: 'Statuswechsel, Positionen, Summen.' },
  },
  {
    path: 'customers',
    title: 'Kunden · Werkstatt',
    loadComponent: placeholder,
    data: { title: 'Kunden & Fahrzeuge', planned: 'Stammdaten anlegen und korrigieren.' },
  },
  {
    path: 'reports/revenue',
    title: 'Umsatz · Werkstatt',
    loadComponent: placeholder,
    data: { title: 'Umsatzbericht', planned: 'Zeitraum, Kennzahlen, Umsatz je Kunde.' },
  },
  { path: '**', redirectTo: 'board' },
];

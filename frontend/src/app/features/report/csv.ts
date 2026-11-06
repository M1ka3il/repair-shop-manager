import type { CustomerRevenue } from '../../core/api/models';

/**
 * CSV the way German Excel opens it without an import dialog: semicolon
 * separators, decimal comma, UTF-8 BOM (so umlauts survive), CRLF line ends.
 * Amounts stay plain numbers — no currency sign, no thousands separators — so
 * the spreadsheet can still sum them.
 */
export function toCsv(rows: readonly CustomerRevenue[]): string {
  const header = ['Kunde', 'Aufträge', 'Arbeitszeit (EUR)', 'Teile (EUR)', 'Gesamt (EUR)'];
  const lines = rows.map((row) =>
    [
      quote(row.customerName),
      String(row.orderCount),
      amount(row.labourRevenue),
      amount(row.partsRevenue),
      amount(row.totalRevenue),
    ].join(';'),
  );
  return '﻿' + [header.join(';'), ...lines].join('\r\n') + '\r\n';
}

function amount(value: number): string {
  return value.toFixed(2).replace('.', ',');
}

function quote(value: string): string {
  return /[";\r\n]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value;
}

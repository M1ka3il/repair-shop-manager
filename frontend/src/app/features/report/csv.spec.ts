import { toCsv } from './csv';

describe('toCsv', () => {
  it('writes German Excel CSV: BOM, semicolons, decimal comma, CRLF', () => {
    const csv = toCsv([
      {
        customerId: 1,
        customerName: 'Kfz Lorenz GmbH',
        orderCount: 2,
        labourRevenue: 445,
        partsRevenue: 367.5,
        totalRevenue: 812.5,
      },
    ]);

    expect(csv).toBe(
      '﻿Kunde;Aufträge;Arbeitszeit (EUR);Teile (EUR);Gesamt (EUR)\r\n' +
        'Kfz Lorenz GmbH;2;445,00;367,50;812,50\r\n',
    );
  });

  it('quotes names that contain the separator or quotes', () => {
    const csv = toCsv([
      {
        customerId: 2,
        customerName: 'Müller; "Der Schrauber"',
        orderCount: 1,
        labourRevenue: 0,
        partsRevenue: 0,
        totalRevenue: 0,
      },
    ]);

    expect(csv).toContain('"Müller; ""Der Schrauber"""');
  });
});

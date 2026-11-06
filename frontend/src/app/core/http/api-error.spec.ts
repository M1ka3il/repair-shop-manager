import { HttpErrorResponse } from '@angular/common/http';
import { ApiError, toApiError } from './api-error';

describe('toApiError', () => {
  it('keeps the domain message of a rejected business rule (400 problem details)', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'Business rule violated',
        detail: "Cannot change status from 'Invoiced' to 'InProgress'.",
        traceId: 'abc:1',
      },
    });

    const result = toApiError(error);

    expect(result).toBeInstanceOf(ApiError);
    expect(result).toMatchObject({
      status: 400,
      title: 'Business rule violated',
      detail: "Cannot change status from 'Invoiced' to 'InProgress'.",
      traceId: 'abc:1',
    });
  });

  it('flattens validation problem details into the detail text', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'One or more validation errors occurred.',
        errors: { Email: ['Invalid e-mail.'], Name: ['Required.'] },
      },
    });

    expect(toApiError(error).detail).toBe('Invalid e-mail. Required.');
  });

  it('reports an unreachable server as status 0 with a German message', () => {
    const result = toApiError(new HttpErrorResponse({ status: 0 }));

    expect(result.status).toBe(0);
    expect(result.title).toBe('Server nicht erreichbar');
  });

  it('unwraps an error that was wrapped with a cause', () => {
    const inner = new ApiError(409, 'Conflict', "License plate 'M-AB 1234' is already registered.");

    expect(toApiError(new Error('wrapped', { cause: inner }))).toBe(inner);
  });
});

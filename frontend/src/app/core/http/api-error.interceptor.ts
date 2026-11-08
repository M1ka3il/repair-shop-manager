import type { HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { toApiError } from './api-error';

/**
 * Normalises every HTTP failure into an `ApiError`, so components never parse
 * `HttpErrorResponse` themselves. A rejected status change (400) then shows the
 * domain's own message, e.g. "Cannot change status from 'Invoiced' to …".
 */
export const apiErrorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(catchError((error: unknown) => throwError(() => toApiError(error))));

import { HttpErrorResponse } from '@angular/common/http';

/**
 * Every failed API call reaches components as this one type. The backend
 * answers with RFC 7807 problem details (`title`, `detail`, `traceId`), or with
 * validation problem details (`errors`) from model binding.
 *
 * A real `Error` subclass on purpose: Angular's resources wrap anything else in
 * a generic Error, which would hide the server's message.
 */
export class ApiError extends Error {
  constructor(
    /** HTTP status; 0 when the server was not reachable at all. */
    readonly status: number,
    readonly title: string,
    readonly detail: string,
    /** Links a UI error to the server's log line, if the server sent one. */
    readonly traceId?: string,
  ) {
    super(`${title}: ${detail}`);
    this.name = 'ApiError';
  }
}

interface ProblemBody {
  title?: string;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) {
    return error;
  }

  // Defensive: something upstream wrapped the original error.
  if (error instanceof Error && error.cause !== undefined) {
    return toApiError(error.cause);
  }

  if (!(error instanceof HttpErrorResponse)) {
    return new ApiError(0, 'Unerwarteter Fehler', String(error));
  }

  if (error.status === 0) {
    return new ApiError(
      0,
      'Server nicht erreichbar',
      'Bitte Verbindung prüfen und erneut versuchen.',
    );
  }

  const body: ProblemBody =
    typeof error.error === 'object' && error.error !== null ? error.error : {};
  const validation = body.errors ? Object.values(body.errors).flat().join(' ') : undefined;

  return new ApiError(
    error.status,
    body.title ?? (error.statusText || 'Fehler'),
    body.detail ?? validation ?? error.message,
    body.traceId,
  );
}

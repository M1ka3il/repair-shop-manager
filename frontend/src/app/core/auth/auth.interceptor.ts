import type { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { toApiError } from '../http/api-error';
import { AuthService } from './auth.service';

const TOKEN_ENDPOINT = '/api/auth/token';

/**
 * Sends the bearer token with every API call, and turns a 401 into "sign in
 * again" (expired or revoked token). The login request itself is left alone:
 * its 401 means "wrong password" and is shown on the login form.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const isApi = req.url.startsWith('/api/');
  const isLogin = req.url === TOKEN_ENDPOINT;
  const token = auth.token();

  const request =
    isApi && !isLogin && token
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(request).pipe(
    catchError((error: unknown) => {
      if (isApi && !isLogin && toApiError(error).status === 401) {
        auth.logout();
        void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
      }
      return throwError(() => error);
    }),
  );
};

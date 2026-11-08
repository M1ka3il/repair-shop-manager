import { inject } from '@angular/core';
import { type CanActivateFn, Router } from '@angular/router';
import { AuthService, type Role } from './auth.service';

/** Signed in, or off to the login page — and back here afterwards. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  return auth.isAuthenticated()
    ? true
    : inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/**
 * Only for the given role; others land on the board. The API refuses them
 * anyway (403) — this just spares them a screen full of errors.
 */
export const roleGuard =
  (role: Role): CanActivateFn =>
  (route, state) => {
    const auth = inject(AuthService);
    if (!auth.isAuthenticated()) {
      return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
    }
    return auth.hasRole(role) ? true : inject(Router).createUrlTree(['/board']);
  };

/** Only same-app paths: "/x" yes, "//evil.example" or "https://…" no (no open redirect). */
export function safeReturnUrl(value: string | null | undefined): string {
  return value && value.startsWith('/') && !value.startsWith('//') ? value : '/board';
}

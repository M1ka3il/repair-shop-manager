import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  type ActivatedRouteSnapshot,
  type RouterStateSnapshot,
  UrlTree,
  provideRouter,
  Router,
} from '@angular/router';
import { signInForTest, signOutForTest } from '../../../testing/auth';
import { authGuard, roleGuard, safeReturnUrl } from './auth.guards';

describe('auth guards', () => {
  const run = (guard: typeof authGuard, url: string) =>
    TestBed.runInInjectionContext(() =>
      guard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
    );
  const serialize = (result: unknown) => TestBed.inject(Router).serializeUrl(result as UrlTree);

  beforeEach(() => {
    signOutForTest();
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient()] });
  });

  afterEach(() => signOutForTest());

  it('sends anonymous users to the login, remembering where they wanted to go', () => {
    expect(serialize(run(authGuard, '/orders/7'))).toBe('/login?returnUrl=%2Forders%2F7');
  });

  it('lets signed-in users through', () => {
    signInForTest('Mechanic');
    expect(run(authGuard, '/board')).toBe(true);
  });

  it('keeps mechanics out of office pages, onto the board', () => {
    signInForTest('Mechanic');
    expect(serialize(run(roleGuard('Office'), '/reports/revenue'))).toBe('/board');
  });

  it('lets the office into office pages', () => {
    signInForTest('Office');
    expect(run(roleGuard('Office'), '/customers')).toBe(true);
  });

  it('only returns to paths inside the app (no open redirect)', () => {
    expect(safeReturnUrl('/orders/7')).toBe('/orders/7');
    expect(safeReturnUrl('//evil.example')).toBe('/board');
    expect(safeReturnUrl('https://evil.example')).toBe('/board');
    expect(safeReturnUrl(undefined)).toBe('/board');
  });
});

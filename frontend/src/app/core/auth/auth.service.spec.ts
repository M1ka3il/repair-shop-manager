import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { signInForTest, signOutForTest } from '../../../testing/auth';
import { AuthService, SESSION_STORAGE_KEY } from './auth.service';

describe('AuthService', () => {
  beforeEach(() => {
    signOutForTest();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => signOutForTest());

  it('signs in, keeps the session for this browser tab, and signs out again', () => {
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);
    expect(auth.isAuthenticated()).toBe(false);

    auth.login('buero', 'pw').subscribe();
    const request = http.expectOne('/api/auth/token');
    expect(request.request.body).toEqual({ userName: 'buero', password: 'pw' });
    request.flush({
      accessToken: 'jwt',
      userName: 'buero',
      role: 'Office',
      expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
    });

    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.isOffice()).toBe(true);
    expect(auth.token()).toBe('jwt');
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toContain('"jwt"');

    auth.logout();
    expect(auth.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it('restores a stored session on start', () => {
    signInForTest('Mechanic');
    const auth = TestBed.inject(AuthService);

    expect(auth.userName()).toBe('werkstatt');
    expect(auth.hasRole('Mechanic')).toBe(true);
    expect(auth.isOffice()).toBe(false);
  });

  it('treats an expired session as signed out', () => {
    signInForTest('Office', -1);
    const auth = TestBed.inject(AuthService);

    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.token()).toBeNull();
  });

  it('ignores garbage in storage', () => {
    sessionStorage.setItem(SESSION_STORAGE_KEY, '{"accessToken":');
    expect(TestBed.inject(AuthService).isAuthenticated()).toBe(false);
  });
});

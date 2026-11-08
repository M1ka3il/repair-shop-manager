import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { signInForTest, signOutForTest } from '../../../testing/auth';
import { apiErrorInterceptor } from '../http/api-error.interceptor';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    signInForTest('Office');
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor, apiErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    backend.verify();
    signOutForTest();
  });

  it('sends the bearer token to the API', () => {
    http.get('/api/repair-orders').subscribe();

    expect(backend.expectOne('/api/repair-orders').request.headers.get('Authorization')).toBe(
      'Bearer test-token-Office',
    );
  });

  it('sends no token to other hosts or to the login itself', () => {
    http.get('https://example.com/api/x').subscribe();
    http.post('/api/auth/token', {}).subscribe();

    expect(
      backend.expectOne('https://example.com/api/x').request.headers.has('Authorization'),
    ).toBe(false);
    expect(backend.expectOne('/api/auth/token').request.headers.has('Authorization')).toBe(false);
  });

  it('signs out and asks to sign in again when the API answers 401', () => {
    http.get('/api/repair-orders').subscribe({ error: () => undefined });
    backend
      .expectOne('/api/repair-orders')
      .flush({ title: 'Not signed in' }, { status: 401, statusText: 'Unauthorized' });

    expect(TestBed.inject(AuthService).isAuthenticated()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: { returnUrl: '/' } });
  });

  it('leaves a failed login (401 = wrong password) to the login form', () => {
    http.post('/api/auth/token', {}).subscribe({ error: () => undefined });
    backend.expectOne('/api/auth/token').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(navigate).not.toHaveBeenCalled();
  });
});

import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { signOutForTest } from '../../../testing/auth';
import { authInterceptor } from '../../core/auth/auth.interceptor';
import { apiErrorInterceptor } from '../../core/http/api-error.interceptor';
import { Login } from './login';

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let el: HTMLElement;
  let backend: HttpTestingController;
  let navigateByUrl: ReturnType<typeof vi.spyOn>;

  beforeEach(async () => {
    signOutForTest();
    TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor, apiErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    backend = TestBed.inject(HttpTestingController);
    navigateByUrl = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(Login);
    el = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
  });

  afterEach(() => {
    backend.verify();
    signOutForTest();
  });

  async function fillAndSubmit(user: string, password: string): Promise<void> {
    for (const [name, value] of [
      ['userName', user],
      ['password', password],
    ]) {
      const input = el.querySelector(`[formControlName="${name}"]`) as HTMLInputElement;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    (el.querySelector('button[type="submit"]') as HTMLButtonElement).click();
    await fixture.whenStable();
  }

  it('signs in and returns to the requested page', async () => {
    fixture.componentRef.setInput('returnUrl', '/orders/7');
    await fillAndSubmit(' buero ', 'pw');

    const request = backend.expectOne('/api/auth/token');
    expect(request.request.body).toEqual({ userName: 'buero', password: 'pw' });
    request.flush({
      accessToken: 'jwt',
      userName: 'buero',
      role: 'Office',
      expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
    });
    await fixture.whenStable();

    expect(navigateByUrl).toHaveBeenCalledWith('/orders/7');
  });

  it('ignores a return URL pointing outside the app', async () => {
    fixture.componentRef.setInput('returnUrl', '//evil.example');
    await fillAndSubmit('buero', 'pw');
    backend.expectOne('/api/auth/token').flush({
      accessToken: 'jwt',
      userName: 'buero',
      role: 'Office',
      expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
    });
    await fixture.whenStable();

    expect(navigateByUrl).toHaveBeenCalledWith('/board');
  });

  it('says the credentials are wrong and clears the password', async () => {
    await fillAndSubmit('buero', 'falsch');
    backend.expectOne('/api/auth/token').flush({}, { status: 401, statusText: 'Unauthorized' });
    await fixture.whenStable();

    expect(el.querySelector('[role="alert"]')?.textContent).toContain(
      'Benutzername oder Passwort ist falsch.',
    );
    expect((el.querySelector('[formControlName="password"]') as HTMLInputElement).value).toBe('');
    expect(navigateByUrl).not.toHaveBeenCalled();
  });

  it('explains throttling', async () => {
    await fillAndSubmit('buero', 'x');
    backend
      .expectOne('/api/auth/token')
      .flush({}, { status: 429, statusText: 'Too Many Requests' });
    await fixture.whenStable();

    expect(el.querySelector('[role="alert"]')?.textContent).toContain('Zu viele Anmeldeversuche');
  });

  it('does not send an empty form', async () => {
    await fillAndSubmit('', '');

    backend.expectNone('/api/auth/token');
    expect(el.textContent).toContain('Pflichtfeld.');
  });
});

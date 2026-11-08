import { type Role, SESSION_STORAGE_KEY } from '../app/core/auth/auth.service';

/**
 * Puts a session in storage the way a real login does. Call before the
 * AuthService is first injected (it reads storage once, on creation).
 */
export function signInForTest(role: Role, minutesValid = 60): void {
  sessionStorage.setItem(
    SESSION_STORAGE_KEY,
    JSON.stringify({
      accessToken: `test-token-${role}`,
      userName: role === 'Office' ? 'buero' : 'werkstatt',
      role,
      expiresAtUtc: new Date(Date.now() + minutesValid * 60_000).toISOString(),
    }),
  );
}

export function signOutForTest(): void {
  sessionStorage.removeItem(SESSION_STORAGE_KEY);
}

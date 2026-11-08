import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { type Observable, tap } from 'rxjs';
import type { TokenResponse } from '../api/models';

/** Roles as the API names them (RepairShop.Application.Auth.Roles). */
export type Role = 'Mechanic' | 'Office';

export const ROLE_LABELS: Record<Role, string> = { Mechanic: 'Werkstatt', Office: 'Büro' };

export interface Session {
  readonly accessToken: string;
  readonly userName: string;
  readonly role: Role;
  readonly expiresAtUtc: string;
}

/** Must match nothing else in storage; read/written only here. */
export const SESSION_STORAGE_KEY = 'repair-shop-session';

/**
 * Who is signed in. The token lives in sessionStorage: closing the browser
 * signs out, which suits shared workshop PCs. (An HttpOnly cookie would also
 * keep it away from scripts, but needs CSRF protection on the API — a separate
 * step.) The role here only shapes the UI; the API enforces every rule itself.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly session = signal<Session | null>(readSession());

  readonly userName = computed(() => this.current()?.userName ?? null);
  readonly role = computed(() => this.current()?.role ?? null);
  readonly isAuthenticated = computed(() => this.current() !== null);
  readonly isOffice = computed(() => this.role() === 'Office');

  /** The session if it has not expired; an expired one counts as signed out. */
  current(): Session | null {
    const session = this.session();
    return session && Date.parse(session.expiresAtUtc) > Date.now() ? session : null;
  }

  token(): string | null {
    return this.current()?.accessToken ?? null;
  }

  hasRole(role: Role): boolean {
    return this.role() === role;
  }

  login(userName: string, password: string): Observable<TokenResponse> {
    return this.http.post<TokenResponse>('/api/auth/token', { userName, password }).pipe(
      tap((response) => {
        const session: Session = {
          accessToken: response.accessToken,
          userName: response.userName,
          role: response.role as Role,
          expiresAtUtc: response.expiresAtUtc,
        };
        this.session.set(session);
        writeSession(session);
      }),
    );
  }

  logout(): void {
    this.session.set(null);
    writeSession(null);
  }
}

function readSession(): Session | null {
  try {
    const raw = sessionStorage.getItem(SESSION_STORAGE_KEY);
    const parsed = raw ? (JSON.parse(raw) as Partial<Session>) : null;
    return parsed?.accessToken && parsed.userName && parsed.role && parsed.expiresAtUtc
      ? (parsed as Session)
      : null;
  } catch {
    return null;
  }
}

function writeSession(session: Session | null): void {
  try {
    if (session) {
      sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
    } else {
      sessionStorage.removeItem(SESSION_STORAGE_KEY);
    }
  } catch {
    // Storage unavailable: the session still works until the page is reloaded.
  }
}

import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService, ROLE_LABELS } from './core/auth/auth.service';
import { ThemeService } from './core/theme/theme';

/** Application shell: header with navigation, routed page below. */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly theme = inject(ThemeService);

  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  private readonly allNav = [
    { path: '/board', label: 'Board', officeOnly: false },
    { path: '/customers', label: 'Kunden', officeOnly: true },
    { path: '/reports/revenue', label: 'Umsatzbericht', officeOnly: true },
  ];

  /** Only what this role may open; the API is the actual gate. */
  protected readonly nav = computed(() =>
    this.auth.isAuthenticated()
      ? this.allNav.filter((item) => !item.officeOnly || this.auth.isOffice())
      : [],
  );

  protected readonly roleLabel = computed(() => {
    const role = this.auth.role();
    return role ? ROLE_LABELS[role] : '';
  });

  protected logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}

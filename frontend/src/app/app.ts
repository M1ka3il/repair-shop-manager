import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
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

  protected readonly nav = [
    { path: '/board', label: 'Board' },
    { path: '/customers', label: 'Kunden' },
    { path: '/reports/revenue', label: 'Umsatzbericht' },
  ];
}

import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Stand-in for screens that exist in the mockups but are not built yet. The
 * route's `data` is bound to the inputs (withComponentInputBinding), so the
 * route table documents what is still missing.
 */
@Component({
  selector: 'app-placeholder',
  imports: [RouterLink],
  template: `
    <h1>{{ title() }}</h1>
    <p class="muted">Noch nicht umgesetzt – siehe UI-Mockups. Geplant: {{ planned() }}</p>
    <a routerLink="/board">← Zum Board</a>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Placeholder {
  readonly title = input.required<string>();
  readonly planned = input<string>('');
}

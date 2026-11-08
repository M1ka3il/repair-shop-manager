import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { safeReturnUrl } from '../../core/auth/auth.guards';
import { AuthService } from '../../core/auth/auth.service';
import { toApiError } from '../../core/http/api-error';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Login {
  /** Where to go after signing in (query param); only same-app paths are honoured. */
  readonly returnUrl = input<string>();

  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly pending = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly submitted = signal(false);

  protected readonly form = new FormGroup({
    userName: new FormControl('', { nonNullable: true, validators: Validators.required }),
    password: new FormControl('', { nonNullable: true, validators: Validators.required }),
  });

  protected submit(): void {
    this.submitted.set(true);
    if (this.form.invalid || this.pending()) {
      return;
    }
    const { userName, password } = this.form.getRawValue();
    this.pending.set(true);
    this.error.set(null);
    this.auth
      .login(userName.trim(), password)
      .pipe(
        finalize(() => this.pending.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => void this.router.navigateByUrl(safeReturnUrl(this.returnUrl())),
        error: (error: unknown) => {
          const status = toApiError(error).status;
          this.form.controls.password.reset();
          this.error.set(
            status === 401
              ? 'Benutzername oder Passwort ist falsch.'
              : status === 429
                ? 'Zu viele Anmeldeversuche. Bitte eine Minute warten.'
                : status === 0
                  ? 'Server nicht erreichbar. Bitte Verbindung prüfen.'
                  : 'Anmeldung fehlgeschlagen. Bitte später erneut versuchen.',
          );
        },
      });
  }
}

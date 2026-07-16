import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AuthStateService } from '../../core/auth/auth-state.service';
import { ApiError } from '../../core/api-error';
import { LanguageService } from '../../core/language.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, TranslocoPipe],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login implements OnInit {
  private readonly auth = inject(AuthStateService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly email = signal('');
  protected readonly password = signal('');
  protected readonly showPassword = signal(false);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly canSubmit = computed(
    () => !this.loading() && this.email().trim().length > 0 && this.password().length > 0
  );

  ngOnInit(): void {
    // Already authenticated → skip the login screen.
    if (this.auth.isAuthenticated()) {
      void this.router.navigateByUrl(this.returnUrl());
    }
  }

  toggleLang(): void {
    this.language.toggle();
  }

  togglePassword(): void {
    this.showPassword.update((v) => !v);
  }

  onLogin(): void {
    if (this.loading()) return;
    const email = this.email().trim();
    const password = this.password();
    if (!email || !password) {
      this.error.set(this.transloco.translate('login.errorRequired'));
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.auth.login(email, password).subscribe({
      next: () => {
        this.loading.set(false);
        void this.router.navigateByUrl(this.returnUrl());
      },
      error: (err: ApiError) => {
        this.loading.set(false);
        this.error.set(this.messageFor(err));
      }
    });
  }

  /** Safe, user-facing message — never leaks backend detail. */
  private messageFor(err: ApiError): string {
    if (err?.kind === 'unauthenticated' || err?.status === 401) {
      return this.transloco.translate('login.errorInvalid');
    }
    if (err?.status === 429) {
      // The backend locks accounts / rate-limits; surface a safe "try later" message.
      return this.transloco.translate('login.errorLocked');
    }
    if (err?.kind === 'validation' || err?.status === 400) {
      return this.transloco.translate('login.errorInvalid');
    }
    return this.transloco.translate('login.errorGeneric');
  }

  private returnUrl(): string {
    const url = this.route.snapshot.queryParamMap.get('returnUrl');
    // Only allow same-app relative paths (avoid open redirects).
    return url && url.startsWith('/') && !url.startsWith('//') ? url : '/hub';
  }
}

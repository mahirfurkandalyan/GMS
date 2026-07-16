import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, map, of, tap, throwError } from 'rxjs';
import { shareReplay } from 'rxjs/operators';
import { AuthApiService } from './auth-api.service';
import { TokenStorageService } from './token-storage.service';
import { AuthResponse, AuthUser } from './auth.models';

/**
 * The single owner of authentication STATE: current user, authenticated flag, roles, permissions,
 * and the login / logout / session-restoration / refresh lifecycle. Components and guards read from
 * here (never from storage). Token persistence is delegated to TokenStorageService; raw HTTP to
 * AuthApiService. This is deliberately NOT one giant AuthService.
 */
@Injectable({ providedIn: 'root' })
export class AuthStateService {
  private readonly api = inject(AuthApiService);
  private readonly storage = inject(TokenStorageService);

  private readonly userSig = signal<AuthUser | null>(null);
  readonly user = this.userSig.asReadonly();
  readonly isAuthenticated = computed(() => this.userSig() !== null);
  readonly roles = computed(() => this.userSig()?.roles ?? []);
  readonly permissions = computed(() => this.userSig()?.permissions ?? []);

  /** Single-flight refresh — concurrent callers share one in-flight refresh request. */
  private refreshInFlight: Observable<string> | null = null;

  hasPermission(code: string): boolean {
    return this.userSig()?.permissions.includes(code) ?? false;
  }

  hasAnyPermission(codes: string[]): boolean {
    const perms = this.userSig()?.permissions ?? [];
    return codes.some((c) => perms.includes(c));
  }

  hasAllPermissions(codes: string[]): boolean {
    const perms = this.userSig()?.permissions ?? [];
    return codes.every((c) => perms.includes(c));
  }

  hasRole(role: string): boolean {
    return this.userSig()?.roles.includes(role) ?? false;
  }

  hasAnyRole(rolesToCheck: string[]): boolean {
    const roles = this.userSig()?.roles ?? [];
    return rolesToCheck.some((r) => roles.includes(r));
  }

  /** Email + password login. Stores the token pair and current user on success. */
  login(email: string, password: string): Observable<AuthUser> {
    return this.api.login({ email, password }).pipe(
      tap((r) => this.applySession(r)),
      map((r) => r.user)
    );
  }

  /**
   * Restores a session on app startup:
   *  - valid access token in memory → confirm via /me;
   *  - expired access but a usable refresh token → refresh, then adopt the user;
   *  - otherwise clear. Resolves to true when a user is restored.
   */
  restoreSession(): Observable<boolean> {
    // Valid access token → confirm via /me (the interceptor auto-refreshes on a 401).
    if (this.storage.hasValidAccessToken()) {
      return this.api.me().pipe(
        tap((u) => this.userSig.set(u)),
        map(() => true),
        catchError(() => { this.clearSession(); return of(false); })
      );
    }
    // Expired access but a usable refresh token → refresh, then adopt the returned user.
    if (this.storage.hasUsableRefreshToken()) {
      return this.refresh().pipe(
        map(() => true),
        catchError(() => { this.clearSession(); return of(false); })
      );
    }
    this.clearSession();
    return of(false);
  }

  /**
   * Single-flight rotating refresh. Returns the new access token. If a refresh is already running,
   * the same observable is returned so N concurrent 401s trigger exactly one refresh request.
   */
  refresh(): Observable<string> {
    if (this.refreshInFlight) return this.refreshInFlight;

    const rt = this.storage.refreshToken;
    if (!rt) return throwError(() => new Error('no-refresh-token'));

    this.refreshInFlight = this.api.refresh(rt).pipe(
      tap((r) => this.applySession(r)),
      map((r) => r.accessToken),
      catchError((e) => {
        this.clearSession();
        return throwError(() => e);
      }),
      finalize(() => (this.refreshInFlight = null)),
      shareReplay(1)
    );
    return this.refreshInFlight;
  }

  /** Normal logout: best-effort backend revoke, then clear local state regardless of the result. */
  logout(): Observable<void> {
    const rt = this.storage.refreshToken;
    return this.api.logout(rt).pipe(
      catchError(() => of(void 0)),
      finalize(() => this.clearSession())
    );
  }

  /** Revoke every session for this user, then clear local state. */
  logoutAll(): Observable<void> {
    return this.api.logoutAll().pipe(
      catchError(() => of(void 0)),
      finalize(() => this.clearSession())
    );
  }

  /**
   * Changes the password. On success the backend revokes every refresh token, so the caller must
   * clear the local session and send the user back to login.
   */
  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.api.changePassword({ currentPassword, newPassword });
  }

  /** Clears in-memory user + all persisted tokens. */
  clearSession(): void {
    this.userSig.set(null);
    this.storage.clear();
  }

  private applySession(r: AuthResponse): void {
    this.storage.setTokens(r.accessToken, r.refreshToken, r.accessTokenExpiresAt, r.refreshTokenExpiresAt);
    this.userSig.set(r.user);
  }
}

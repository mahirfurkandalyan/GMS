import { Injectable, computed, inject } from '@angular/core';
import { AuthStateService } from './auth/auth-state.service';

/**
 * COMPATIBILITY FACADE (deprecated surface).
 *
 * The real authentication system is {@link AuthStateService} (+ AuthApiService / TokenStorageService).
 * This thin facade is kept ONLY so already-built feature components (which read the current user)
 * keep working without a big-bang refactor — per the sprint's "do not migrate every module at once"
 * rule. It contains NO mock login, NO localStorage identity, NO role-name heuristics beyond a simple
 * manager convenience. New code should inject AuthStateService directly.
 */
export interface CurrentUser {
  userId: string;
  fullName: string;
  email: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly state = inject(AuthStateService);

  /** Reactive current user in the legacy shape, derived from the real auth state. */
  readonly currentUser = computed<CurrentUser | null>(() => {
    const u = this.state.user();
    return u ? { userId: u.id, fullName: u.fullName, email: u.email, roles: u.roles } : null;
  });

  getCurrentUser(): CurrentUser | null {
    return this.currentUser();
  }

  isLoggedIn(): boolean {
    return this.state.isAuthenticated();
  }

  /** Convenience "elevated" check used by some feature UIs. Backend policies remain authoritative. */
  isManager(): boolean {
    return this.state.hasRole('Admin') || this.state.hasRole('Architect');
  }
}

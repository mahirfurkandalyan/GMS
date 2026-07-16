import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router, RouterStateSnapshot } from '@angular/router';
import { AuthStateService } from './auth-state.service';

/**
 * Route guards backed by AuthStateService (never localStorage). authGuard enforces authentication;
 * permissionGuard/roleGuard enforce authorization. Backend policies remain the real security
 * authority — these only shape client-side navigation.
 */

/** Requires an authenticated session; otherwise redirects to /login preserving returnUrl. */
export const authGuard: CanActivateFn = (_route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const auth = inject(AuthStateService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/**
 * Enforces route permission(s) from route data:
 *   data: { permission: 'release.create' }
 *   data: { permissions: ['release.read','release.create'], permissionMode: 'all' | 'any' }
 * Unauthenticated → /login (with returnUrl); authenticated-but-unauthorized → /forbidden.
 */
export const permissionGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const auth = inject(AuthStateService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }

  const single = route.data['permission'] as string | undefined;
  const multiple = route.data['permissions'] as string[] | undefined;
  const mode = (route.data['permissionMode'] as 'all' | 'any' | undefined) ?? 'any';

  const required = single ? [single] : (multiple ?? []);
  if (required.length === 0) return true; // no permission declared → authentication is enough

  const ok = mode === 'all' ? auth.hasAllPermissions(required) : auth.hasAnyPermission(required);
  return ok ? true : router.createUrlTree(['/forbidden']);
};

/**
 * Enforces route role(s) from route data: `data: { roles: ['Admin'] }`. Prefer permissionGuard;
 * use this only where a genuine role check is needed. Any listed role grants access.
 */
export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const auth = inject(AuthStateService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }
  const roles = (route.data['roles'] as string[] | undefined) ?? [];
  if (roles.length === 0) return true;
  return auth.hasAnyRole(roles) ? true : router.createUrlTree(['/forbidden']);
};

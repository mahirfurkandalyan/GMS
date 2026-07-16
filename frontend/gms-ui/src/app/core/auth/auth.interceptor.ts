import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { AuthStateService } from './auth-state.service';
import { TokenStorageService } from './token-storage.service';

/** login/refresh are anonymous and must never carry a Bearer token or trigger a refresh loop. */
function isNoAuthCall(url: string): boolean {
  return url.endsWith('/auth/login') || url.endsWith('/auth/refresh');
}

/**
 * Attaches the Bearer access token to GMS API requests (only) and, on a 401, coordinates a single
 * rotating refresh before retrying the original request ONCE.
 *
 * Registered as the INNER interceptor (see app.config) so it sees the raw HttpErrorResponse before
 * the global error normalizer. Its `next` is the terminal (backend) handler, so the retry does not
 * re-enter the interceptor chain — preventing infinite loops.
 *
 * Concurrency: AuthStateService.refresh() is single-flight (shareReplay), so N simultaneous 401s
 * cause exactly one refresh request; all waiting requests retry once the new token arrives.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthStateService);
  const storage = inject(TokenStorageService);
  const router = inject(Router);

  const isApi = req.url.startsWith(API_BASE_URL);
  const noAuth = isNoAuthCall(req.url);
  const token = storage.accessToken;

  // Only attach to our API, and never to the anonymous login/refresh calls or external URLs.
  const authed = isApi && token && !noAuth
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authed).pipe(
    catchError((err: HttpErrorResponse) => {
      // 403 must NOT trigger a refresh/logout; only an expired-session 401 does.
      if (err.status !== 401 || !isApi || noAuth) {
        return throwError(() => err);
      }
      return auth.refresh().pipe(
        // Refresh itself failed → the session is dead: clear and send to login.
        catchError((refreshErr) => {
          auth.clearSession();
          void router.navigateByUrl('/login');
          return throwError(() => refreshErr);
        }),
        // Retry the original request ONCE with the new token (goes straight to backend).
        switchMap((newToken) =>
          next(authed.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } }))
        )
      );
    })
  );
};

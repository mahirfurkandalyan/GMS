import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { normalizeHttpError } from './api-error';

/**
 * Global API error normalizer. Converts every failed response into a typed {@link ApiError} with a
 * safe Turkish message, so feature code and toasts never handle raw backend payloads or stack
 * traces. Registered as the OUTER interceptor (see app.config) so the inner auth interceptor gets a
 * chance to transparently refresh a 401 first; only unresolved errors reach here and get normalized.
 */
export const apiErrorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((err) => {
      if (err instanceof HttpErrorResponse) {
        return throwError(() => normalizeHttpError(err));
      }
      return throwError(() => err);
    })
  );

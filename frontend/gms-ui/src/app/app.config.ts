import { ApplicationConfig, inject, isDevMode, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideTransloco } from '@jsverse/transloco';
import { catchError, firstValueFrom, of } from 'rxjs';

import { routes } from './app.routes';
import { I18nLoader } from './core/i18n.loader';
import { LanguageService } from './core/language.service';
import { authInterceptor } from './core/auth/auth.interceptor';
import { apiErrorInterceptor } from './core/api-error.interceptor';
import { AuthStateService } from './core/auth/auth-state.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    // Interceptor order is deliberate: apiErrorInterceptor is OUTER (normalizes unresolved errors),
    // authInterceptor is INNER (sees raw 401s to run a single rotating refresh + one retry).
    provideHttpClient(withFetch(), withInterceptors([apiErrorInterceptor, authInterceptor])),
    // Session restoration BEFORE the first route resolves → no protected-shell flicker.
    provideAppInitializer(() => {
      const auth = inject(AuthStateService);
      return firstValueFrom(auth.restoreSession().pipe(catchError(() => of(false))));
    }),
    provideTransloco({
      config: {
        availableLangs: ['tr', 'en'],
        defaultLang: LanguageService.resolveInitialLang(),
        reRenderOnLangChange: true,
        prodMode: !isDevMode()
      },
      loader: I18nLoader
    })
  ]
};

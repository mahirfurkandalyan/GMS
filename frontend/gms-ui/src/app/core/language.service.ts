import { Injectable, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoService } from '@jsverse/transloco';

const LANG_KEY = 'gms.lang';
export type AppLang = 'tr' | 'en';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly transloco = inject(TranslocoService);

  readonly current = toSignal(this.transloco.langChanges$, {
    initialValue: this.transloco.getActiveLang()
  });

  setLang(lang: AppLang): void {
    localStorage.setItem(LANG_KEY, lang);
    this.transloco.setActiveLang(lang);
  }

  toggle(): void {
    this.setLang(this.current() === 'tr' ? 'en' : 'tr');
  }

  static resolveInitialLang(): AppLang {
    const stored = localStorage.getItem(LANG_KEY);
    if (stored === 'tr' || stored === 'en') return stored;
    return navigator.language?.toLowerCase().startsWith('en') ? 'en' : 'tr';
  }
}

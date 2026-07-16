import { Component, inject } from '@angular/core';
import { Location } from '@angular/common';
import { RouterLink } from '@angular/router';

/**
 * 403 access-denied page. Shown when an AUTHENTICATED user lacks the permission for a route. The
 * session is preserved (no logout) — the user can go back or to the hub. Uses the existing design.
 */
@Component({
  selector: 'app-forbidden',
  imports: [RouterLink],
  template: `
    <div class="forbidden">
      <div class="forbidden__card">
        <div class="forbidden__icon">403</div>
        <h1 class="forbidden__title">Erişim reddedildi</h1>
        <p class="forbidden__text">
          Bu sayfayı görüntülemek için gerekli yetkiye sahip değilsiniz.
          Oturumunuz açık kalmaya devam ediyor.
        </p>
        <div class="forbidden__actions">
          <button type="button" class="forbidden__btn forbidden__btn--ghost" (click)="goBack()">Geri dön</button>
          <a class="forbidden__btn forbidden__btn--primary" routerLink="/hub">Ana sayfaya git</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .forbidden { display: flex; align-items: center; justify-content: center; min-height: 60vh; padding: 2rem; }
    .forbidden__card { text-align: center; max-width: 460px; background: var(--surface, #fff);
      border: 1px solid var(--border, #e2e8f0); border-radius: 16px; padding: 2.5rem 2rem;
      box-shadow: 0 10px 30px rgba(15,23,42,0.06); }
    .forbidden__icon { font-size: 3rem; font-weight: 800; letter-spacing: 2px; color: #ef4444; margin-bottom: 0.5rem; }
    .forbidden__title { font-size: 1.35rem; font-weight: 700; margin: 0 0 0.5rem; color: var(--text, #0f172a); }
    .forbidden__text { color: var(--text-muted, #64748b); line-height: 1.5; margin: 0 0 1.5rem; }
    .forbidden__actions { display: flex; gap: 0.75rem; justify-content: center; flex-wrap: wrap; }
    .forbidden__btn { padding: 0.6rem 1.1rem; border-radius: 10px; font-weight: 600; cursor: pointer;
      text-decoration: none; border: 1px solid transparent; font-size: 0.9rem; }
    .forbidden__btn--primary { background: var(--primary, #2563eb); color: #fff; }
    .forbidden__btn--ghost { background: transparent; border-color: var(--border, #e2e8f0); color: var(--text, #0f172a); }
  `]
})
export class Forbidden {
  private readonly location = inject(Location);
  goBack(): void {
    this.location.back();
  }
}

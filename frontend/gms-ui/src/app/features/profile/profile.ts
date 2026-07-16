import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { AuthService } from '../../core/auth.service';
import { AuthStateService } from '../../core/auth/auth-state.service';
import { ApiError } from '../../core/api-error';
import { LanguageService } from '../../core/language.service';
import { GmsTabs, TabItem } from '../../shared/ui/tabs/tabs';
import { GmsAuditList, AuditEntry } from '../../shared/ui/audit-list/audit-list';
import { GmsTimeline, TimelineItem } from '../../shared/ui/timeline/timeline';

@Component({
  selector: 'app-profile',
  imports: [FormsModule, RouterLink, GmsTabs, GmsAuditList, GmsTimeline, TranslocoPipe],
  providers: [provideTranslocoScope('profile')],
  templateUrl: './profile.html',
  styleUrl: './profile.scss'
})
export class Profile {
  private readonly auth = inject(AuthService);
  private readonly authState = inject(AuthStateService);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  // ── Change password ──
  protected readonly currentPassword = signal('');
  protected readonly newPassword = signal('');
  protected readonly confirmPassword = signal('');
  protected readonly pwLoading = signal(false);
  protected readonly pwError = signal<string | null>(null);

  changePassword(): void {
    if (this.pwLoading()) return;
    const cur = this.currentPassword();
    const nw = this.newPassword();
    const conf = this.confirmPassword();
    if (!cur || !nw || !conf) { this.pwError.set(this.transloco.translate('profile.security.errorRequired')); return; }
    if (nw !== conf) { this.pwError.set(this.transloco.translate('profile.security.errorMismatch')); return; }
    if (nw.length < 8) { this.pwError.set(this.transloco.translate('profile.security.errorLength')); return; }

    this.pwLoading.set(true);
    this.pwError.set(null);
    this.authState.changePassword(cur, nw).subscribe({
      next: () => {
        // Backend revoked all refresh tokens → drop the local session and re-authenticate.
        this.authState.clearSession();
        void this.router.navigate(['/login'], { queryParams: { pwChanged: '1' } });
      },
      error: (err: ApiError) => {
        this.pwLoading.set(false);
        this.pwError.set(
          err?.status === 400 || err?.kind === 'validation'
            ? this.transloco.translate('profile.security.errorCurrent')
            : this.transloco.translate('profile.security.errorGeneric')
        );
      }
    });
  }

  logoutAll(): void {
    this.authState.logoutAll().subscribe({ complete: () => this.router.navigate(['/login']) });
  }

  protected readonly currentUser = this.auth.currentUser;
  protected readonly fullName = computed(() => this.currentUser()?.fullName ?? '');
  protected readonly email = computed(() => this.currentUser()?.email ?? '');
  protected readonly roles = computed(() => this.currentUser()?.roles ?? []);
  protected readonly initial = computed(
    () => this.fullName().charAt(0).toUpperCase() || '?'
  );

  protected readonly activeTab = signal('general');

  // gms-tabs düz metin bekliyor (labelKey değil) — dil değişince yeniden hesaplanması
  // için computed() + transloco.translate() kalıbı kullanılıyor (bkz. hub.ts / badge.ts).
  protected readonly tabs = computed<TabItem[]>(() => {
    this.language.current();
    const t = (key: string) => this.transloco.translate(key);
    return [
      { id: 'general', label: t('profile.tabs.general'), icon: 'user' },
      { id: 'history', label: t('profile.tabs.history'), icon: 'audit', badge: 3 },
      { id: 'activity', label: t('profile.tabs.activity'), icon: 'activity' },
      { id: 'prefs', label: t('profile.tabs.prefs'), icon: 'bell' }
    ];
  });

  // Mock — Onay geçmişi (audit list bileşenini gösterir). gms-audit-list düz metin
  // bekliyor, bu yüzden reaktif computed() ile çevriliyor. "System Administrator" /
  // "Architect User" / "QA Specialist" sistem rol tanımlayıcılarıdır, çevrilmez.
  // "Onaylandı"/"Reddedildi" durumları paylaşılan badge kaydından (badge.status.*) alınır.
  protected readonly approvalHistory = computed<AuditEntry[]>(() => {
    this.language.current();
    const t = (key: string) => this.transloco.translate(key);
    return [
      { time: '05.07.2026 14:20', user: 'System Administrator', action: t('profile.history.approvedRelease'), status: { label: t('badge.status.Approved'), tone: 'success' } },
      { time: '04.07.2026 11:05', user: 'Architect User', action: t('profile.history.reviewedChange'), description: t('profile.history.reviewedChangeDesc'), status: { label: t('profile.history.reviewedStatus'), tone: 'info' } },
      { time: '02.07.2026 09:40', user: 'QA Specialist', action: t('profile.history.rejectedValidation'), status: { label: t('badge.status.Rejected'), tone: 'danger' } }
    ];
  });

  // Mock — Aktivite (timeline bileşenini gösterir). Proje/eğitim adları (EBR Migration,
  // GxP Temelleri) seed verisi olarak çevrilmeden bırakılıyor.
  protected readonly activity = computed<TimelineItem[]>(() => {
    this.language.current();
    const t = (key: string) => this.transloco.translate(key);
    return [
      { title: t('profile.activity.signedIn'), time: `${t('profile.time.today')} 09:12`, tone: 'info', icon: 'user' },
      { title: t('profile.activity.addedToProject'), time: t('profile.time.yesterday'), tone: 'success', icon: 'team' },
      { title: t('profile.activity.assignedTraining'), time: t('profile.activity.daysAgo3'), tone: 'warning', icon: 'training', description: `${t('profile.activity.dueDatePrefix')}: 15.08.2026` },
      { title: t('profile.activity.accountCreated'), time: '01.01.2026', icon: 'shield' }
    ];
  });
}

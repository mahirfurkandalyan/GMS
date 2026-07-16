import { Component, computed, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from './core/auth.service';
import { AuthStateService } from './core/auth/auth-state.service';
import { NotificationService, NOTIFICATION_META } from './core/notification.service';
import { SearchService, SearchResult } from './core/search.service';
import { SearchHistoryService } from './core/search-history.service';
import { NavigationService } from './core/navigation.service';
import { LanguageService } from './core/language.service';
import { GmsIcon } from './shared/icon/icon';
import { GmsCommandPalette } from './shared/ui/command-palette/command-palette';
import { GmsToastHost } from './shared/ui/toast/toast';
import { GmsConfirmHost, GmsDrawer } from './shared/ui/dialog/dialog';
import { GmsQuickCreate } from './shared/ui/quick-create/quick-create';
import { GmsNotificationList, NotificationRow } from './shared/ui/notification-list/notification-list';

const SIDEBAR_KEY = 'gms.sidebarCollapsed';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    FormsModule,
    TranslocoPipe,
    GmsIcon,
    GmsCommandPalette,
    GmsToastHost,
    GmsConfirmHost,
    GmsDrawer,
    GmsQuickCreate,
    GmsNotificationList
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly authState = inject(AuthStateService);
  private readonly router = inject(Router);
  private readonly notif = inject(NotificationService);
  private readonly searchSvc = inject(SearchService);
  private readonly searchHistory = inject(SearchHistoryService);
  private readonly nav = inject(NavigationService);
  protected readonly language = inject(LanguageService);

  protected readonly navSections = this.nav.sections;

  protected readonly currentUser = this.auth.currentUser;
  protected readonly isAuthenticated = computed(() => this.currentUser() !== null);
  protected readonly primaryRole = computed(() => this.currentUser()?.roles?.[0] ?? '');
  protected readonly userInitial = computed(
    () => this.currentUser()?.fullName?.charAt(0).toUpperCase() ?? '?'
  );

  protected readonly unreadCount = this.notif.unreadCount;

  protected readonly collapsed = signal(localStorage.getItem(SIDEBAR_KEY) === '1');
  protected readonly mobileNavOpen = signal(false);
  protected readonly notifOpen = signal(false);

  // Global arama
  protected readonly searchTerm = signal('');
  protected readonly searchOpen = signal(false);
  protected readonly searchActive = signal(-1);

  // Sonuçları kategoriye göre grupla (aynı sıra korunur → klavye index eşleşir)
  protected readonly results = computed<SearchResult[]>(() => {
    const term = this.searchTerm().trim();
    if (!term) return [];
    return [...this.searchSvc.search(term)].sort((a, b) => a.category.localeCompare(b.category, 'tr'));
  });
  protected readonly recentSearches = this.searchHistory.recent;
  protected readonly pinnedSearches = this.searchHistory.pinned;
  protected readonly suggestions = this.searchHistory.suggestions;

  protected readonly showResults = computed(
    () => this.searchOpen() && this.searchTerm().trim().length > 0
  );
  protected readonly showEmptyPanel = computed(
    () => this.searchOpen() && !this.searchTerm().trim()
  );

  showSearchHeader(index: number): boolean {
    const r = this.results();
    return index === 0 || r[index].category !== r[index - 1].category;
  }

  /** Bold-highlight the matched substring (own trusted data). */
  highlight(label: string): string {
    const term = this.searchTerm().trim();
    if (!term) return label;
    const i = label.toLocaleLowerCase('tr').indexOf(term.toLocaleLowerCase('tr'));
    if (i < 0) return label;
    return (
      label.slice(0, i) + '<mark>' + label.slice(i, i + term.length) + '</mark>' + label.slice(i + term.length)
    );
  }

  onSearchKeydown(event: KeyboardEvent): void {
    const list = this.results();
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.searchActive.update((i) => Math.min(i + 1, list.length - 1));
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.searchActive.update((i) => Math.max(i - 1, 0));
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const idx = this.searchActive();
      const chosen = idx >= 0 ? list[idx] : list[0];
      if (chosen) this.onSelectResult(chosen);
    } else if (event.key === 'Escape') {
      this.searchOpen.set(false);
    }
  }

  applyRecent(term: string): void {
    this.searchTerm.set(term);
    this.searchActive.set(-1);
  }

  clearHistory(): void {
    this.searchHistory.clear();
  }

  // Bildirim önizleme (topbar paneli)
  protected readonly notifRows = computed<NotificationRow[]>(() =>
    this.notif.notifications().slice(0, 6).map((n) => ({
      id: n.id,
      categoryLabel: NOTIFICATION_META[n.kind].label,
      badgeClass: NOTIFICATION_META[n.kind].badge,
      priority: n.priority,
      title: n.title,
      detail: n.detail,
      time: n.time,
      read: n.read,
      actionLabel: n.action?.label,
      actionRoute: n.action?.route
    }))
  );

  toggleLang(): void {
    this.language.toggle();
  }

  toggleSidebar(): void {
    this.collapsed.update((v) => {
      const next = !v;
      localStorage.setItem(SIDEBAR_KEY, next ? '1' : '0');
      return next;
    });
  }

  onSearchFocus(): void {
    this.searchOpen.set(true);
    this.searchActive.set(-1);
  }

  onSearchBlur(): void {
    setTimeout(() => this.searchOpen.set(false), 150);
  }

  onSelectResult(result: SearchResult): void {
    this.searchHistory.add(this.searchTerm());
    if (result.route) {
      this.router.navigateByUrl(result.route);
    }
    this.searchTerm.set('');
    this.searchActive.set(-1);
    this.searchOpen.set(false);
  }

  markAllRead(): void {
    this.notif.markAllRead();
  }

  onNotifRead(id: string): void {
    this.notif.markRead(id);
  }

  goToNotifications(): void {
    this.notifOpen.set(false);
    this.router.navigateByUrl('/notifications');
  }

  navigateMobile(route: string): void {
    this.mobileNavOpen.set(false);
    this.router.navigateByUrl(route);
  }

  logout(): void {
    // Clear the session (best-effort backend revoke) then go to login.
    this.authState.logout().subscribe({ complete: () => this.router.navigate(['/login']) });
  }

  logoutAll(): void {
    this.authState.logoutAll().subscribe({ complete: () => this.router.navigate(['/login']) });
  }
}

import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { toObservable, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, switchMap, catchError, of } from 'rxjs';
import { TranslocoPipe, provideTranslocoScope } from '@jsverse/transloco';
import { ChangeApiService } from '../../core/change/change-api.service';
import { ChangeRequestListItem, ChangeListQuery } from '../../core/change/change.models';
import {
  CHANGE_CLASS_VALUES, CHANGE_TYPE_VALUES, CHANGE_STATUS_VALUES, CHANGE_RISK_VALUES,
  changeClassKey, changeTypeKey
} from '../../core/change/change-labels';
import { ReferenceDataApiService, CustomerRef, ProjectRef, EnvironmentRef } from '../../core/reference/reference-data-api.service';
import { ApiError } from '../../core/api-error';
import { HasPermissionDirective } from '../../core/auth/has-permission.directive';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsDataGrid, GmsCellDef, ColumnDef } from '../../shared/ui/data-grid/data-grid';
import { GmsState } from '../../shared/ui/state/state';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { STATUS_BADGES, RISK_BADGES } from '../../shared/ui/badge/badge';

/**
 * Change list — backed by the real GET /api/change-requests (paged envelope). Filters, sort and
 * paging are all server-side; the data-grid is used purely to render the current page (its internal
 * paging/search are disabled). The filter/sort/page signals feed one switchMap stream so stale
 * requests are cancelled and search is debounced.
 */
@Component({
  selector: 'app-change-list',
  imports: [
    FormsModule, DatePipe, TranslocoPipe, HasPermissionDirective,
    GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsDataGrid, GmsCellDef, GmsState, GmsContextSection
  ],
  providers: [provideTranslocoScope('changes')],
  templateUrl: './change-list.html',
  styleUrl: './change-list.scss'
})
export class ChangeList implements OnInit {
  private readonly api = inject(ChangeApiService);
  private readonly ref = inject(ReferenceDataApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly classOptions = CHANGE_CLASS_VALUES;
  protected readonly typeOptions = CHANGE_TYPE_VALUES;
  protected readonly statusOptions = CHANGE_STATUS_VALUES;
  protected readonly riskOptions = CHANGE_RISK_VALUES;
  protected readonly classLabelKey = changeClassKey;
  protected readonly typeLabelKey = changeTypeKey;
  protected readonly statusLabelKey = (s: string) => STATUS_BADGES[s]?.labelKey ?? s;
  protected readonly riskLabelKey = (r: string) => RISK_BADGES[r]?.labelKey ?? r;

  // ── Filters (server-side) ──
  protected readonly fSearch = signal('');
  protected readonly fCustomer = signal('');
  protected readonly fProject = signal('');
  protected readonly fEnvironment = signal('');
  protected readonly fStatus = signal('');
  protected readonly fClass = signal('');
  protected readonly fType = signal('');
  protected readonly fRisk = signal('');
  protected readonly sortBy = signal('createdAt');
  protected readonly sortDir = signal<'asc' | 'desc'>('desc');
  protected readonly page = signal(1);
  protected readonly pageSize = 20;

  // ── Reference data (dependent chain) ──
  protected readonly customers = signal<CustomerRef[]>([]);
  protected readonly projects = signal<ProjectRef[]>([]);
  protected readonly environments = signal<EnvironmentRef[]>([]);

  // ── Result state ──
  protected readonly rows = signal<ChangeRequestListItem[]>([]);
  protected readonly total = signal(0);
  protected readonly totalPages = signal(0);
  protected readonly loading = signal(false);
  protected readonly error = signal<ApiError | null>(null);

  protected readonly hasFilters = computed(() =>
    !!(this.fSearch() || this.fCustomer() || this.fProject() || this.fEnvironment()
      || this.fStatus() || this.fClass() || this.fType() || this.fRisk()));

  protected readonly isEmpty = computed(() => !this.loading() && !this.error() && this.rows().length === 0);
  protected readonly rangeStart = computed(() => this.total() === 0 ? 0 : (this.page() - 1) * this.pageSize + 1);
  protected readonly rangeEnd = computed(() => Math.min(this.page() * this.pageSize, this.total()));

  private readonly query = computed<ChangeListQuery>(() => ({
    search: this.fSearch().trim() || undefined,
    customerId: this.fCustomer() || undefined,
    projectId: this.fProject() || undefined,
    environmentId: this.fEnvironment() || undefined,
    status: this.fStatus() || undefined,
    changeClass: this.fClass() || undefined,
    changeType: this.fType() || undefined,
    riskLevel: this.fRisk() || undefined,
    sortBy: this.sortBy(),
    sortDir: this.sortDir(),
    page: this.page(),
    pageSize: this.pageSize
  }));

  protected readonly columns = computed<ColumnDef[]>(() => [
    { key: 'changeNo', header: 'Değişiklik No', sticky: true, width: '150px' },
    { key: 'title', header: 'Başlık' },
    { key: 'changeClass', header: 'Sınıf' },
    { key: 'changeType', header: 'Tür' },
    { key: 'projectName', header: 'Proje' },
    { key: 'environmentName', header: 'Ortam', type: 'badge', badgeKind: 'environment' },
    { key: 'riskLevel', header: 'Risk', type: 'badge', badgeKind: 'risk' },
    { key: 'priority', header: 'Öncelik', type: 'badge', badgeKind: 'priority' },
    { key: 'status', header: 'Durum', type: 'badge', badgeKind: 'status' },
    { key: 'createdByUserName', header: 'Oluşturan' },
    { key: 'createdAt', header: 'Oluşturulma', type: 'date' },
    { key: 'updatedAt', header: 'Güncelleme', type: 'date' }
  ]);

  constructor() {
    // Restore filters from the URL (deep-link friendly).
    const q = this.route.snapshot.queryParamMap;
    this.fSearch.set(q.get('search') ?? '');
    this.fCustomer.set(q.get('customerId') ?? '');
    this.fProject.set(q.get('projectId') ?? '');
    this.fEnvironment.set(q.get('environmentId') ?? '');
    this.fStatus.set(q.get('status') ?? '');
    this.fClass.set(q.get('changeClass') ?? '');
    this.fType.set(q.get('changeType') ?? '');
    this.fRisk.set(q.get('riskLevel') ?? '');
    this.page.set(Number(q.get('page')) || 1);

    // Single switchMap stream: debounce, drop duplicates, cancel stale requests.
    toObservable(this.query).pipe(
      debounceTime(250),
      distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
      switchMap((query) => {
        this.loading.set(true);
        this.error.set(null);
        this.syncUrl(query);
        return this.api.list(query).pipe(
          catchError((e: ApiError) => { this.error.set(e); return of(null); })
        );
      }),
      takeUntilDestroyed()
    ).subscribe((res) => {
      this.loading.set(false);
      if (res) {
        this.rows.set(res.items);
        this.total.set(res.totalCount);
        this.totalPages.set(res.totalPages);
      } else {
        this.rows.set([]);
        this.total.set(0);
        this.totalPages.set(0);
      }
    });
  }

  ngOnInit(): void {
    this.ref.customers().subscribe({ next: (c) => this.customers.set(c), error: () => this.customers.set([]) });
    if (this.fCustomer()) this.loadProjects(this.fCustomer());
    if (this.fProject()) this.loadEnvironments(this.fProject());
  }

  // ── Filter change handlers (reset page + dependent chain) ──
  onSearch(v: string): void { this.fSearch.set(v); this.page.set(1); }
  onStatus(v: string): void { this.fStatus.set(v); this.page.set(1); }
  onClass(v: string): void { this.fClass.set(v); this.page.set(1); }
  onType(v: string): void { this.fType.set(v); this.page.set(1); }
  onRisk(v: string): void { this.fRisk.set(v); this.page.set(1); }

  onCustomer(v: string): void {
    this.fCustomer.set(v);
    this.fProject.set('');
    this.fEnvironment.set('');
    this.projects.set([]);
    this.environments.set([]);
    this.page.set(1);
    if (v) this.loadProjects(v);
  }

  onProject(v: string): void {
    this.fProject.set(v);
    this.fEnvironment.set('');
    this.environments.set([]);
    this.page.set(1);
    if (v) this.loadEnvironments(v);
  }

  onEnvironment(v: string): void { this.fEnvironment.set(v); this.page.set(1); }

  setSort(by: string): void {
    if (this.sortBy() === by) {
      this.sortDir.update((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortBy.set(by);
      this.sortDir.set('desc');
    }
    this.page.set(1);
  }

  prevPage(): void { if (this.page() > 1) this.page.update((p) => p - 1); }
  nextPage(): void { if (this.page() < this.totalPages()) this.page.update((p) => p + 1); }

  resetFilters(): void {
    this.fSearch.set(''); this.fCustomer.set(''); this.fProject.set(''); this.fEnvironment.set('');
    this.fStatus.set(''); this.fClass.set(''); this.fType.set(''); this.fRisk.set('');
    this.projects.set([]); this.environments.set([]);
    this.page.set(1);
  }

  reload(): void {
    // Force a re-fetch by nudging the query (page stays); simplest is to re-run current query.
    const p = this.page();
    this.page.set(0); this.page.set(p);
  }

  openChange(row: ChangeRequestListItem): void { this.router.navigate(['/changes', row.id]); }
  newChange(): void { this.router.navigate(['/changes/new']); }

  private loadProjects(customerId: string): void {
    this.ref.projects(customerId).subscribe({ next: (p) => this.projects.set(p), error: () => this.projects.set([]) });
  }
  private loadEnvironments(projectId: string): void {
    this.ref.environments(projectId).subscribe({ next: (e) => this.environments.set(e), error: () => this.environments.set([]) });
  }

  private syncUrl(query: ChangeListQuery): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: query.search ?? null,
        customerId: query.customerId ?? null,
        projectId: query.projectId ?? null,
        environmentId: query.environmentId ?? null,
        status: query.status ?? null,
        changeClass: query.changeClass ?? null,
        changeType: query.changeType ?? null,
        riskLevel: query.riskLevel ?? null,
        page: query.page && query.page > 1 ? query.page : null
      },
      replaceUrl: true
    });
  }
}

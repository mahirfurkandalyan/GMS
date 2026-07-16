import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { toObservable, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, switchMap, catchError, of } from 'rxjs';
import { TranslocoPipe, provideTranslocoScope } from '@jsverse/transloco';
import { ReleaseApiService } from '../../core/release/release-api.service';
import { ReleasePlanListItem, ReleaseListQuery } from '../../core/release/release.models';
import { RELEASE_STATUS_VALUES, releaseTypeKey } from '../../core/release/release-labels';
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
import { STATUS_BADGES } from '../../shared/ui/badge/badge';

/**
 * Release list — backed by the real GET /api/releases (paged envelope). Customer/project/environment/
 * status/search filters, sort and paging are all server-side (the backend does not support release-type
 * or date filters, so those are not offered as server filters). Filter/sort/page signals feed one
 * switchMap stream so stale requests are cancelled and search is debounced.
 */
@Component({
  selector: 'app-release-list',
  imports: [
    FormsModule, DatePipe, TranslocoPipe, HasPermissionDirective,
    GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsDataGrid, GmsCellDef, GmsState, GmsContextSection
  ],
  providers: [provideTranslocoScope('releases')],
  templateUrl: './release-list.html',
  styleUrl: './release-list.scss'
})
export class ReleaseList implements OnInit {
  private readonly api = inject(ReleaseApiService);
  private readonly ref = inject(ReferenceDataApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly statusOptions = RELEASE_STATUS_VALUES;
  protected readonly typeLabelKey = releaseTypeKey;
  protected readonly statusLabelKey = (s: string) => STATUS_BADGES[s]?.labelKey ?? s;

  // ── Filters (server-side) ──
  protected readonly fSearch = signal('');
  protected readonly fCustomer = signal('');
  protected readonly fProject = signal('');
  protected readonly fEnvironment = signal('');
  protected readonly fStatus = signal('');
  protected readonly sortBy = signal('createdAt');
  protected readonly sortDir = signal<'asc' | 'desc'>('desc');
  protected readonly page = signal(1);
  protected readonly pageSize = 20;

  // ── Reference data (dependent chain) ──
  protected readonly customers = signal<CustomerRef[]>([]);
  protected readonly projects = signal<ProjectRef[]>([]);
  protected readonly environments = signal<EnvironmentRef[]>([]);

  // ── Result state ──
  protected readonly rows = signal<ReleasePlanListItem[]>([]);
  protected readonly total = signal(0);
  protected readonly totalPages = signal(0);
  protected readonly loading = signal(false);
  protected readonly error = signal<ApiError | null>(null);

  protected readonly hasFilters = computed(() =>
    !!(this.fSearch() || this.fCustomer() || this.fProject() || this.fEnvironment() || this.fStatus()));
  protected readonly isEmpty = computed(() => !this.loading() && !this.error() && this.rows().length === 0);
  protected readonly rangeStart = computed(() => this.total() === 0 ? 0 : (this.page() - 1) * this.pageSize + 1);
  protected readonly rangeEnd = computed(() => Math.min(this.page() * this.pageSize, this.total()));

  private readonly query = computed<ReleaseListQuery>(() => ({
    search: this.fSearch().trim() || undefined,
    customerId: this.fCustomer() || undefined,
    projectId: this.fProject() || undefined,
    environmentId: this.fEnvironment() || undefined,
    status: this.fStatus() || undefined,
    sortBy: this.sortBy(),
    sortDir: this.sortDir(),
    page: this.page(),
    pageSize: this.pageSize
  }));

  protected readonly columns = computed<ColumnDef[]>(() => [
    { key: 'releaseNo', header: 'Yayın No', sticky: true, width: '150px' },
    { key: 'name', header: 'Ad' },
    { key: 'customerName', header: 'Müşteri' },
    { key: 'projectName', header: 'Proje' },
    { key: 'environmentName', header: 'Ortam', type: 'badge', badgeKind: 'environment' },
    { key: 'releaseType', header: 'Tür' },
    { key: 'status', header: 'Durum', type: 'badge', badgeKind: 'status' },
    { key: 'riskLevel', header: 'Risk', type: 'badge', badgeKind: 'risk' },
    { key: 'changeCount', header: 'Değişiklik', align: 'right' },
    { key: 'plannedDeploymentStart', header: 'Planlanan', type: 'date' },
    { key: 'releaseManagerName', header: 'Yayın Yöneticisi' },
    { key: 'createdAt', header: 'Oluşturulma', type: 'date' }
  ]);

  constructor() {
    const q = this.route.snapshot.queryParamMap;
    this.fSearch.set(q.get('search') ?? '');
    this.fCustomer.set(q.get('customerId') ?? '');
    this.fProject.set(q.get('projectId') ?? '');
    this.fEnvironment.set(q.get('environmentId') ?? '');
    this.fStatus.set(q.get('status') ?? '');
    this.page.set(Number(q.get('page')) || 1);

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
        this.rows.set([]); this.total.set(0); this.totalPages.set(0);
      }
    });
  }

  ngOnInit(): void {
    this.ref.customers().subscribe({ next: (c) => this.customers.set(c), error: () => this.customers.set([]) });
    if (this.fCustomer()) this.loadProjects(this.fCustomer());
    if (this.fProject()) this.loadEnvironments(this.fProject());
  }

  onSearch(v: string): void { this.fSearch.set(v); this.page.set(1); }
  onStatus(v: string): void { this.fStatus.set(v); this.page.set(1); }

  onCustomer(v: string): void {
    this.fCustomer.set(v);
    this.fProject.set(''); this.fEnvironment.set('');
    this.projects.set([]); this.environments.set([]);
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

  prevPage(): void { if (this.page() > 1) this.page.update((p) => p - 1); }
  nextPage(): void { if (this.page() < this.totalPages()) this.page.update((p) => p + 1); }

  resetFilters(): void {
    this.fSearch.set(''); this.fCustomer.set(''); this.fProject.set(''); this.fEnvironment.set(''); this.fStatus.set('');
    this.projects.set([]); this.environments.set([]);
    this.page.set(1);
  }

  reload(): void { const p = this.page(); this.page.set(0); this.page.set(p); }

  openRelease(row: ReleasePlanListItem): void { this.router.navigate(['/releases', row.id]); }
  newRelease(): void { this.router.navigate(['/releases/new']); }

  private loadProjects(customerId: string): void {
    this.ref.projects(customerId).subscribe({ next: (p) => this.projects.set(p), error: () => this.projects.set([]) });
  }
  private loadEnvironments(projectId: string): void {
    this.ref.environments(projectId).subscribe({ next: (e) => this.environments.set(e), error: () => this.environments.set([]) });
  }

  private syncUrl(query: ReleaseListQuery): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: query.search ?? null,
        customerId: query.customerId ?? null,
        projectId: query.projectId ?? null,
        environmentId: query.environmentId ?? null,
        status: query.status ?? null,
        page: query.page && query.page > 1 ? query.page : null
      },
      replaceUrl: true
    });
  }
}

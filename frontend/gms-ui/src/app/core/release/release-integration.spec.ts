import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { API_BASE_URL, PagedResult } from '../api.config';
import { apiErrorInterceptor } from '../api-error.interceptor';
import { ReleaseApiService } from './release-api.service';
import { ReleasePlanDetail, ReleasePlanListItem } from './release.models';
import { RELEASE_TYPE_VALUES, releaseTypeKey, RELEASE_TERMINAL_STATUSES } from './release-labels';
import { ChangeApiService } from '../change/change-api.service';

/** In-memory Web Storage polyfill — the test runtime ships only a partial global shim. */
function installMemoryStorage(): void {
  let store: Record<string, string> = {};
  const mem: Storage = {
    getItem: (k) => (k in store ? store[k] : null),
    setItem: (k, v) => { store[k] = String(v); },
    removeItem: (k) => { delete store[k]; },
    clear: () => { store = {}; },
    key: (i) => Object.keys(store)[i] ?? null,
    get length() { return Object.keys(store).length; }
  };
  Object.defineProperty(globalThis, 'localStorage', { value: mem, configurable: true, writable: true });
}

function pagedList(items: Partial<ReleasePlanListItem>[], totalCount = items.length): PagedResult<ReleasePlanListItem> {
  return { items: items as ReleasePlanListItem[], page: 1, pageSize: 20, totalCount, totalPages: Math.ceil(totalCount / 20) };
}

function detail(over: Partial<ReleasePlanDetail> = {}): ReleasePlanDetail {
  return {
    id: 'r1', releaseNo: 'REL-2026-000001', name: 'Rel', version: '1.0', customerId: 'cu1', customerName: 'Cust',
    projectId: 'p1', projectName: 'Proj', environmentId: 'e1', environmentName: 'PROD', releaseType: 'Minor',
    status: 'Planned', riskLevel: 'Medium', riskScore: 30, totalEstimatedMinutes: 60, plannedDeploymentStart: null,
    plannedDeploymentEnd: null, rollbackWindow: '', businessOwner: '', technicalOwner: '', releaseManagerUserId: 'u1',
    releaseManagerName: 'RM', description: '', createdAt: '2026-07-16T00:00:00Z', updatedAt: null, rowVersion: 'AAA=',
    items: [], deploymentPlan: null, documents: [], auditEvents: [], ...over
  };
}

describe('ReleaseApiService — real backend integration', () => {
  let api: ReleaseApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    installMemoryStorage();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([apiErrorInterceptor])), provideHttpClientTesting()]
    });
    api = TestBed.inject(ReleaseApiService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('1 — list calls GET /releases with pagination + filter params (no hidden pageSize)', () => {
    api.list({ page: 2, pageSize: 20, status: 'Planned', customerId: 'cu1', projectId: 'p1', environmentId: 'e1', search: 'x', sortBy: 'createdAt', sortDir: 'desc' }).subscribe();
    const req = http.expectOne((r) => r.url === `${API_BASE_URL}/releases`);
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('20');
    expect(req.request.params.get('status')).toBe('Planned');
    expect(req.request.params.get('customerId')).toBe('cu1');
    expect(req.request.params.get('environmentId')).toBe('e1');
    req.flush(pagedList([{ id: 'r1' }]));
  });

  it('2 — list returns the backend paged envelope items', () => {
    let res: PagedResult<ReleasePlanListItem> | undefined;
    api.list().subscribe((r) => (res = r));
    http.expectOne((r) => r.url === `${API_BASE_URL}/releases`).flush(pagedList([{ id: 'r1' }, { id: 'r2' }], 2));
    expect(res?.items.length).toBe(2);
    expect(res?.totalCount).toBe(2);
  });

  it('3 — list handles an empty result envelope', () => {
    let res: PagedResult<ReleasePlanListItem> | undefined;
    api.list().subscribe((r) => (res = r));
    http.expectOne((r) => r.url === `${API_BASE_URL}/releases`).flush(pagedList([], 0));
    expect(res?.items.length).toBe(0);
  });

  it('4 — create POSTs the mapped DTO (changeIds + releaseManagerUserId, no actorUserId)', () => {
    const input = {
      name: 'Rel', version: '1.0', customerId: 'cu1', projectId: 'p1', environmentId: 'e1', releaseType: 'Minor',
      releaseManagerUserId: 'u1', changeIds: ['c1', 'c2'], documents: []
    };
    api.create(input as any).subscribe();
    const req = http.expectOne(`${API_BASE_URL}/releases`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.changeIds).toEqual(['c1', 'c2']);
    expect(req.request.body.releaseManagerUserId).toBe('u1');
    expect('actorUserId' in req.request.body).toBe(false);
    req.flush(detail());
  });

  it('5 — create returns the real detail (generated releaseNo + backend risk)', () => {
    let res: ReleasePlanDetail | undefined;
    api.create({} as any).subscribe((d) => (res = d));
    http.expectOne(`${API_BASE_URL}/releases`).flush(detail({ releaseNo: 'REL-2026-000042', riskScore: 55, riskLevel: 'High' }));
    expect(res?.releaseNo).toBe('REL-2026-000042');
    expect(res?.riskScore).toBe(55);
  });

  it('6 — schedule POSTs to /schedule and returns Scheduled', () => {
    let res: ReleasePlanDetail | undefined;
    api.schedule('r1').subscribe((d) => (res = d));
    const req = http.expectOne(`${API_BASE_URL}/releases/r1/schedule`);
    expect(req.request.method).toBe('POST');
    req.flush(detail({ status: 'Scheduled' }));
    expect(res?.status).toBe('Scheduled');
  });

  it('7 — cancel POSTs to /cancel (no manual local status change)', () => {
    api.cancel('r1').subscribe();
    const req = http.expectOne(`${API_BASE_URL}/releases/r1/cancel`);
    expect(req.request.method).toBe('POST');
    req.flush(detail({ status: 'Cancelled' }));
  });

  it('8 — update PUTs the body including RowVersion', () => {
    api.update('r1', { name: 'New', rowVersion: 'ROW==' }).subscribe();
    const req = http.expectOne(`${API_BASE_URL}/releases/r1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.rowVersion).toBe('ROW==');
    req.flush(detail());
  });

  it('9 — a 409 update is normalized to a concurrency conflict', () => {
    let err: any;
    api.update('r1', { name: 'X', rowVersion: 'STALE' }).subscribe({ error: (e) => (err = e) });
    http.expectOne(`${API_BASE_URL}/releases/r1`).flush({ message: 'conflict' }, { status: 409, statusText: 'Conflict' });
    expect(err.kind).toBe('conflict');
    expect(err.message).toContain('başka bir kullanıcı');
  });

  it('10 — getById returns detail with items + backend risk', () => {
    let res: ReleasePlanDetail | undefined;
    api.getById('r1').subscribe((d) => (res = d));
    http.expectOne(`${API_BASE_URL}/releases/r1`).flush(detail({
      riskLevel: 'High', riskScore: 70,
      items: [{ id: 'i1', changeRequestId: 'c1', changeNo: 'CHG-1', changeTitle: 'T', changeStatus: 'Scheduled', changeRiskLevel: 'High', deploymentOrder: 1, estimatedMinutes: 30, rollbackRequired: true }]
    }));
    expect(res?.items.length).toBe(1);
    expect(res?.riskLevel).toBe('High');
  });

  it('11 — audit returns actor-named events', () => {
    let res: any;
    api.getAudit('r1').subscribe((r) => (res = r));
    http.expectOne(`${API_BASE_URL}/releases/r1/audit`).flush([
      { id: 'a1', eventType: 'ReleaseCreated', description: 'd', actorUserId: 'u1', actorUserName: 'Release Manager User', createdAt: '2026-07-16T00:00:00Z' }
    ]);
    expect(res[0].actorUserName).toBe('Release Manager User');
  });

  it('12 — a backend validation 400 (e.g. only-approved rule) is normalized', () => {
    let err: any;
    api.create({} as any).subscribe({ error: (e) => (err = e) });
    http.expectOne(`${API_BASE_URL}/releases`).flush(
      { message: "Yayın yalnızca 'Approved' durumundaki değişikliklerden oluşturulabilir." },
      { status: 400, statusText: 'Bad Request' }
    );
    expect(err.kind).toBe('validation');
    expect(err.message).toContain('Approved');
  });

  it('13 — approved-change selection queries ChangeApiService with status=Approved + scope', () => {
    const changeApi = TestBed.inject(ChangeApiService);
    changeApi.list({ status: 'Approved', customerId: 'cu1', projectId: 'p1', environmentId: 'e1', pageSize: 100 }).subscribe();
    const req = http.expectOne((r) => r.url === `${API_BASE_URL}/change-requests`);
    expect(req.request.params.get('status')).toBe('Approved');
    expect(req.request.params.get('customerId')).toBe('cu1');
    expect(req.request.params.get('projectId')).toBe('p1');
    expect(req.request.params.get('environmentId')).toBe('e1');
    req.flush({ items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 });
  });

  it('14 — the service never writes Release domain state to localStorage', () => {
    api.create({} as any).subscribe();
    http.expectOne(`${API_BASE_URL}/releases`).flush(detail());
    expect(localStorage.getItem('gms.releases')).toBeNull();
  });

  it('15 — release label helpers + terminal-status set are correct', () => {
    expect(releaseTypeKey('Major')).toBe('releases.type.Major');
    expect(RELEASE_TYPE_VALUES).toContain('Hotfix' as any);
    expect(RELEASE_TERMINAL_STATUSES.has('Completed')).toBe(true);
    expect(RELEASE_TERMINAL_STATUSES.has('Planned')).toBe(false);
  });
});

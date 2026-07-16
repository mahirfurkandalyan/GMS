import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { API_BASE_URL } from '../api.config';
import { apiErrorInterceptor } from '../api-error.interceptor';
import { normalizeHttpError } from '../api-error';
import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { ChangeApiService } from './change-api.service';
import { ChangeRequestDetail, ChangeRequestListItem } from './change.models';
import { KEBAB_TO_CHANGE_TYPE, changeTypeKey, CHANGE_TYPE_VALUES } from './change-labels';
import { PagedResult } from '../api.config';

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

function pagedList(items: Partial<ChangeRequestListItem>[], totalCount = items.length): PagedResult<ChangeRequestListItem> {
  return { items: items as ChangeRequestListItem[], page: 1, pageSize: 20, totalCount, totalPages: Math.ceil(totalCount / 20) };
}

function detail(over: Partial<ChangeRequestDetail> = {}): ChangeRequestDetail {
  return {
    id: 'c1', changeNo: 'CHG-2026-000001', title: 'T', customerName: 'Cust', projectName: 'Proj', environmentName: 'PROD',
    changeClass: 'Standard', changeType: 'ConfigurationChange', priority: 'Low', status: 'Draft', riskLevel: 'Medium', riskScore: 30,
    plannedImplementationDate: null, createdByUserName: 'Req', createdAt: '2026-07-16T00:00:00Z', updatedAt: null,
    description: '', businessReason: 'BR', customerId: 'cu1', projectId: 'p1', environmentId: 'e1', plannedRollbackDate: null,
    sourceSystem: null, sourceReference: null, createdByUserId: 'u1', rowVersion: 'AAA=', latestRevision: null,
    assets: [], documents: [], auditEvents: [], readiness: { readinessScore: 50, findings: [] }, ...over
  };
}

describe('ChangeApiService — real backend integration', () => {
  let api: ChangeApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    installMemoryStorage();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([apiErrorInterceptor])),
        provideHttpClientTesting()
      ]
    });
    api = TestBed.inject(ChangeApiService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('1 — list calls GET /change-requests with pagination + filter params (no hidden pageSize=100)', () => {
    api.list({ page: 2, pageSize: 20, status: 'Draft', changeClass: 'Standard', search: 'abc', sortBy: 'createdAt', sortDir: 'desc' }).subscribe();
    const req = http.expectOne((r) => r.url === `${API_BASE_URL}/change-requests`);
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('20');
    expect(req.request.params.get('status')).toBe('Draft');
    expect(req.request.params.get('changeClass')).toBe('Standard');
    expect(req.request.params.get('search')).toBe('abc');
    expect(req.request.params.get('sortDir')).toBe('desc');
    req.flush(pagedList([{ id: 'c1', changeNo: 'CHG-1' }]));
  });

  it('2 — list returns the backend paged envelope items', () => {
    let result: PagedResult<ChangeRequestListItem> | undefined;
    api.list().subscribe((r) => (result = r));
    http.expectOne((r) => r.url === `${API_BASE_URL}/change-requests`).flush(pagedList([{ id: 'c1' }, { id: 'c2' }], 2));
    expect(result?.items.length).toBe(2);
    expect(result?.totalCount).toBe(2);
  });

  it('3 — list handles an empty result envelope', () => {
    let result: PagedResult<ChangeRequestListItem> | undefined;
    api.list().subscribe((r) => (result = r));
    http.expectOne((r) => r.url === `${API_BASE_URL}/change-requests`).flush(pagedList([], 0));
    expect(result?.items.length).toBe(0);
    expect(result?.totalCount).toBe(0);
  });

  it('4 — create POSTs the mapped backend DTO (PascalCase enums, no actorUserId)', () => {
    const input = {
      title: 'T', businessReason: 'BR', customerId: 'cu1', projectId: 'p1', environmentId: 'e1',
      changeClass: 'Standard', changeType: KEBAB_TO_CHANGE_TYPE['config'], priority: 'Low',
      revision: { technicalSummary: 'x' }, assets: [], documents: []
    };
    api.create(input as any).subscribe();
    const req = http.expectOne(`${API_BASE_URL}/change-requests`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.changeType).toBe('ConfigurationChange');
    expect(req.request.body.changeClass).toBe('Standard');
    expect('actorUserId' in req.request.body).toBe(false);
    req.flush(detail());
  });

  it('5 — create returns the real detail (with generated changeNo + backend risk)', () => {
    let res: ChangeRequestDetail | undefined;
    api.create({} as any).subscribe((d) => (res = d));
    http.expectOne(`${API_BASE_URL}/change-requests`).flush(detail({ changeNo: 'CHG-2026-000099', riskScore: 45, riskLevel: 'High' }));
    expect(res?.changeNo).toBe('CHG-2026-000099');
    expect(res?.riskScore).toBe(45);
  });

  it('6 — submit POSTs to /submit and returns UnderReview', () => {
    let res: ChangeRequestDetail | undefined;
    api.submit('c1').subscribe((d) => (res = d));
    const req = http.expectOne(`${API_BASE_URL}/change-requests/c1/submit`);
    expect(req.request.method).toBe('POST');
    req.flush(detail({ status: 'UnderReview' }));
    expect(res?.status).toBe('UnderReview');
  });

  it('7 — submit readiness 400 is normalized with structured findings', () => {
    let err: any;
    api.submit('c1').subscribe({ error: (e) => (err = e) });
    http.expectOne(`${API_BASE_URL}/change-requests/c1/submit`).flush(
      { message: 'Kritik hazırlık bulguları', readinessScore: 40, findings: [{ code: 'ROLLBACK_MISSING', severity: 'Critical', message: 'm', recommendation: 'r' }] },
      { status: 400, statusText: 'Bad Request' }
    );
    expect(err.kind).toBe('validation');
    expect(err.readinessFindings.length).toBe(1);
    expect(err.readinessFindings[0].code).toBe('ROLLBACK_MISSING');
  });

  it('8 — getById returns real risk + readiness', () => {
    let res: ChangeRequestDetail | undefined;
    api.getById('c1').subscribe((d) => (res = d));
    http.expectOne(`${API_BASE_URL}/change-requests/c1`).flush(detail({ riskLevel: 'High', riskScore: 60, readiness: { readinessScore: 80, findings: [] } }));
    expect(res?.riskLevel).toBe('High');
    expect(res?.readiness.readinessScore).toBe(80);
  });

  it('9 — update PUTs the body including RowVersion', () => {
    api.update('c1', { title: 'New', rowVersion: 'ROW==' }).subscribe();
    const req = http.expectOne(`${API_BASE_URL}/change-requests/c1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.rowVersion).toBe('ROW==');
    req.flush(detail());
  });

  it('10 — a 409 update is normalized to a concurrency conflict', () => {
    let err: any;
    api.update('c1', { title: 'X', rowVersion: 'STALE' }).subscribe({ error: (e) => (err = e) });
    http.expectOne(`${API_BASE_URL}/change-requests/c1`).flush({ message: 'conflict' }, { status: 409, statusText: 'Conflict' });
    expect(err.kind).toBe('conflict');
    expect(err.message).toContain('başka bir kullanıcı');
  });

  it('11 — addRevision POSTs to /revisions and returns updated detail', () => {
    let res: ChangeRequestDetail | undefined;
    api.addRevision('c1', { technicalSummary: 'ts', rollbackScript: 'rb' }).subscribe((d) => (res = d));
    const req = http.expectOne(`${API_BASE_URL}/change-requests/c1/revisions`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.technicalSummary).toBe('ts');
    req.flush(detail({ latestRevision: { id: 'r2', revisionNo: 2, technicalSummary: 'ts', implementationNotes: '', deploymentInstructions: '', sqlScript: '', rollbackScript: 'rb', rollbackStrategy: '', rollbackOwner: '', estimatedDurationMinutes: 0, createdByUserId: 'u1', createdAt: '2026-07-16T00:00:00Z' } }));
    expect(res?.latestRevision?.revisionNo).toBe(2);
  });

  it('12 — cancel POSTs to /cancel (no manual local status change)', () => {
    api.cancel('c1').subscribe();
    const req = http.expectOne(`${API_BASE_URL}/change-requests/c1/cancel`);
    expect(req.request.method).toBe('POST');
    req.flush(detail({ status: 'Cancelled' }));
  });

  it('13 — audit endpoint returns actor-named events', () => {
    let res: any;
    api.getAudit('c1').subscribe((r) => (res = r));
    http.expectOne(`${API_BASE_URL}/change-requests/c1/audit`).flush([
      { id: 'a1', eventType: 'ChangeCreated', description: 'd', actorUserId: 'u1', actorUserName: 'Requester User', createdAt: '2026-07-16T00:00:00Z' }
    ]);
    expect(res[0].actorUserName).toBe('Requester User');
  });

  it('14 — the service never writes Change domain state to localStorage', () => {
    const before = { ...localStorage };
    api.create({} as any).subscribe();
    http.expectOne(`${API_BASE_URL}/change-requests`).flush(detail());
    expect(localStorage.getItem('gms.changes')).toBeNull();
    expect(Object.keys(localStorage).length).toBe(Object.keys(before).length);
  });

  it('15 — kebab→backend change-type mapping is complete and valid', () => {
    for (const backend of Object.values(KEBAB_TO_CHANGE_TYPE)) {
      expect(CHANGE_TYPE_VALUES).toContain(backend as any);
    }
    expect(changeTypeKey('ApplicationDeployment')).toBe('changes.type.ApplicationDeployment');
  });

  it('16 — a 403 is normalized to forbidden (session-preserving; no logout signal)', () => {
    const err = normalizeHttpError(new HttpErrorResponse({ status: 403, statusText: 'Forbidden', error: { message: 'no' }, headers: new HttpHeaders() }));
    expect(err.kind).toBe('forbidden');
    expect(err.status).toBe(403);
  });
});

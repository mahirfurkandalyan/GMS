import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { API_BASE_URL, PagedResult } from '../api.config';
import { apiErrorInterceptor } from '../api-error.interceptor';
import { WorkflowInstanceApiService } from './workflow-instance-api.service';
import { WorkflowInstanceDetail, WorkflowInstanceListItem, WorkflowTaskItem } from './workflow.models';
import { workflowStatusLabelKey, workflowStepTypeLabelKey } from './workflow-labels';

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

function instance(over: Partial<WorkflowInstanceDetail> = {}): WorkflowInstanceDetail {
  return {
    id: 'w1', instanceNo: 'WFI-1', workflowDefinitionId: 'd1', workflowCode: 'CHG', workflowName: 'Change WF',
    workflowVersionId: 'v1', versionNumber: 1, triggerObjectType: 'ChangeRequest', triggerObjectId: 'c1',
    triggerObjectNumber: 'CHG-2026-000001', status: 'Waiting', currentStepInstanceId: 's1', outcome: null,
    createdAt: '2026-07-16T00:00:00Z', startedAt: null, completedAt: null, rowVersion: 'ROW==',
    steps: [], events: [], ...over
  };
}

describe('WorkflowInstanceApiService — real backend integration', () => {
  let api: WorkflowInstanceApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    installMemoryStorage();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([apiErrorInterceptor])), provideHttpClientTesting()]
    });
    api = TestBed.inject(WorkflowInstanceApiService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('1 — myTasks GETs /workflow-instances/tasks/mine (server-enforced visibility)', () => {
    let res: WorkflowTaskItem[] | undefined;
    api.myTasks().subscribe((t) => (res = t));
    const req = http.expectOne(`${API_BASE_URL}/workflow-instances/tasks/mine`);
    expect(req.request.method).toBe('GET');
    req.flush([{ instanceId: 'w1', instanceNo: 'WFI-1', stepInstanceId: 's1', stepKey: 'k', stepName: 'Architect Review', stepType: 'Approval', workflowName: 'Change WF', triggerObjectType: 'ChangeRequest', triggerObjectId: 'c1', triggerObjectNumber: 'CHG-1', assignedRole: 'Architect', assignedUserId: null, dueAt: null, isOverdue: false, createdAt: '2026-07-16T00:00:00Z' }]);
    expect(res?.length).toBe(1);
    expect(res?.[0].stepName).toBe('Architect Review');
  });

  it('2 — list GETs instances filtered by triggerObjectId (paged envelope)', () => {
    let res: PagedResult<WorkflowInstanceListItem> | undefined;
    api.list({ triggerObjectId: 'c1', pageSize: 1, sortBy: 'createdAt', sortDir: 'desc' }).subscribe((r) => (res = r));
    const req = http.expectOne((r) => r.url === `${API_BASE_URL}/workflow-instances`);
    expect(req.request.params.get('triggerObjectId')).toBe('c1');
    expect(req.request.params.get('pageSize')).toBe('1');
    req.flush({ items: [{ id: 'w1' }], page: 1, pageSize: 1, totalCount: 1, totalPages: 1 });
    expect(res?.items.length).toBe(1);
  });

  it('3 — getById returns the instance with steps + events', () => {
    let res: WorkflowInstanceDetail | undefined;
    api.getById('w1').subscribe((i) => (res = i));
    http.expectOne(`${API_BASE_URL}/workflow-instances/w1`).flush(instance({
      steps: [{ id: 's1', stepKey: 'k', name: 'Architect Review', stepType: 'Approval', stepOrder: 1, status: 'Active', assignedRole: 'Architect', assignedUserId: null, dueAt: null, actionedByUserId: null, result: null, comment: null, createdAt: '2026-07-16T00:00:00Z', activatedAt: null, completedAt: null }],
      events: [{ id: 'e1', workflowStepInstanceId: null, eventType: 'WorkflowStarted', description: 'başladı', actorUserId: 'u1', actorUserName: 'Requester User', createdAt: '2026-07-16T00:00:00Z' }]
    }));
    expect(res?.steps[0].status).toBe('Active');
    expect(res?.events[0].actorUserName).toBe('Requester User');
  });

  it('4 — completeTask POSTs to /tasks/complete with the comment', () => {
    let res: WorkflowInstanceDetail | undefined;
    api.completeTask('w1', 'onaylandı').subscribe((i) => (res = i));
    const req = http.expectOne(`${API_BASE_URL}/workflow-instances/w1/tasks/complete`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.comment).toBe('onaylandı');
    req.flush(instance({ status: 'Completed', outcome: 'Approved' }));
    expect(res?.status).toBe('Completed');
  });

  it('5 — rejectTask POSTs to /tasks/reject with the required comment', () => {
    let res: WorkflowInstanceDetail | undefined;
    api.rejectTask('w1', 'yetersiz').subscribe((i) => (res = i));
    const req = http.expectOne(`${API_BASE_URL}/workflow-instances/w1/tasks/reject`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.comment).toBe('yetersiz');
    req.flush(instance({ status: 'Rejected' }));
    expect(res?.status).toBe('Rejected');
  });

  it('6 — pause / resume POST to the correct endpoints', () => {
    api.pause('w1').subscribe();
    http.expectOne(`${API_BASE_URL}/workflow-instances/w1/pause`).flush(instance({ status: 'Running' }));
    api.resume('w1').subscribe();
    http.expectOne(`${API_BASE_URL}/workflow-instances/w1/resume`).flush(instance({ status: 'Waiting' }));
  });

  it('7 — cancel POSTs reason + rowVersion for optimistic concurrency', () => {
    api.cancel('w1', 'gerekçe', 'ROW==').subscribe();
    const req = http.expectOne(`${API_BASE_URL}/workflow-instances/w1/cancel`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.rowVersion).toBe('ROW==');
    expect(req.request.body.reason).toBe('gerekçe');
    req.flush(instance({ status: 'Cancelled' }));
  });

  it('8 — a 403 on task action is normalized to forbidden (session preserved)', () => {
    let err: any;
    api.completeTask('w1', 'x').subscribe({ error: (e) => (err = e) });
    http.expectOne(`${API_BASE_URL}/workflow-instances/w1/tasks/complete`).flush({ message: 'atama uyuşmuyor' }, { status: 403, statusText: 'Forbidden' });
    expect(err.kind).toBe('forbidden');
  });

  it('9 — workflow label helpers resolve under the workflows scope', () => {
    expect(workflowStatusLabelKey('Waiting')).toBe('workflows.status.Waiting');
    expect(workflowStepTypeLabelKey('Approval')).toBe('workflows.stepType.Approval');
  });

  it('10 — the service never writes Workflow domain state to localStorage', () => {
    api.myTasks().subscribe();
    http.expectOne(`${API_BASE_URL}/workflow-instances/tasks/mine`).flush([]);
    expect(localStorage.getItem('gms.workflows')).toBeNull();
  });
});

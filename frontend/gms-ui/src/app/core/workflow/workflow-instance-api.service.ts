import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL, PagedResult } from '../api.config';
import {
  WorkflowInstanceListItem, WorkflowInstanceDetail, WorkflowTaskItem, WorkflowInstanceQuery
} from './workflow.models';

/**
 * Real backend client for the Workflow runtime (/api/workflow-instances). Bearer + error handling
 * come from the interceptors. NOTE: the backend exposes no separate /audit or /timeline endpoint —
 * the instance timeline is the `events` array on the detail response.
 */
@Injectable({ providedIn: 'root' })
export class WorkflowInstanceApiService {
  private readonly http = inject(HttpClient);
  private readonly url = `${API_BASE_URL}/workflow-instances`;

  list(query: WorkflowInstanceQuery = {}): Observable<PagedResult<WorkflowInstanceListItem>> {
    let params = new HttpParams();
    const set = (k: string, v: unknown) => {
      if (v !== undefined && v !== null && v !== '') params = params.set(k, String(v));
    };
    set('definitionId', query.definitionId);
    set('status', query.status);
    set('triggerObjectId', query.triggerObjectId);
    set('search', query.search);
    set('page', query.page);
    set('pageSize', query.pageSize);
    set('sortBy', query.sortBy);
    set('sortDir', query.sortDir);
    return this.http.get<PagedResult<WorkflowInstanceListItem>>(this.url, { params });
  }

  getById(id: string): Observable<WorkflowInstanceDetail> {
    return this.http.get<WorkflowInstanceDetail>(`${this.url}/${id}`);
  }

  /** Tasks assigned to the current user (role/user visibility enforced server-side). */
  myTasks(): Observable<WorkflowTaskItem[]> {
    return this.http.get<WorkflowTaskItem[]>(`${this.url}/tasks/mine`);
  }

  completeTask(instanceId: string, comment?: string): Observable<WorkflowInstanceDetail> {
    return this.http.post<WorkflowInstanceDetail>(`${this.url}/${instanceId}/tasks/complete`, { comment });
  }

  rejectTask(instanceId: string, comment: string): Observable<WorkflowInstanceDetail> {
    return this.http.post<WorkflowInstanceDetail>(`${this.url}/${instanceId}/tasks/reject`, { comment });
  }

  pause(instanceId: string): Observable<WorkflowInstanceDetail> {
    return this.http.post<WorkflowInstanceDetail>(`${this.url}/${instanceId}/pause`, {});
  }

  resume(instanceId: string): Observable<WorkflowInstanceDetail> {
    return this.http.post<WorkflowInstanceDetail>(`${this.url}/${instanceId}/resume`, {});
  }

  cancel(instanceId: string, reason?: string, rowVersion?: string): Observable<WorkflowInstanceDetail> {
    return this.http.post<WorkflowInstanceDetail>(`${this.url}/${instanceId}/cancel`, { reason, rowVersion });
  }
}

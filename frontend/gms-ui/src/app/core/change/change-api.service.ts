import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL, PagedResult } from '../api.config';
import {
  ChangeRequestListItem, ChangeRequestDetail, ChangeAuditEvent, ChangeListQuery,
  CreateChangeRequestInput, UpdateChangeRequestInput, CreateChangeRevisionInput
} from './change.models';

/**
 * Real backend client for the Change Management domain (POST/GET/PUT under /api/change-requests).
 * The Bearer token is attached by authInterceptor and errors are normalized by apiErrorInterceptor —
 * this service only builds URLs/params and types responses. The actor (creator/updater) is resolved
 * from the JWT server-side and is NEVER sent from the client (no actorUserId / X-Actor-User-Id).
 */
@Injectable({ providedIn: 'root' })
export class ChangeApiService {
  private readonly http = inject(HttpClient);
  private readonly url = `${API_BASE_URL}/change-requests`;

  /** Paged, filtered list. Returns the backend PagedResult envelope verbatim (no hidden pageSize). */
  list(query: ChangeListQuery = {}): Observable<PagedResult<ChangeRequestListItem>> {
    let params = new HttpParams();
    const set = (k: string, v: unknown) => {
      if (v !== undefined && v !== null && v !== '') params = params.set(k, String(v));
    };
    set('customerId', query.customerId);
    set('projectId', query.projectId);
    set('environmentId', query.environmentId);
    set('status', query.status);
    set('changeClass', query.changeClass);
    set('changeType', query.changeType);
    set('riskLevel', query.riskLevel);
    set('search', query.search);
    set('page', query.page);
    set('pageSize', query.pageSize);
    set('sortBy', query.sortBy);
    set('sortDir', query.sortDir);
    return this.http.get<PagedResult<ChangeRequestListItem>>(this.url, { params });
  }

  getById(id: string): Observable<ChangeRequestDetail> {
    return this.http.get<ChangeRequestDetail>(`${this.url}/${id}`);
  }

  create(input: CreateChangeRequestInput): Observable<ChangeRequestDetail> {
    return this.http.post<ChangeRequestDetail>(this.url, input);
  }

  update(id: string, input: UpdateChangeRequestInput): Observable<ChangeRequestDetail> {
    return this.http.put<ChangeRequestDetail>(`${this.url}/${id}`, input);
  }

  submit(id: string): Observable<ChangeRequestDetail> {
    return this.http.post<ChangeRequestDetail>(`${this.url}/${id}/submit`, {});
  }

  cancel(id: string): Observable<ChangeRequestDetail> {
    return this.http.post<ChangeRequestDetail>(`${this.url}/${id}/cancel`, {});
  }

  addRevision(id: string, input: CreateChangeRevisionInput): Observable<ChangeRequestDetail> {
    return this.http.post<ChangeRequestDetail>(`${this.url}/${id}/revisions`, input);
  }

  /** Dedicated audit endpoint (list is also embedded in detail.auditEvents). */
  getAudit(id: string): Observable<ChangeAuditEvent[]> {
    return this.http.get<ChangeAuditEvent[]>(`${this.url}/${id}/audit`);
  }
}

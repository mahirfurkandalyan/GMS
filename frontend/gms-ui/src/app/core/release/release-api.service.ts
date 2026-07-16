import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL, PagedResult } from '../api.config';
import {
  ReleasePlanListItem, ReleasePlanDetail, ReleaseAuditEvent, ReleaseListQuery,
  CreateReleasePlanInput, UpdateReleasePlanInput
} from './release.models';

/**
 * Real backend client for the Release Planning domain (/api/releases). The Bearer token is attached
 * by authInterceptor and errors are normalized by apiErrorInterceptor — this service only builds
 * URLs/params and types responses. The actor is resolved from the JWT server-side (no actorUserId).
 *
 * NOTE: the backend list endpoint filters by customerId/projectId/environmentId/status/search only;
 * release-type/date are not server-filterable, so they are intentionally not offered as server filters.
 */
@Injectable({ providedIn: 'root' })
export class ReleaseApiService {
  private readonly http = inject(HttpClient);
  private readonly url = `${API_BASE_URL}/releases`;

  list(query: ReleaseListQuery = {}): Observable<PagedResult<ReleasePlanListItem>> {
    let params = new HttpParams();
    const set = (k: string, v: unknown) => {
      if (v !== undefined && v !== null && v !== '') params = params.set(k, String(v));
    };
    set('customerId', query.customerId);
    set('projectId', query.projectId);
    set('environmentId', query.environmentId);
    set('status', query.status);
    set('search', query.search);
    set('page', query.page);
    set('pageSize', query.pageSize);
    set('sortBy', query.sortBy);
    set('sortDir', query.sortDir);
    return this.http.get<PagedResult<ReleasePlanListItem>>(this.url, { params });
  }

  getById(id: string): Observable<ReleasePlanDetail> {
    return this.http.get<ReleasePlanDetail>(`${this.url}/${id}`);
  }

  create(input: CreateReleasePlanInput): Observable<ReleasePlanDetail> {
    return this.http.post<ReleasePlanDetail>(this.url, input);
  }

  update(id: string, input: UpdateReleasePlanInput): Observable<ReleasePlanDetail> {
    return this.http.put<ReleasePlanDetail>(`${this.url}/${id}`, input);
  }

  schedule(id: string): Observable<ReleasePlanDetail> {
    return this.http.post<ReleasePlanDetail>(`${this.url}/${id}/schedule`, {});
  }

  cancel(id: string): Observable<ReleasePlanDetail> {
    return this.http.post<ReleasePlanDetail>(`${this.url}/${id}/cancel`, {});
  }

  complete(id: string): Observable<ReleasePlanDetail> {
    return this.http.post<ReleasePlanDetail>(`${this.url}/${id}/complete`, {});
  }

  getAudit(id: string): Observable<ReleaseAuditEvent[]> {
    return this.http.get<ReleaseAuditEvent[]>(`${this.url}/${id}/audit`);
  }
}

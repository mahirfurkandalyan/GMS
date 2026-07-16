import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL, PagedResult } from './api.config';

/**
 * Real backend API client for Integration executions (the retry/outbox-ready runtime).
 *
 * NOTE: Integration-ready, intentionally NOT wired into the UI yet. Execution summaries
 * are sanitized server-side (never contain secrets). Dispatch/retry are Admin-only,
 * rate-limited endpoints. Types mirror backend DTOs.
 */

export interface IntegrationExecutionListItem {
  id: string;
  executionNo: string;
  integrationDefinitionId: string;
  integrationName: string;
  direction: string;
  operation: string;
  status: string;
  httpStatusCode: number | null;
  retryCount: number;
  correlationId: string;
  createdAt: string;
  completedAt: string | null;
}

export interface IntegrationExecutionAttempt {
  id: string;
  attemptNo: number;
  startedAt: string;
  completedAt: string | null;
  status: string;
  httpStatusCode: number | null;
  errorMessage: string | null;
  durationMilliseconds: number;
}

export interface IntegrationEvent {
  id: string;
  integrationExecutionId: string | null;
  eventType: string;
  description: string;
  actorUserId: string | null;
  createdAt: string;
}

export interface IntegrationExecutionDetail extends IntegrationExecutionListItem {
  objectType: string | null;
  objectId: string | null;
  startedAt: string | null;
  requestSummary: string | null;
  responseSummary: string | null;
  errorCode: string | null;
  errorMessage: string | null;
  rowVersion: string;
  attempts: IntegrationExecutionAttempt[];
  events: IntegrationEvent[];
}

export interface DispatchResult {
  processed: number;
  succeeded: number;
  failed: number;
  deadLettered: number;
}

@Injectable({ providedIn: 'root' })
export class IntegrationExecutionApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE_URL}/integration-executions`;

  list(opts: { integrationId?: string; status?: string; direction?: string; correlationId?: string; page?: number; pageSize?: number } = {})
    : Observable<PagedResult<IntegrationExecutionListItem>> {
    let params = new HttpParams();
    for (const [k, v] of Object.entries(opts)) if (v != null) params = params.set(k, String(v));
    return this.http.get<PagedResult<IntegrationExecutionListItem>>(this.base, { params });
  }

  get(id: string): Observable<IntegrationExecutionDetail> {
    return this.http.get<IntegrationExecutionDetail>(`${this.base}/${id}`);
  }

  retry(id: string): Observable<IntegrationExecutionDetail> {
    return this.http.post<IntegrationExecutionDetail>(`${this.base}/${id}/retry`, {});
  }

  dispatchPending(max = 50): Observable<DispatchResult> {
    return this.http.post<DispatchResult>(`${this.base}/dispatch-pending?max=${max}`, {});
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/cancel`, {});
  }
}

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { API_BASE_URL, PagedResult } from './api.config';

/**
 * Real backend API client for the Approval Management domain.
 *
 * NOTE: Integration-ready, intentionally NOT wired into the UI yet. Types mirror
 * the backend DTOs. UI migration to the real backend happens in a later step.
 */

export interface ApprovalListItem {
  id: string;
  approvalNo: string;
  relatedObjectType: string;
  relatedObjectId: string;
  title: string;
  status: string;
  priority: string;
  requestedByUserName: string;
  requestedAt: string;
  completedAt: string | null;
  stepCount: number;
  currentStepNo: number;
  currentStepName: string | null;
}

export interface ApprovalStep {
  id: string;
  stepNo: number;
  stepName: string;
  approverRole: string;
  approverUserId: string | null;
  approverUserName: string | null;
  status: string;
  dueDate: string | null;
  completedAt: string | null;
}

export interface ApprovalDecision {
  id: string;
  approvalStepId: string;
  stepNo: number;
  decision: string;
  comment: string;
  signatureMeaning: string;
  signedByUserId: string;
  signedByUserName: string;
  signedAt: string;
}

export interface ApprovalAuditEvent {
  id: string;
  eventType: string;
  description: string;
  actorUserId: string;
  createdAt: string;
}

export interface RelatedObjectSummary {
  type: string;
  id: string;
  code: string;
  title: string;
  status: string;
  riskLevel: string;
}

export interface ApprovalDetail extends ApprovalListItem {
  description: string;
  requestedByUserId: string;
  createdAt: string;
  updatedAt: string | null;
  steps: ApprovalStep[];
  decisions: ApprovalDecision[];
  auditEvents: ApprovalAuditEvent[];
  relatedObject: RelatedObjectSummary | null;
}

export interface ApprovalFilters {
  status?: string;
  relatedObjectType?: string;
  relatedObjectId?: string;
  requestedByUserId?: string;
  approverUserId?: string;
  approverRole?: string;
  search?: string;
}

export interface ApprovalActionInput {
  userId: string;
  comment?: string;
  signatureMeaning?: string;
}

@Injectable({ providedIn: 'root' })
export class ApprovalApiService {
  private readonly http = inject(HttpClient);
  private readonly url = `${API_BASE_URL}/approvals`;

  // Unwraps the backend paged envelope (PagedResult<T>) to keep the array contract.
  list(filters: ApprovalFilters = {}): Observable<ApprovalListItem[]> {
    let params = new HttpParams().set('pageSize', '100');
    for (const [key, value] of Object.entries(filters)) {
      if (value) params = params.set(key, String(value));
    }
    return this.http
      .get<PagedResult<ApprovalListItem>>(this.url, { params })
      .pipe(map((r) => r.items));
  }

  getById(id: string): Observable<ApprovalDetail> {
    return this.http.get<ApprovalDetail>(`${this.url}/${id}`);
  }

  getByChange(changeRequestId: string): Observable<ApprovalDetail> {
    return this.http.get<ApprovalDetail>(`${this.url}/by-change/${changeRequestId}`);
  }

  approve(id: string, input: ApprovalActionInput): Observable<ApprovalDetail> {
    return this.http.post<ApprovalDetail>(`${this.url}/${id}/approve`, input);
  }

  reject(id: string, input: ApprovalActionInput): Observable<ApprovalDetail> {
    return this.http.post<ApprovalDetail>(`${this.url}/${id}/reject`, input);
  }

  requestRevision(id: string, input: ApprovalActionInput): Observable<ApprovalDetail> {
    return this.http.post<ApprovalDetail>(`${this.url}/${id}/request-revision`, input);
  }

  getAudit(id: string): Observable<ApprovalAuditEvent[]> {
    return this.http.get<ApprovalAuditEvent[]>(`${this.url}/${id}/audit`);
  }
}

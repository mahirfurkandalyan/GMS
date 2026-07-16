import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api.config';

/**
 * Real backend API client for operational status (background processing).
 *
 * NOTE: Integration-ready, intentionally NOT wired into the UI. Status is Admin/Auditor-readable;
 * the run-once trigger is Admin-only and exists for controlled diagnostics, not normal operation.
 * Types mirror backend DTOs.
 */

export interface WorkerHeartbeat {
  workerName: string;
  instanceId: string;
  lastStartedAt: string | null;
  lastSucceededAt: string | null;
  lastFailedAt: string | null;
  lastError: string | null;
  updatedAt: string;
}

export interface OperationalStatus {
  pendingIntegrationExecutions: number;
  retryScheduledIntegrationExecutions: number;
  deadLetterIntegrationExecutions: number;
  pendingNotificationDeliveries: number;
  failedNotificationDeliveries: number;
  overdueWorkflowTasks: number;
  generatedAt: string;
  workers: WorkerHeartbeat[];
}

export interface WorkerRunResult {
  workerName: string;
  processed: number;
}

@Injectable({ providedIn: 'root' })
export class OperationsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE_URL}/operations`;

  status(): Observable<OperationalStatus> {
    return this.http.get<OperationalStatus>(`${this.base}/status`);
  }

  // Admin-only controlled diagnostic — not for normal operation.
  runOnce(workerName: string): Observable<WorkerRunResult> {
    return this.http.post<WorkerRunResult>(`${this.base}/workers/${workerName}/run-once`, {});
  }
}

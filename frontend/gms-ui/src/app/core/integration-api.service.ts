import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL, PagedResult } from './api.config';

/**
 * Real backend API client for the Integration Hub (definition management).
 *
 * NOTE: This is an integration-ready layer, intentionally NOT yet wired into the
 * UI. The existing placeholder Integration pages remain untouched. Secrets are never
 * returned by the backend — credential DTOs expose only a masked value. When wiring
 * the UI later, credential *input* fields must be write-only (never render a stored
 * secret) and the raw value must be sent over HTTPS only. Types mirror backend DTOs.
 */

export interface IntegrationListItem {
  id: string;
  integrationNo: string;
  code: string;
  name: string;
  provider: string;
  category: string;
  status: string;
  authenticationType: string;
  isSystem: boolean;
  createdAt: string;
  lastSuccessfulConnectionAt: string | null;
  lastFailedConnectionAt: string | null;
}

export interface IntegrationCredential {
  id: string;
  credentialType: string;
  keyName: string;
  maskedValue: string; // never the raw/encrypted value
  createdAt: string;
  rotatedAt: string | null;
}

export interface IntegrationEndpoint {
  id: string;
  name: string;
  direction: string;
  relativePath: string;
  httpMethod: string;
  timeoutSeconds: number;
  isActive: boolean;
  createdAt: string;
}

export interface IntegrationSubscription {
  id: string;
  eventType: string;
  objectType: string | null;
  targetEndpointId: string;
  isActive: boolean;
  createdAt: string;
}

export interface IntegrationDetail {
  id: string;
  integrationNo: string;
  code: string;
  name: string;
  description: string;
  provider: string;
  category: string;
  status: string;
  baseUrl: string | null;
  authenticationType: string;
  isSystem: boolean;
  createdAt: string;
  updatedAt: string | null;
  lastSuccessfulConnectionAt: string | null;
  lastFailedConnectionAt: string | null;
  rowVersion: string;
  credentials: IntegrationCredential[];
  endpoints: IntegrationEndpoint[];
  subscriptions: IntegrationSubscription[];
}

export interface ProviderInfo {
  provider: string;
  implemented: boolean;
  supportsIncoming: boolean;
  supportsOutgoing: boolean;
}

export interface ConnectionTestResult {
  success: boolean;
  httpStatusCode: number | null;
  message: string;
  durationMilliseconds: number;
}

export interface ExternalObjectLink {
  id: string;
  integrationDefinitionId: string;
  integrationName: string;
  internalObjectType: string;
  internalObjectId: string;
  externalObjectType: string;
  externalObjectId: string;
  externalObjectKey: string | null;
  externalUrl: string | null;
  createdByUserId: string;
  createdAt: string;
  lastSyncedAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class IntegrationApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE_URL}/integrations`;

  list(opts: { provider?: string; status?: string; category?: string; search?: string; page?: number; pageSize?: number } = {})
    : Observable<PagedResult<IntegrationListItem>> {
    let params = new HttpParams();
    for (const [k, v] of Object.entries(opts)) if (v != null) params = params.set(k, String(v));
    return this.http.get<PagedResult<IntegrationListItem>>(this.base, { params });
  }

  get(id: string): Observable<IntegrationDetail> {
    return this.http.get<IntegrationDetail>(`${this.base}/${id}`);
  }

  providers(): Observable<ProviderInfo[]> {
    return this.http.get<ProviderInfo[]>(`${this.base}/providers`);
  }

  create(body: { code: string; name: string; description?: string; provider: string; category?: string; baseUrl?: string; authenticationType?: string })
    : Observable<IntegrationDetail> {
    return this.http.post<IntegrationDetail>(this.base, body);
  }

  update(id: string, body: { name?: string; description?: string; category?: string; baseUrl?: string; authenticationType?: string; rowVersion?: string })
    : Observable<IntegrationDetail> {
    return this.http.put<IntegrationDetail>(`${this.base}/${id}`, body);
  }

  activate(id: string): Observable<IntegrationDetail> { return this.http.post<IntegrationDetail>(`${this.base}/${id}/activate`, {}); }
  deactivate(id: string): Observable<IntegrationDetail> { return this.http.post<IntegrationDetail>(`${this.base}/${id}/deactivate`, {}); }
  testConnection(id: string): Observable<ConnectionTestResult> { return this.http.post<ConnectionTestResult>(`${this.base}/${id}/test-connection`, {}); }

  // Credentials — the raw value is write-only; the response never echoes it back.
  addCredential(id: string, body: { keyName: string; value: string; credentialType?: string }): Observable<IntegrationCredential> {
    return this.http.post<IntegrationCredential>(`${this.base}/${id}/credentials`, body);
  }
  rotateCredential(id: string, credentialId: string, value: string): Observable<IntegrationCredential> {
    return this.http.put<IntegrationCredential>(`${this.base}/${id}/credentials/${credentialId}`, { value });
  }
  deleteCredential(id: string, credentialId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}/credentials/${credentialId}`);
  }

  addEndpoint(id: string, body: unknown): Observable<IntegrationEndpoint> { return this.http.post<IntegrationEndpoint>(`${this.base}/${id}/endpoints`, body); }
  addSubscription(id: string, body: unknown): Observable<IntegrationSubscription> { return this.http.post<IntegrationSubscription>(`${this.base}/${id}/subscriptions`, body); }

  linksForObject(objectType: string, objectId: string): Observable<ExternalObjectLink[]> {
    return this.http.get<ExternalObjectLink[]>(`${this.base}/links/object/${objectType}/${objectId}`);
  }
  createLink(id: string, body: { internalObjectType: string; internalObjectId: string; externalObjectType?: string; externalReference: string; externalUrl?: string })
    : Observable<ExternalObjectLink> {
    return this.http.post<ExternalObjectLink>(`${this.base}/${id}/links`, body);
  }
}

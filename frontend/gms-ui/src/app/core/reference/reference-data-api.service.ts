import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api.config';

/** Reference-data DTOs (any authenticated user). Mirror Gms.Api.Contracts customer/project/environment. */
export interface CustomerRef {
  id: string;
  name: string;
  code: string;
  status: string;
  createdAt: string;
}

export interface ProjectRef {
  id: string;
  customerId: string;
  customerName: string;
  name: string;
  code: string;
  description: string;
  status: string;
  createdAt: string;
}

export interface EnvironmentRef {
  id: string;
  projectId: string;
  projectName: string;
  name: string;
  type: string;
  status: string;
}

/**
 * Reference data used by the Change wizard and list filters. Supports the dependent chain
 * Customer → Projects(customerId) → Environments(projectId). No static/mock catalogs.
 */
@Injectable({ providedIn: 'root' })
export class ReferenceDataApiService {
  private readonly http = inject(HttpClient);
  private readonly base = API_BASE_URL;

  customers(): Observable<CustomerRef[]> {
    return this.http.get<CustomerRef[]>(`${this.base}/customers`);
  }

  projects(customerId?: string): Observable<ProjectRef[]> {
    const params = customerId ? new HttpParams().set('customerId', customerId) : undefined;
    return this.http.get<ProjectRef[]>(`${this.base}/projects`, { params });
  }

  environments(projectId?: string): Observable<EnvironmentRef[]> {
    const params = projectId ? new HttpParams().set('projectId', projectId) : undefined;
    return this.http.get<EnvironmentRef[]>(`${this.base}/environments`, { params });
  }
}

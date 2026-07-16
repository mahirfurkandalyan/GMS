import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { API_BASE_URL } from './api.config';
import { MOCK_PROJECTS, MOCK_ENVIRONMENTS } from './mock-data';

export interface Project {
  id: string;
  customerId: string;
  customerName: string;
  name: string;
  code: string;
  description: string;
  status: string;
}

export interface AppEnvironment {
  id: string;
  projectId: string;
  projectName: string;
  name: string;
  type: string;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);

  getProjects(customerId?: string): Observable<Project[]> {
    let params = new HttpParams();
    if (customerId) {
      params = params.set('customerId', customerId);
    }
    return this.http.get<Project[]>(`${API_BASE_URL}/projects`, { params }).pipe(
      catchError(() => of(customerId ? MOCK_PROJECTS.filter((p) => p.customerId === customerId) : MOCK_PROJECTS))
    );
  }

  getEnvironments(projectId?: string): Observable<AppEnvironment[]> {
    let params = new HttpParams();
    if (projectId) {
      params = params.set('projectId', projectId);
    }
    return this.http.get<AppEnvironment[]>(`${API_BASE_URL}/environments`, { params }).pipe(
      catchError(() => of(projectId ? MOCK_ENVIRONMENTS.filter((e) => e.projectId === projectId) : MOCK_ENVIRONMENTS))
    );
  }
}

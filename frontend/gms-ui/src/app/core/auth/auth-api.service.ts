import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { AuthResponse, AuthUser, ChangePasswordRequest, LoginRequest } from './auth.models';

const AUTH_URL = `${API_BASE_URL}/auth`;

/**
 * Raw HTTP client for the backend auth endpoints. NO state, NO storage — just the calls. State
 * lives in AuthStateService; tokens in TokenStorageService. Matches the backend contract exactly.
 */
@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);

  login(body: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${AUTH_URL}/login`, body);
  }

  /** Rotating refresh — the backend revokes the old token and returns a new pair. */
  refresh(refreshToken: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${AUTH_URL}/refresh`, { refreshToken });
  }

  logout(refreshToken: string | null): Observable<void> {
    return this.http.post<void>(`${AUTH_URL}/logout`, { refreshToken });
  }

  logoutAll(): Observable<void> {
    return this.http.post<void>(`${AUTH_URL}/logout-all`, {});
  }

  me(): Observable<AuthUser> {
    return this.http.get<AuthUser>(`${AUTH_URL}/me`);
  }

  changePassword(body: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${AUTH_URL}/change-password`, body);
  }
}

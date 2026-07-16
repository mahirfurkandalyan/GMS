import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { API_BASE_URL } from '../api.config';
import { AuthStateService } from './auth-state.service';
import { TokenStorageService } from './token-storage.service';
import { authInterceptor } from './auth.interceptor';
import { apiErrorInterceptor } from '../api-error.interceptor';
import { authGuard, permissionGuard } from './guards';
import { HasPermissionDirective } from './has-permission.directive';
import { AuthResponse } from './auth.models';

function authResponse(overrides: Partial<AuthResponse> = {}): AuthResponse {
  const future = new Date(Date.now() + 60 * 60 * 1000).toISOString();
  return {
    accessToken: 'ACCESS-1',
    refreshToken: 'REFRESH-1',
    accessTokenExpiresAt: future,
    refreshTokenExpiresAt: future,
    user: { id: 'u1', fullName: 'Test User', email: 'test@gms.local', roles: ['Requester'], permissions: ['change.read'] },
    ...overrides
  };
}

function configure(): void {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([apiErrorInterceptor, authInterceptor])),
      provideHttpClientTesting(),
      provideRouter([
        { path: 'login', children: [] },
        { path: 'forbidden', children: [] }
      ])
    ]
  });
}

describe('AuthStateService', () => {
  let state: AuthStateService;
  let storage: TokenStorageService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    configure();
    state = TestBed.inject(AuthStateService);
    storage = TestBed.inject(TokenStorageService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('1 — successful login stores session and user', () => {
    let user;
    state.login('test@gms.local', 'pw').subscribe((u) => (user = u));
    httpMock.expectOne(`${API_BASE_URL}/auth/login`).flush(authResponse());
    expect(state.isAuthenticated()).toBe(true);
    expect(state.user()?.email).toBe('test@gms.local');
    expect(storage.accessToken).toBe('ACCESS-1');
    expect(storage.refreshToken).toBe('REFRESH-1');
    expect(user).toBeTruthy();
  });

  it('2 — failed login surfaces a normalized error and keeps the user unauthenticated', () => {
    let err: any;
    state.login('test@gms.local', 'wrong').subscribe({ error: (e) => (err = e) });
    httpMock.expectOne(`${API_BASE_URL}/auth/login`).flush({ message: 'bad' }, { status: 401, statusText: 'Unauthorized' });
    expect(err?.kind).toBe('unauthenticated');
    expect(state.isAuthenticated()).toBe(false);
  });

  it('11 — logout clears tokens and user', () => {
    state.login('test@gms.local', 'pw').subscribe();
    httpMock.expectOne(`${API_BASE_URL}/auth/login`).flush(authResponse());
    state.logout().subscribe();
    httpMock.expectOne(`${API_BASE_URL}/auth/logout`).flush(null);
    expect(state.isAuthenticated()).toBe(false);
    expect(storage.accessToken).toBeNull();
    expect(storage.refreshToken).toBeNull();
  });

  it('12 — session restoration calls /me when the access token is valid', () => {
    storage.setTokens('AT', 'RT', new Date(Date.now() + 3600_000).toISOString(), new Date(Date.now() + 3600_000).toISOString());
    let restored: boolean | undefined;
    state.restoreSession().subscribe((ok) => (restored = ok));
    httpMock.expectOne(`${API_BASE_URL}/auth/me`).flush(authResponse().user);
    expect(restored).toBe(true);
    expect(state.isAuthenticated()).toBe(true);
  });

  it('13 — expired access token restores via refresh', () => {
    storage.setTokens('AT', 'RT', new Date(Date.now() - 1000).toISOString(), new Date(Date.now() + 3600_000).toISOString());
    let restored: boolean | undefined;
    state.restoreSession().subscribe((ok) => (restored = ok));
    httpMock.expectOne(`${API_BASE_URL}/auth/refresh`).flush(authResponse({ accessToken: 'ACCESS-2' }));
    expect(restored).toBe(true);
    expect(storage.accessToken).toBe('ACCESS-2');
  });
});

describe('authInterceptor (bearer + refresh)', () => {
  let storage: TokenStorageService;
  let httpMock: HttpTestingController;
  let state: AuthStateService;
  let http: HttpClient;

  beforeEach(() => {
    sessionStorage.clear();
    configure();
    storage = TestBed.inject(TokenStorageService);
    httpMock = TestBed.inject(HttpTestingController);
    state = TestBed.inject(AuthStateService);
    http = TestBed.inject(HttpClient);
  });

  afterEach(() => httpMock.verify());

  it('3 — adds the Bearer token to API requests', () => {
    storage.setTokens('AT', 'RT', new Date(Date.now() + 3600_000).toISOString(), new Date(Date.now() + 3600_000).toISOString());
    http.get(`${API_BASE_URL}/changes`).subscribe();
    const req = httpMock.expectOne(`${API_BASE_URL}/changes`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer AT');
    req.flush([]);
  });

  it('4 — does NOT add the token to unrelated external URLs', () => {
    storage.setTokens('AT', 'RT', new Date(Date.now() + 3600_000).toISOString(), new Date(Date.now() + 3600_000).toISOString());
    http.get('https://external.example.com/data').subscribe();
    const req = httpMock.expectOne('https://external.example.com/data');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('5 — a single refresh handles concurrent 401 responses', () => {
    storage.setTokens('AT', 'RT', new Date(Date.now() + 3600_000).toISOString(), new Date(Date.now() + 3600_000).toISOString());
    let aDone = false, bDone = false;
    http.get(`${API_BASE_URL}/a`).subscribe(() => (aDone = true));
    http.get(`${API_BASE_URL}/b`).subscribe(() => (bDone = true));

    const initial = httpMock.match((r) => r.url === `${API_BASE_URL}/a` || r.url === `${API_BASE_URL}/b`);
    expect(initial.length).toBe(2);
    initial.forEach((r) => r.flush(null, { status: 401, statusText: 'Unauthorized' }));

    // Exactly ONE refresh for both failures.
    const refresh = httpMock.expectOne(`${API_BASE_URL}/auth/refresh`);
    refresh.flush(authResponse({ accessToken: 'ACCESS-NEW' }));

    const retried = httpMock.match((r) => r.url === `${API_BASE_URL}/a` || r.url === `${API_BASE_URL}/b`);
    expect(retried.length).toBe(2);
    retried.forEach((r) => {
      expect(r.request.headers.get('Authorization')).toBe('Bearer ACCESS-NEW');
      r.flush({});
    });
    expect(aDone && bDone).toBe(true);
  });

  it('6 — a failed refresh clears the session', () => {
    storage.setTokens('AT', 'RT', new Date(Date.now() + 3600_000).toISOString(), new Date(Date.now() + 3600_000).toISOString());
    // Seed a user so we can observe it being cleared.
    state.login('x@y.z', 'pw').subscribe();
    httpMock.expectOne(`${API_BASE_URL}/auth/login`).flush(authResponse());

    http.get(`${API_BASE_URL}/secure`).subscribe({ error: () => undefined });
    httpMock.expectOne(`${API_BASE_URL}/secure`).flush(null, { status: 401, statusText: 'Unauthorized' });
    httpMock.expectOne(`${API_BASE_URL}/auth/refresh`).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(state.isAuthenticated()).toBe(false);
    expect(storage.refreshToken).toBeNull();
  });

  it('10 — a 403 does NOT trigger refresh or clear the session', () => {
    state.login('x@y.z', 'pw').subscribe();
    httpMock.expectOne(`${API_BASE_URL}/auth/login`).flush(authResponse());

    http.get(`${API_BASE_URL}/forbidden-resource`).subscribe({ error: () => undefined });
    httpMock.expectOne(`${API_BASE_URL}/forbidden-resource`).flush({ message: 'no' }, { status: 403, statusText: 'Forbidden' });

    httpMock.expectNone(`${API_BASE_URL}/auth/refresh`);
    expect(state.isAuthenticated()).toBe(true);
  });
});

describe('guards', () => {
  let state: AuthStateService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    configure();
    state = TestBed.inject(AuthStateService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  function authenticateWith(permissions: string[]): void {
    state.login('a@b.c', 'pw').subscribe();
    httpMock.expectOne(`${API_BASE_URL}/auth/login`).flush(
      authResponse({ user: { id: 'u', fullName: 'U', email: 'a@b.c', roles: ['Requester'], permissions } })
    );
  }

  it('7 — authGuard redirects an anonymous user to /login', () => {
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url: '/changes' } as RouterStateSnapshot)
    );
    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toContain('/login');
  });

  it('8 — permissionGuard allows a user with the required permission', () => {
    authenticateWith(['change.read']);
    const route = { data: { permission: 'change.read' } } as unknown as ActivatedRouteSnapshot;
    const result = TestBed.runInInjectionContext(() => permissionGuard(route, { url: '/changes' } as RouterStateSnapshot));
    expect(result).toBe(true);
  });

  it('9 — permissionGuard blocks a user without the permission (→ /forbidden)', () => {
    authenticateWith(['release.read']);
    const route = { data: { permission: 'change.create' } } as unknown as ActivatedRouteSnapshot;
    const result = TestBed.runInInjectionContext(() => permissionGuard(route, { url: '/changes/new' } as RouterStateSnapshot));
    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toContain('/forbidden');
  });
});

@Component({
  standalone: true,
  imports: [HasPermissionDirective],
  template: `<button *gmsHasPermission="'change.create'" class="create">Yeni</button>`
})
class DirectiveHost {}

describe('HasPermissionDirective (14)', () => {
  let state: AuthStateService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    configure();
    state = TestBed.inject(AuthStateService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  function authenticateWith(permissions: string[]): void {
    state.login('a@b.c', 'pw').subscribe();
    httpMock.expectOne(`${API_BASE_URL}/auth/login`).flush(
      authResponse({ user: { id: 'u', fullName: 'U', email: 'a@b.c', roles: ['x'], permissions } })
    );
  }

  it('hides the element without the permission and shows it with', () => {
    const fixture = TestBed.createComponent(DirectiveHost);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.create')).toBeNull();

    authenticateWith(['change.create']);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.create')).not.toBeNull();
  });
});

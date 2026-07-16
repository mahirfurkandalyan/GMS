import { Injectable } from '@angular/core';

/**
 * The ONLY place tokens are persisted. Feature components must never touch storage directly.
 *
 * Storage policy:
 *  - Access token: kept IN MEMORY only (never persisted) → not reachable by XSS via storage, and
 *    gone on tab close. It is short-lived and re-obtained from the refresh token on reload.
 *  - Refresh token + expiry metadata: kept in sessionStorage (per-tab, cleared on tab close) so a
 *    browser refresh can restore the session. sessionStorage is preferred over localStorage to
 *    shrink the XSS exposure window (no cross-tab persistence).
 *
 * XSS RISK (documented): any token reachable by JavaScript can be exfiltrated by a successful XSS.
 * The backend has no httpOnly-cookie auth in this PoC, so the refresh token lives in sessionStorage.
 * Mitigations: short access-token lifetime + refresh rotation (old refresh token is revoked on use)
 * limit the blast radius. A production hardening step is httpOnly, Secure, SameSite cookies.
 *
 * Never stored: passwords, password hashes, signing keys, or full sensitive user objects.
 */
const REFRESH_KEY = 'gms.auth.refreshToken';
const ACCESS_EXP_KEY = 'gms.auth.accessExpiresAt';
const REFRESH_EXP_KEY = 'gms.auth.refreshExpiresAt';

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  // In-memory access token — intentionally not persisted.
  private accessTokenValue: string | null = null;
  private accessExpiresAtValue: Date | null = null;

  setTokens(accessToken: string, refreshToken: string, accessExpiresAt: string, refreshExpiresAt: string): void {
    this.accessTokenValue = accessToken;
    this.accessExpiresAtValue = new Date(accessExpiresAt);
    try {
      sessionStorage.setItem(REFRESH_KEY, refreshToken);
      sessionStorage.setItem(ACCESS_EXP_KEY, accessExpiresAt);
      sessionStorage.setItem(REFRESH_EXP_KEY, refreshExpiresAt);
    } catch {
      /* storage may be unavailable (private mode) — session simply won't survive reload */
    }
  }

  get accessToken(): string | null {
    return this.accessTokenValue;
  }

  get refreshToken(): string | null {
    try { return sessionStorage.getItem(REFRESH_KEY); } catch { return null; }
  }

  /** Access-token expiry (in memory). */
  get accessExpiresAt(): Date | null {
    return this.accessExpiresAtValue;
  }

  /** Refresh-token expiry (from storage) — used at startup to decide whether restore is worthwhile. */
  get refreshExpiresAt(): Date | null {
    try {
      const v = sessionStorage.getItem(REFRESH_EXP_KEY);
      return v ? new Date(v) : null;
    } catch { return null; }
  }

  /** True when we hold an in-memory access token that has not expired (with a small skew). */
  hasValidAccessToken(skewMs = 5000): boolean {
    if (!this.accessTokenValue || !this.accessExpiresAtValue) return false;
    return this.accessExpiresAtValue.getTime() - skewMs > Date.now();
  }

  /** True when a (non-expired) refresh token exists to attempt session restoration. */
  hasUsableRefreshToken(): boolean {
    const rt = this.refreshToken;
    if (!rt) return false;
    const exp = this.refreshExpiresAt;
    return !exp || exp.getTime() > Date.now();
  }

  clear(): void {
    this.accessTokenValue = null;
    this.accessExpiresAtValue = null;
    try {
      sessionStorage.removeItem(REFRESH_KEY);
      sessionStorage.removeItem(ACCESS_EXP_KEY);
      sessionStorage.removeItem(REFRESH_EXP_KEY);
    } catch {
      /* ignore */
    }
  }
}

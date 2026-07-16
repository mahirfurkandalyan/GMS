// Centralized backend base URL — the SINGLE source of truth for the API host. Every service
// imports API_BASE_URL from here; no URL is hardcoded elsewhere. For a different host (LAN/prod),
// change this one value (or wire an Angular environment fileReplacement in a later step).
export const API_BASE_URL = 'http://localhost:5080/api';

/** Origin (without the /api suffix) — used e.g. for correlation-scoped checks. */
export const API_ORIGIN = 'http://localhost:5080';

/**
 * Standard paged list envelope returned by every backend list endpoint
 * (page, pageSize, totalCount, totalPages + items). Mirrors backend PagedResult<T>.
 */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

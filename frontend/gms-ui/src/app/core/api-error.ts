import { HttpErrorResponse } from '@angular/common/http';

/**
 * Normalized frontend error shape. Every API failure is mapped to this by the global error
 * interceptor / normalizer so UI code shows safe, consistent Turkish messages — never a raw
 * backend payload or stack trace.
 */
export type ApiErrorKind =
  | 'validation'      // 400 (domain/validation)
  | 'unauthenticated' // 401
  | 'forbidden'       // 403
  | 'notFound'        // 404
  | 'conflict'        // 409 (RowVersion concurrency)
  | 'rateLimited'     // 429
  | 'server'          // 5xx
  | 'network'         // no response (offline / CORS)
  | 'unknown';

/** A structured readiness finding echoed by the Change submit endpoint on a 400 (blocking). */
export interface ApiReadinessFinding {
  code: string;
  severity: string;
  message: string;
  recommendation: string;
}

export interface ApiError {
  status: number;
  kind: ApiErrorKind;
  /** Short Turkish title suitable for a toast heading. */
  title: string;
  /** Safe Turkish message for display. */
  message: string;
  /** Optional field-level validation errors (key → messages). */
  errors?: Record<string, string[]>;
  /** Structured readiness findings (Change submit 400) — safe, backend-authored, renderable. */
  readinessFindings?: ApiReadinessFinding[];
  correlationId?: string;
  /** Raw backend code/message kept for diagnostics/logging only (never rendered verbatim as-is). */
  rawCode?: string;
}

const CONFLICT_MESSAGE =
  'Kayıt başka bir kullanıcı tarafından güncellendi. Güncel veriyi yükleyip tekrar deneyin.';

/** Maps an HttpErrorResponse to the normalized ApiError. Safe Turkish messages only. */
export function normalizeHttpError(err: HttpErrorResponse): ApiError {
  const correlationId = err.headers?.get?.('X-Correlation-Id') ?? undefined;
  // The backend DomainExceptionMiddleware returns { message } for domain errors.
  const backendMessage: string | undefined =
    typeof err.error === 'object' && err.error && typeof err.error.message === 'string'
      ? err.error.message
      : undefined;
  const backendErrors: Record<string, string[]> | undefined =
    typeof err.error === 'object' && err.error && err.error.errors && typeof err.error.errors === 'object'
      ? (err.error.errors as Record<string, string[]>)
      : undefined;
  // Change submit returns a 400 with { message, readinessScore, findings } when critical
  // readiness findings block submission. Capture the structured findings so the UI can render them.
  const readinessFindings: ApiReadinessFinding[] | undefined =
    typeof err.error === 'object' && err.error && Array.isArray(err.error.findings)
      ? (err.error.findings as ApiReadinessFinding[])
      : undefined;

  const base = (kind: ApiErrorKind, title: string, message: string): ApiError => ({
    status: err.status, kind, title, message, errors: backendErrors, readinessFindings, correlationId, rawCode: backendMessage
  });

  if (err.status === 0) {
    return base('network', 'Bağlantı hatası', 'Sunucuya ulaşılamıyor. İnternet bağlantınızı kontrol edin.');
  }
  switch (err.status) {
    case 400:
      return base('validation', 'Geçersiz istek', backendMessage ?? 'Gönderilen bilgiler geçersiz. Lütfen kontrol edip tekrar deneyin.');
    case 401:
      return base('unauthenticated', 'Oturum gerekli', 'Oturumunuz sona erdi. Lütfen tekrar giriş yapın.');
    case 403:
      return base('forbidden', 'Yetkisiz işlem', 'Bu işlem için yetkiniz bulunmuyor.');
    case 404:
      return base('notFound', 'Bulunamadı', backendMessage ?? 'İstenen kayıt bulunamadı.');
    case 409:
      return base('conflict', 'Eşzamanlılık çakışması', CONFLICT_MESSAGE);
    case 429:
      return base('rateLimited', 'Çok fazla istek', 'Çok sık istek gönderildi. Lütfen biraz bekleyip tekrar deneyin.');
    default:
      if (err.status >= 500) {
        return base('server', 'Sunucu hatası', 'Beklenmeyen bir sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin.');
      }
      return base('unknown', 'Hata', backendMessage ?? 'Beklenmeyen bir hata oluştu.');
  }
}

/** Convenience type guard for concurrency (RowVersion) conflicts in feature code. */
export function isConcurrencyError(e: unknown): e is ApiError {
  return !!e && typeof e === 'object' && (e as ApiError).kind === 'conflict';
}

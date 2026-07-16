/**
 * Backend enum vocabularies for the Release domain (verbatim from Gms.Api.Common.ReleaseStatuses)
 * plus value→i18n-key helpers. Display labels resolve through the `releases` Transloco scope.
 * Status badges reuse the shared badge registry (STATUS_BADGES). Never send a display label to the backend.
 */

export const RELEASE_STATUS_VALUES = ['Planned', 'Scheduled', 'InProgress', 'Completed', 'Accepted', 'Cancelled'] as const;

export const RELEASE_TYPE_VALUES = ['Major', 'Minor', 'Patch', 'Hotfix', 'Emergency'] as const;

export const RELEASE_RISK_VALUES = ['Low', 'Medium', 'High', 'Critical'] as const;

export const releaseTypeKey = (v: string) => `releases.type.${v}`;
export const releaseAuditEventKey = (v: string) => `releases.audit.${v}`;

/** Statuses in which the release is still editable / actionable (backend enforces the rest). */
export const RELEASE_TERMINAL_STATUSES: ReadonlySet<string> = new Set(['Completed', 'Accepted', 'Cancelled']);

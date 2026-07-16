import { IconName } from '../../shared/icon/icon';

/**
 * Backend enum vocabularies for the Change domain (verbatim values from Gms.Api.Common.ChangeConstants)
 * plus mapping helpers to Turkish/i18n display labels and icons. Display labels resolve through the
 * `changes` Transloco scope (changes.class.*, changes.type.*, changes.priority.*, changes.status.*),
 * so both languages stay in the i18n files — this module only owns the value lists and the
 * value→i18nKey / value→icon mappings. Never send a display label to the backend.
 */

/* ── Enum value lists (exact backend strings) ─────────────── */

export const CHANGE_CLASS_VALUES = ['Standard', 'Normal', 'Emergency'] as const;

export const CHANGE_TYPE_VALUES = [
  'ApplicationDeployment', 'DatabaseSchemaChange', 'SqlDataFix', 'StoredProcedureFunctionChange',
  'ApiChange', 'ConfigurationChange', 'InfrastructureChange', 'IntegrationChange',
  'DocumentSopChange', 'Other'
] as const;

export const CHANGE_PRIORITY_VALUES = ['Low', 'Medium', 'High', 'Critical'] as const;

export const CHANGE_STATUS_VALUES = [
  'Draft', 'Submitted', 'UnderReview', 'Approved', 'Scheduled', 'Implemented', 'Cancelled'
] as const;

export const CHANGE_RISK_VALUES = ['Low', 'Medium', 'High', 'Critical'] as const;

/** Change types that touch SQL/DB objects → a rollback script is expected (mirrors backend SqlRelated). */
export const SQL_RELATED_TYPES: ReadonlySet<string> = new Set([
  'DatabaseSchemaChange', 'SqlDataFix', 'StoredProcedureFunctionChange'
]);

/* ── i18n key helpers (resolve under the `changes` scope) ── */

export const changeClassKey = (v: string) => `changes.class.${v}`;
export const changeTypeKey = (v: string) => `changes.type.${v}`;
export const changePriorityKey = (v: string) => `changes.priority.${v}`;
export const changeAuditEventKey = (v: string) => `changes.audit.${v}`;

/* ── Icon per change type (reuses the existing icon set) ──── */

const TYPE_ICONS: Record<string, IconName> = {
  ApplicationDeployment: 'dashboard',
  DatabaseSchemaChange: 'server',
  SqlDataFix: 'server',
  StoredProcedureFunctionChange: 'server',
  ApiChange: 'share',
  ConfigurationChange: 'filter',
  InfrastructureChange: 'server',
  IntegrationChange: 'hub',
  DocumentSopChange: 'document',
  Other: 'change'
};
export const changeTypeIcon = (v: string): IconName => TYPE_ICONS[v] ?? 'change';

/* ── Wizard kebab-key ↔ backend value bridge ──────────────── */

/**
 * The existing wizard schema (technical fields, icons) is keyed by short kebab keys. This maps those
 * to backend enum values so the wizard can keep its internal schema while sending real constants.
 */
export const KEBAB_TO_CHANGE_TYPE: Record<string, string> = {
  'app-deploy': 'ApplicationDeployment',
  'db-schema': 'DatabaseSchemaChange',
  'sql-fix': 'SqlDataFix',
  'sp-func': 'StoredProcedureFunctionChange',
  'api': 'ApiChange',
  'config': 'ConfigurationChange',
  'infra': 'InfrastructureChange',
  'integration': 'IntegrationChange',
  'doc-sop': 'DocumentSopChange',
  'other': 'Other'
};

export const CHANGE_TYPE_TO_KEBAB: Record<string, string> =
  Object.fromEntries(Object.entries(KEBAB_TO_CHANGE_TYPE).map(([k, v]) => [v, k]));

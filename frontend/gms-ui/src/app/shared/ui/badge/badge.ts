import { Component, computed, inject, input } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { LanguageService } from '../../../core/language.service';

export type BadgeTone = 'neutral' | 'info' | 'success' | 'warning' | 'danger';

export interface BadgeSpec {
  /** Translation key (looked up against the active language). Known registry entries use this. */
  labelKey?: string;
  /** Literal label — used for the "unknown value" fallback where there's no key to translate. */
  label?: string;
  tone: BadgeTone;
}

/** Release / change / execution status → badge (full governance lifecycle). */
export const STATUS_BADGES: Record<string, BadgeSpec> = {
  Draft: { labelKey: 'badge.status.Draft', tone: 'neutral' },
  Pending: { labelKey: 'badge.status.Pending', tone: 'warning' },
  Waiting: { labelKey: 'badge.status.Waiting', tone: 'info' },
  Expired: { labelKey: 'badge.status.Expired', tone: 'danger' },
  Passed: { labelKey: 'badge.status.Passed', tone: 'success' },
  Warning: { labelKey: 'badge.status.Warning', tone: 'warning' },
  Failed: { labelKey: 'badge.status.Failed', tone: 'danger' },
  Skipped: { labelKey: 'badge.status.Skipped', tone: 'neutral' },
  Open: { labelKey: 'badge.status.Open', tone: 'warning' },
  Resolved: { labelKey: 'badge.status.Resolved', tone: 'success' },
  Running: { labelKey: 'badge.status.Running', tone: 'info' },
  Paused: { labelKey: 'badge.status.Paused', tone: 'warning' },
  RolledBack: { labelKey: 'badge.status.RolledBack', tone: 'danger' },
  Active: { labelKey: 'badge.status.Active', tone: 'success' },
  Inactive: { labelKey: 'badge.status.Inactive', tone: 'neutral' },
  Deprecated: { labelKey: 'badge.status.Deprecated', tone: 'warning' },
  Archived: { labelKey: 'badge.status.Archived', tone: 'neutral' },
  Planning: { labelKey: 'badge.status.Planning', tone: 'info' },
  Planned: { labelKey: 'badge.status.Planned', tone: 'info' },
  InProgress: { labelKey: 'badge.status.InProgress', tone: 'warning' },
  Accepted: { labelKey: 'badge.status.Accepted', tone: 'success' },
  Validation: { labelKey: 'badge.status.Validation', tone: 'info' },
  InReview: { labelKey: 'badge.status.InReview', tone: 'warning' },
  UnderReview: { labelKey: 'badge.status.UnderReview', tone: 'warning' },
  Approval: { labelKey: 'badge.status.Approval', tone: 'warning' },
  Submitted: { labelKey: 'badge.status.Submitted', tone: 'info' },
  Approved: { labelKey: 'badge.status.Approved', tone: 'success' },
  Scheduled: { labelKey: 'badge.status.Scheduled', tone: 'info' },
  Implemented: { labelKey: 'badge.status.Implemented', tone: 'success' },
  Created: { labelKey: 'badge.status.Created', tone: 'neutral' },
  Ready: { labelKey: 'badge.status.Ready', tone: 'info' },
  Executing: { labelKey: 'badge.status.Executing', tone: 'warning' },
  Executed: { labelKey: 'badge.status.Executed', tone: 'success' },
  Completed: { labelKey: 'badge.status.Completed', tone: 'success' },
  Rejected: { labelKey: 'badge.status.Rejected', tone: 'danger' },
  Cancelled: { labelKey: 'badge.status.Cancelled', tone: 'neutral' }
};

/** Priority → badge. */
export const PRIORITY_BADGES: Record<string, BadgeSpec> = {
  Critical: { labelKey: 'badge.priority.Critical', tone: 'danger' },
  High: { labelKey: 'badge.priority.High', tone: 'warning' },
  Medium: { labelKey: 'badge.priority.Medium', tone: 'info' },
  Low: { labelKey: 'badge.priority.Low', tone: 'neutral' }
};

/** Risk → badge. */
export const RISK_BADGES: Record<string, BadgeSpec> = {
  Critical: { labelKey: 'badge.risk.Critical', tone: 'danger' },
  High: { labelKey: 'badge.risk.High', tone: 'danger' },
  Medium: { labelKey: 'badge.risk.Medium', tone: 'warning' },
  Low: { labelKey: 'badge.risk.Low', tone: 'success' }
};

/** Environment → badge. */
export const ENV_BADGES: Record<string, BadgeSpec> = {
  DEV: { label: 'DEV', tone: 'neutral' },
  TEST: { label: 'TEST', tone: 'info' },
  UAT: { label: 'UAT', tone: 'warning' },
  PREPROD: { label: 'PREPROD', tone: 'warning' },
  PROD: { label: 'PROD', tone: 'danger' }
};

/**
 * GMS badge. Either provide a `tone` + projected label,
 * or a `kind`+`value` pair to resolve from a registry.
 */
@Component({
  selector: 'gms-badge',
  standalone: true,
  template: `
    <span class="badge" [class]="toneClass()">
      @if (dot()) { <span class="badge__dot"></span> }
      {{ displayLabel() }}<ng-content></ng-content>
    </span>
  `
})
export class GmsBadge {
  private readonly transloco = inject(TranslocoService);
  private readonly language = inject(LanguageService);

  readonly tone = input<BadgeTone>('neutral');
  readonly dot = input(false);
  readonly kind = input<'status' | 'priority' | 'risk' | 'environment' | null>(null);
  readonly value = input<string | null>(null);

  protected readonly resolved = computed<BadgeSpec | null>(() => {
    const kind = this.kind();
    const value = this.value();
    if (!kind || !value) return null;
    const registry =
      kind === 'status' ? STATUS_BADGES
      : kind === 'priority' ? PRIORITY_BADGES
      : kind === 'risk' ? RISK_BADGES
      : ENV_BADGES;
    return registry[value] ?? { label: value, tone: 'neutral' };
  });

  protected readonly displayLabel = computed(() => {
    this.language.current();
    const r = this.resolved();
    if (!r) return '';
    return r.labelKey ? this.transloco.translate(r.labelKey) : (r.label ?? '');
  });

  protected readonly toneClass = computed(
    () => 'badge--' + (this.resolved()?.tone ?? this.tone())
  );
}

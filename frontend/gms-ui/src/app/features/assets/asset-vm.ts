import { TranslocoService } from '@jsverse/transloco';
import { GmsAsset, DependencyRelation, assetTypeIcon as assetTypeIconOf } from '../../core/asset.service';
import { ActivityItem } from '../../shared/ui/activity-feed/activity-feed';
import { TimelineItem } from '../../shared/ui/timeline/timeline';
import { LinkItem } from '../../shared/ui/item-list/item-list';
import { IconName } from '../../shared/icon/icon';

/**
 * Asset type catalog labels live in core/asset.service.ts (ASSET_TYPES) and are hardcoded
 * Turkish there. Since that core service is shared/out of scope, we keep our own translated
 * dictionary here (keyed by the same `type` key) independent of the core labels.
 */
const ASSET_TYPE_KEYS: Record<string, string> = {
  database: 'assets.type.database',
  table: 'assets.type.table',
  view: 'assets.type.view',
  'stored-procedure': 'assets.type.storedProcedure',
  function: 'assets.type.function',
  api: 'assets.type.api',
  service: 'assets.type.service',
  application: 'assets.type.application',
  microservice: 'assets.type.microservice',
  configuration: 'assets.type.configuration',
  'message-queue': 'assets.type.messageQueue',
  report: 'assets.type.report',
  file: 'assets.type.file',
  document: 'assets.type.document',
  infrastructure: 'assets.type.infrastructure',
  other: 'assets.type.other'
};

export function assetTypeLabel(key: string, t: TranslocoService): string {
  const labelKey = ASSET_TYPE_KEYS[key];
  return labelKey ? t.translate(labelKey) : key;
}

/**
 * DEPENDENCY_RELATIONS in core/asset.service.ts is also hardcoded Turkish — same
 * pattern: translate locally, independent of the core dictionary.
 */
const DEPENDENCY_RELATION_KEYS: Record<DependencyRelation, string> = {
  'depends-on': 'assets.dependencyRelation.dependsOn',
  'used-by': 'assets.dependencyRelation.usedBy',
  'connects-to': 'assets.dependencyRelation.connectsTo',
  contains: 'assets.dependencyRelation.contains'
};

export function dependencyRelationLabel(relation: DependencyRelation, t: TranslocoService): string {
  return t.translate(DEPENDENCY_RELATION_KEYS[relation] ?? relation);
}

/** Bottom timeline: Created → Modified → Used in Release → Referenced by Change → Archived. */
export function detailTimeline(a: GmsAsset, t: TranslocoService): TimelineItem[] {
  const c = new Date(a.createdAt).toLocaleDateString('tr-TR');
  const u = new Date(a.updatedAt).toLocaleDateString('tr-TR');
  const items: TimelineItem[] = [
    { title: t.translate('assets.detail.timeline.created'), time: c, tone: 'info', icon: 'plus', description: t.translate('assets.detail.timeline.createdBy', { owner: a.owner }) },
    { title: t.translate('assets.detail.timeline.updated'), time: u, tone: 'info', icon: 'change', description: t.translate('assets.detail.timeline.updatedDescription') }
  ];
  if (a.releases.length) {
    items.push({ title: t.translate('assets.detail.timeline.usedInRelease'), time: u, tone: 'success', icon: 'release', description: t.translate('assets.detail.timeline.usedInReleaseDescription', { code: a.releases[0].code }) });
  }
  if (a.changes.length) {
    items.push({ title: t.translate('assets.detail.timeline.referencedByChange'), time: u, tone: 'warning', icon: 'change', description: t.translate('assets.detail.timeline.referencedByChangeDescription', { code: a.changes[0].code }) });
  }
  items.push({
    title: a.status === 'Archived' ? t.translate('assets.detail.timeline.archived') : t.translate('assets.detail.timeline.archive'),
    time: a.status === 'Archived' ? u : t.translate('assets.detail.timeline.pending'),
    tone: 'neutral',
    icon: 'folder'
  });
  return items;
}

export function detailActivity(a: GmsAsset, t: TranslocoService): ActivityItem[] {
  return [
    { actor: a.owner, action: t.translate('assets.detail.activity.updatedConfiguration'), time: t.translate('assets.detail.activity.twoDaysAgo'), icon: 'change' },
    { actor: 'Mehmet Kaya', action: t.translate('assets.detail.activity.linkedToRelease'), time: t.translate('assets.detail.activity.fourDaysAgo'), icon: 'release' },
    { actor: 'Ayşe Yılmaz', action: t.translate('assets.detail.activity.reviewedDependencies'), time: t.translate('assets.detail.activity.oneWeekAgo'), icon: 'search' }
  ];
}

/** Right-panel: Recent Changes. */
export function recentChanges(a: GmsAsset): LinkItem[] {
  return a.changes.map((c): LinkItem => ({ id: c.id, label: c.code, hint: c.name, route: c.route, icon: 'change' }));
}

/** Right-panel: Recent Releases. */
export function recentReleases(a: GmsAsset): LinkItem[] {
  return a.releases.map((r): LinkItem => ({ id: r.id, label: r.code, hint: r.name, route: r.route, icon: 'release' }));
}

/** Right-panel: Related Documents. */
export function relatedDocuments(a: GmsAsset): LinkItem[] {
  return a.documents.map((d): LinkItem => ({ id: d.id, label: d.code, hint: d.name, route: d.route, icon: 'document' }));
}

/** A single node inside a relationship group. */
export interface RelationCard {
  code: string;
  name: string;
  meta: string; // relation label or object type
  icon: IconName;
  route: string | null;
}

/** A group of relationship cards, rendered as a lane in the Relationships tab. */
export interface RelationGroup {
  key: string;
  title: string;
  icon: IconName;
  accent: 'brand' | 'info' | 'success' | 'warning' | 'neutral';
  cards: RelationCard[];
  /** Placeholder groups (future integrations) render as "soon" without cards. */
  placeholder?: boolean;
}

/**
 * Build the enterprise relationship lanes for the Relationships tab. Groups the
 * asset's connections by object family. The dependency lane is the seed of the
 * future dependency-graph engine (not implemented here).
 */
export function relationshipGroups(a: GmsAsset, t: TranslocoService): RelationGroup[] {
  return [
    {
      key: 'project',
      title: t.translate('assets.relation.project'),
      icon: 'folder',
      accent: 'neutral',
      cards: [{ code: a.projectName, name: t.translate('assets.detail.environmentSuffix', { env: a.environment }), meta: t.translate('assets.relation.belongsToProject'), icon: 'folder', route: '/releases' }]
    },
    {
      key: 'releases',
      title: t.translate('assets.relation.releases'),
      icon: 'release',
      accent: 'success',
      cards: a.releases.map((r) => ({ code: r.code, name: r.name, meta: t.translate('assets.relation.release'), icon: 'release' as IconName, route: r.route }))
    },
    {
      key: 'changes',
      title: t.translate('assets.relation.changes'),
      icon: 'change',
      accent: 'warning',
      cards: a.changes.map((c) => ({ code: c.code, name: c.name, meta: t.translate('assets.relation.change'), icon: 'change' as IconName, route: c.route }))
    },
    {
      key: 'documents',
      title: t.translate('assets.relation.documents'),
      icon: 'document',
      accent: 'info',
      cards: a.documents.map((d) => ({ code: d.code, name: d.name, meta: t.translate('assets.relation.document'), icon: 'document' as IconName, route: d.route }))
    },
    {
      key: 'dependencies',
      title: t.translate('assets.relation.dependencies'),
      icon: 'share',
      accent: 'brand',
      cards: a.dependencies.map((d) => ({
        code: d.code,
        name: d.name,
        meta: `${dependencyRelationLabel(d.relation as DependencyRelation, t)} · ${assetTypeLabel(d.type, t)}`,
        icon: assetTypeIconOf(d.type),
        route: `/assets/${d.id}`
      }))
    },
    {
      key: 'future',
      title: t.translate('assets.relation.futureIntegrations'),
      icon: 'hub',
      accent: 'neutral',
      cards: [],
      placeholder: true
    }
  ];
}


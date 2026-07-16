// GMS Design System v1.0 — public component surface
export { GmsIcon } from '../icon/icon';
export type { IconName } from '../icon/icon';

export { GmsButton } from './button/button';
export type { ButtonVariant, ButtonSize } from './button/button';

export {
  GmsBadge,
  STATUS_BADGES,
  PRIORITY_BADGES,
  RISK_BADGES,
  ENV_BADGES
} from './badge/badge';
export type { BadgeTone, BadgeSpec } from './badge/badge';

export { GmsSkeleton } from './skeleton/skeleton';
export type { SkeletonVariant } from './skeleton/skeleton';

export { GmsEmptyState } from './empty-state/empty-state';
export { GmsMessage } from './message/message';
export type { MessageTone } from './message/message';

export { GmsBreadcrumbs } from './breadcrumbs/breadcrumbs';
export type { Crumb } from './breadcrumbs/breadcrumbs';
export { GmsPageHeader } from './page-header/page-header';
export type { PageStatus, HeaderContext } from './page-header/page-header';
export { GmsPage } from './page/page';
export { GmsContextSection } from './context-panel/context-section';
export { GmsRelationshipStrip } from './relationship-strip/relationship-strip';
export type { RelationNode } from './relationship-strip/relationship-strip';

export { GmsTimeline } from './timeline/timeline';
export type { TimelineItem } from './timeline/timeline';
export { GmsActivityFeed } from './activity-feed/activity-feed';
export type { ActivityItem } from './activity-feed/activity-feed';

export { GmsInput, GmsField } from './field/field';
export { GmsFormSection } from './form-section/form-section';
export { GmsMenu } from './menu/menu';
export type { MenuItem } from './menu/menu';

export { GmsTabs } from './tabs/tabs';
export type { TabItem } from './tabs/tabs';
export { GmsToolbar } from './toolbar/toolbar';
export { GmsFilterBar } from './filter-bar/filter-bar';
export type { QuickFilter } from './filter-bar/filter-bar';
export { GmsAuditList } from './audit-list/audit-list';
export type { AuditEntry } from './audit-list/audit-list';

export { ToastService, GmsToastHost } from './toast/toast';
export type { Toast, ToastTone } from './toast/toast';

export { GmsModal, GmsDrawer, ConfirmService, GmsConfirmHost } from './dialog/dialog';
export type { ConfirmOptions, ConfirmVariant } from './dialog/dialog';

export { GmsCommandPalette } from './command-palette/command-palette';
export { CommandService } from './command-palette/command.service';
export type { CommandEntry, CommandGroup } from './command-palette/command.service';

export { GmsDataGrid, GmsCellDef } from './data-grid/data-grid';
export type { ColumnDef, RowActionEvent } from './data-grid/data-grid';
export { STANDARD_ROW_ACTIONS, STANDARD_BULK_ACTIONS } from './data-grid/presets';

export { GmsState } from './state/state';
export type { PageStateVariant } from './state/state';
export { GmsUploadZone } from './upload/upload';
export type { UploadedFile, UploadKind } from './upload/upload';
export { GmsStat, GmsSparkline } from './stat/stat';
export type { StatTone } from './stat/stat';
export { GmsChart } from './chart/chart';
export type { ChartType, ChartDatum } from './chart/chart';
export { GmsSectionNav } from './section-nav/section-nav';
export type { SectionNavItem, SectionNavGroup } from './section-nav/section-nav';

export type { ToastAction, ToastOptions } from './toast/toast';

// Workspace layer (v3)
export { GmsWidget } from './widget/widget';
export { GmsWorkspaceHeader } from './workspace-header/workspace-header';
export type { WorkspaceEnv, WorkspaceContext } from './workspace-header/workspace-header';
export { GmsQuickCreate } from './quick-create/quick-create';
export type { QuickCreateAction } from './quick-create/quick-create';
export { GmsItemList } from './item-list/item-list';
export type { LinkItem } from './item-list/item-list';
export { GmsNotificationList } from './notification-list/notification-list';
export type { NotificationRow } from './notification-list/notification-list';

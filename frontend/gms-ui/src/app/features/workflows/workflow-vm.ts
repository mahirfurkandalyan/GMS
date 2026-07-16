import { GmsWorkflow, WorkflowNode, WorkflowEdge } from '../../core/workflow.service';
import { TimelineItem } from '../../shared/ui/timeline/timeline';

/** Node card dimensions — shared by the canvas layout + edge geometry. */
export const NODE_W = 158;
export const NODE_H = 62;

export interface EdgeGeometry {
  from: string;
  to: string;
  label?: string;
  path: string;
  midX: number;
  midY: number;
}

/**
 * Local translation-key maps for text that is sourced from `core/workflow.service.ts`
 * (WORKFLOW_CATEGORIES / NODE_TYPES) — that file is out of scope, so instead of
 * translating it directly, components look up the `workflows.*` key for a given
 * raw category/node key and resolve it via TranslocoService.
 */
const CATEGORY_LABEL_KEYS: Record<string, string> = {
  release: 'workflows.category.release',
  change: 'workflows.category.change',
  approval: 'workflows.category.approval',
  validation: 'workflows.category.validation',
  execution: 'workflows.category.execution'
};
export function categoryLabelKey(key: string): string {
  return CATEGORY_LABEL_KEYS[key] ?? key;
}

const NODE_LABEL_KEYS: Record<string, string> = {
  start: 'workflows.node.start',
  approval: 'workflows.node.approval',
  validation: 'workflows.node.validation',
  execution: 'workflows.node.execution',
  notification: 'workflows.node.notification',
  decision: 'workflows.node.decision',
  manual: 'workflows.node.manual',
  end: 'workflows.node.end'
};
export function nodeLabelKey(type: string): string {
  return NODE_LABEL_KEYS[type] ?? type;
}

const NODE_DESC_KEYS: Record<string, string> = {
  start: 'workflows.nodeDesc.start',
  approval: 'workflows.nodeDesc.approval',
  validation: 'workflows.nodeDesc.validation',
  execution: 'workflows.nodeDesc.execution',
  notification: 'workflows.nodeDesc.notification',
  decision: 'workflows.nodeDesc.decision',
  manual: 'workflows.nodeDesc.manual',
  end: 'workflows.nodeDesc.end'
};
export function nodeDescKey(type: string): string {
  return NODE_DESC_KEYS[type] ?? '';
}

/** Canvas bounds so the SVG/scroll area fits every node. */
export function canvasSize(nodes: WorkflowNode[]): { width: number; height: number } {
  const width = Math.max(600, ...nodes.map((n) => n.x + NODE_W + 40));
  const height = Math.max(320, ...nodes.map((n) => n.y + NODE_H + 40));
  return { width, height };
}

/**
 * Compute an orthogonal-ish connector path between two node cards. Exits the
 * right/bottom edge of `from` and enters the left/top edge of `to`. Pure geometry —
 * the future drag&drop engine will recompute this on move.
 */
export function edgeGeometry(nodes: WorkflowNode[], edges: WorkflowEdge[]): EdgeGeometry[] {
  const byId = new Map(nodes.map((n) => [n.id, n]));
  const result: EdgeGeometry[] = [];
  for (const e of edges) {
    const a = byId.get(e.from);
    const b = byId.get(e.to);
    if (!a || !b) continue;
    const sameRow = Math.abs(a.y - b.y) < NODE_H;
    let x1: number, y1: number, x2: number, y2: number, path: string;
    if (sameRow) {
      // Horizontal connector: right edge → left edge.
      x1 = a.x + NODE_W; y1 = a.y + NODE_H / 2;
      x2 = b.x; y2 = b.y + NODE_H / 2;
      const mx = (x1 + x2) / 2;
      path = `M ${x1} ${y1} C ${mx} ${y1}, ${mx} ${y2}, ${x2} ${y2}`;
    } else {
      // Branch connector: bottom edge → top edge.
      x1 = a.x + NODE_W / 2; y1 = a.y + NODE_H;
      x2 = b.x + NODE_W / 2; y2 = b.y;
      const my = (y1 + y2) / 2;
      path = `M ${x1} ${y1} C ${x1} ${my}, ${x2} ${my}, ${x2} ${y2}`;
    }
    result.push({ from: e.from, to: e.to, label: e.label, path, midX: (x1 + x2) / 2, midY: (y1 + y2) / 2 });
  }
  return result;
}

/**
 * Timeline builder — kept translation-agnostic (pure function). The caller passes a
 * `t()` translate function (bound to `TranslocoService.translate`) so this module
 * doesn't need Angular DI.
 */
export function detailTimeline(
  w: GmsWorkflow,
  t: (key: string, params?: Record<string, unknown>) => string
): TimelineItem[] {
  const c = new Date(w.createdAt).toLocaleDateString('tr-TR');
  const u = new Date(w.updatedAt).toLocaleDateString('tr-TR');
  const pending = t('workflows.detail.timeline.pending');
  return [
    { title: t('workflows.detail.timeline.created'), time: c, tone: 'info', icon: 'plus', description: t('workflows.detail.timeline.createdDesc', { createdBy: w.createdBy }) },
    { title: t('workflows.detail.timeline.updated'), time: u, tone: 'info', icon: 'change', description: t('workflows.detail.timeline.updatedDesc') },
    { title: t('workflows.detail.timeline.published'), time: w.status === 'Active' ? u : pending, tone: w.status === 'Active' ? 'success' : 'neutral', icon: 'check', description: t('workflows.detail.timeline.publishedDesc', { version: w.version }) },
    { title: t('workflows.detail.timeline.archived'), time: w.status === 'Archived' ? u : pending, tone: 'neutral', icon: 'folder' }
  ];
}

import { Injectable, signal } from '@angular/core';
import { Observable, of } from 'rxjs';
import { IconName } from '../shared/icon/icon';
import { BadgeTone } from '../shared/ui/badge/badge';

/**
 * Governance Workflow Center data layer. Models the *design* of governance
 * workflows (nodes + edges) — NOT their execution. Frontend-only, observable
 * based (`of()`) so a real workflow/orchestration API can replace the service
 * body without touching components. No engine, no rule evaluation here.
 */

export type WorkflowNodeType =
  | 'start' | 'approval' | 'validation' | 'execution' | 'notification' | 'decision' | 'manual' | 'end';

export interface NodeTypeMeta {
  label: string;
  icon: IconName;
  tone: BadgeTone;
  description: string;
}

/** Node palette — reused by the canvas, the right panel and the (future) toolbox. */
export const NODE_TYPES: Record<WorkflowNodeType, NodeTypeMeta> = {
  start: { label: 'Başlangıç', icon: 'execution', tone: 'success', description: 'İş akışının giriş noktası. Süreç buradan tetiklenir.' },
  approval: { label: 'Onay', icon: 'approval', tone: 'info', description: 'Bir veya daha fazla onaycının kararını bekleyen adım.' },
  validation: { label: 'Doğrulama', icon: 'shield', tone: 'info', description: 'Otomatik doğrulama kurallarının çalıştırıldığı adım.' },
  execution: { label: 'Yürütme', icon: 'execution', tone: 'warning', description: 'Onaylı işlemin hedef ortamda yürütüldüğü adım.' },
  notification: { label: 'Bildirim', icon: 'bell', tone: 'neutral', description: 'İlgili kişilere otomatik bildirim gönderen adım.' },
  decision: { label: 'Karar', icon: 'change', tone: 'warning', description: 'Koşula göre akışı dallandıran karar noktası.' },
  manual: { label: 'Manuel Adım', icon: 'user', tone: 'neutral', description: 'Bir kullanıcının elle tamamlaması gereken adım.' },
  end: { label: 'Bitiş', icon: 'check', tone: 'neutral', description: 'İş akışının tamamlandığı çıkış noktası.' }
};

export function nodeMeta(t: string): NodeTypeMeta {
  return NODE_TYPES[t as WorkflowNodeType] ?? { label: t, icon: 'audit', tone: 'neutral', description: '' };
}

export interface WorkflowNode {
  id: string;
  type: WorkflowNodeType;
  label: string;
  x: number; // canvas position (px)
  y: number;
  config?: string; // human-readable config summary (placeholder text)
}

export interface WorkflowEdge {
  from: string;
  to: string;
  label?: string; // e.g. branch condition label ("Evet" / "Hayır")
}

export interface WorkflowVariable {
  name: string;
  type: string;
  scope: string;
  description: string;
}

export const WORKFLOW_CATEGORIES = [
  { key: 'release', label: 'Yayın' },
  { key: 'change', label: 'Değişiklik' },
  { key: 'approval', label: 'Onay' },
  { key: 'validation', label: 'Doğrulama' },
  { key: 'execution', label: 'Yürütme' }
];
export function categoryLabel(key: string): string {
  return WORKFLOW_CATEGORIES.find((c) => c.key === key)?.label ?? key;
}

export const WORKFLOW_STATUSES = ['Draft', 'Active', 'Inactive', 'Archived'];

export interface GmsWorkflow {
  id: string;
  code: string;
  name: string;
  category: string;
  version: string;
  status: string;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
  usedBy: number;
  description: string;
  usedModules: string[];
  nodes: WorkflowNode[];
  edges: WorkflowEdge[];
  variables: WorkflowVariable[];
}

export interface CreateWorkflowPayload {
  name: string;
  category: string;
  description: string;
  createdBy: string;
}

const KEY = 'gms.workflows';

/** Default starter graph for a freshly created workflow. */
function starterGraph(): { nodes: WorkflowNode[]; edges: WorkflowEdge[] } {
  return {
    nodes: [
      { id: 'n1', type: 'start', label: 'Başlangıç', x: 40, y: 60 },
      { id: 'n2', type: 'manual', label: 'İlk Adım', x: 260, y: 60, config: 'Atanan: —' },
      { id: 'n3', type: 'end', label: 'Bitiş', x: 480, y: 60 }
    ],
    edges: [
      { from: 'n1', to: 'n2' },
      { from: 'n2', to: 'n3' }
    ]
  };
}

const RELEASE_WF: GmsWorkflow = {
  id: 'wf-release', code: 'WF-2026-001', name: 'Yayın İş Akışı', category: 'release', version: 'v3', status: 'Active',
  createdBy: 'Furkan Demir', createdAt: '2025-11-10T09:00:00', updatedAt: '2026-06-28T14:00:00', usedBy: 7,
  description: 'Standart yayın yönetişim süreci: onay, doğrulama ve üretim yürütmesini uçtan uca yönetir.',
  usedModules: ['Yayın', 'Onay', 'Doğrulama', 'Yürütme'],
  nodes: [
    { id: 'n1', type: 'start', label: 'Başlangıç', x: 40, y: 60 },
    { id: 'n2', type: 'approval', label: 'Yönetim Onayı', x: 240, y: 60, config: 'Onaycı: Mimar · QA' },
    { id: 'n3', type: 'decision', label: 'Risk Kontrolü', x: 440, y: 60, config: 'Koşul: risk = Yüksek' },
    { id: 'n4', type: 'validation', label: 'Doğrulama', x: 640, y: 60, config: 'Kural seti: CSV' },
    { id: 'n5', type: 'notification', label: 'Ekip Bildirimi', x: 440, y: 210, config: 'Kanal: E-posta' },
    { id: 'n6', type: 'execution', label: 'Üretim Yürütmesi', x: 840, y: 60, config: 'Ortam: PROD' },
    { id: 'n7', type: 'end', label: 'Bitiş', x: 1040, y: 60 }
  ],
  edges: [
    { from: 'n1', to: 'n2' },
    { from: 'n2', to: 'n3' },
    { from: 'n3', to: 'n4', label: 'Evet' },
    { from: 'n3', to: 'n5', label: 'Hayır' },
    { from: 'n4', to: 'n6' },
    { from: 'n6', to: 'n7' },
    { from: 'n5', to: 'n7' }
  ],
  variables: [
    { name: 'releaseId', type: 'string', scope: 'global', description: 'İşlenen yayının kimliği.' },
    { name: 'riskLevel', type: 'enum', scope: 'global', description: 'Yayının risk seviyesi (Yüksek/Orta/Düşük).' },
    { name: 'approved', type: 'boolean', scope: 'step', description: 'Onay adımının sonucu.' }
  ]
};

const CHANGE_WF: GmsWorkflow = {
  id: 'wf-change', code: 'WF-2026-002', name: 'Değişiklik İş Akışı', category: 'change', version: 'v2', status: 'Active',
  createdBy: 'Ayşe Yılmaz', createdAt: '2025-12-01T09:00:00', updatedAt: '2026-05-20T10:00:00', usedBy: 5,
  description: 'Değişiklik taleplerinin inceleme, onay ve uygulama sürecini yönetir.',
  usedModules: ['Değişiklik', 'Onay'],
  nodes: [
    { id: 'n1', type: 'start', label: 'Başlangıç', x: 40, y: 60 },
    { id: 'n2', type: 'manual', label: 'İnceleme', x: 240, y: 60, config: 'Atanan: Mimar' },
    { id: 'n3', type: 'approval', label: 'CAB Onayı', x: 440, y: 60, config: 'Onaycı: CAB' },
    { id: 'n4', type: 'notification', label: 'Bildirim', x: 640, y: 60 },
    { id: 'n5', type: 'end', label: 'Bitiş', x: 840, y: 60 }
  ],
  edges: [
    { from: 'n1', to: 'n2' }, { from: 'n2', to: 'n3' }, { from: 'n3', to: 'n4' }, { from: 'n4', to: 'n5' }
  ],
  variables: [
    { name: 'changeId', type: 'string', scope: 'global', description: 'İşlenen değişikliğin kimliği.' },
    { name: 'cabApproved', type: 'boolean', scope: 'step', description: 'CAB onay sonucu.' }
  ]
};

const APPROVAL_WF: GmsWorkflow = {
  id: 'wf-approval', code: 'WF-2026-003', name: 'Onay Zinciri İş Akışı', category: 'approval', version: 'v1', status: 'Draft',
  createdBy: 'Zeynep Şahin', createdAt: '2026-03-15T09:00:00', updatedAt: '2026-06-10T09:00:00', usedBy: 0,
  description: 'Çok aşamalı onay zinciri için taslak süreç.',
  usedModules: ['Onay'],
  nodes: [
    { id: 'n1', type: 'start', label: 'Başlangıç', x: 40, y: 60 },
    { id: 'n2', type: 'approval', label: '1. Seviye Onay', x: 240, y: 60 },
    { id: 'n3', type: 'approval', label: '2. Seviye Onay', x: 440, y: 60 },
    { id: 'n4', type: 'end', label: 'Bitiş', x: 640, y: 60 }
  ],
  edges: [{ from: 'n1', to: 'n2' }, { from: 'n2', to: 'n3' }, { from: 'n3', to: 'n4' }],
  variables: []
};

const VALIDATION_WF: GmsWorkflow = {
  id: 'wf-validation', code: 'WF-2026-004', name: 'Doğrulama İş Akışı', category: 'validation', version: 'v2', status: 'Inactive',
  createdBy: 'Mehmet Kaya', createdAt: '2026-01-20T09:00:00', updatedAt: '2026-04-01T09:00:00', usedBy: 3,
  description: 'Bilgisayarlı sistem doğrulama kurallarının otomatik çalıştırılması.',
  usedModules: ['Doğrulama'],
  nodes: [
    { id: 'n1', type: 'start', label: 'Başlangıç', x: 40, y: 60 },
    { id: 'n2', type: 'validation', label: 'Kural Çalıştırma', x: 240, y: 60 },
    { id: 'n3', type: 'decision', label: 'Sonuç', x: 440, y: 60, config: 'Koşul: bulgu = 0' },
    { id: 'n4', type: 'end', label: 'Bitiş', x: 640, y: 60 }
  ],
  edges: [{ from: 'n1', to: 'n2' }, { from: 'n2', to: 'n3' }, { from: 'n3', to: 'n4', label: 'Geçti' }],
  variables: []
};

const EXECUTION_WF: GmsWorkflow = {
  id: 'wf-execution', code: 'WF-2026-005', name: 'Yürütme İş Akışı', category: 'execution', version: 'v1', status: 'Archived',
  createdBy: 'Ali Vural', createdAt: '2025-09-01T09:00:00', updatedAt: '2025-12-15T09:00:00', usedBy: 2,
  description: 'Onaylı yayınların üretim ortamında yürütülmesi için arşivlenmiş süreç.',
  usedModules: ['Yürütme'],
  nodes: [
    { id: 'n1', type: 'start', label: 'Başlangıç', x: 40, y: 60 },
    { id: 'n2', type: 'execution', label: 'Yürütme', x: 240, y: 60 },
    { id: 'n3', type: 'notification', label: 'Sonuç Bildirimi', x: 440, y: 60 },
    { id: 'n4', type: 'end', label: 'Bitiş', x: 640, y: 60 }
  ],
  edges: [{ from: 'n1', to: 'n2' }, { from: 'n2', to: 'n3' }, { from: 'n3', to: 'n4' }],
  variables: []
};

const SEED: GmsWorkflow[] = [RELEASE_WF, CHANGE_WF, APPROVAL_WF, VALIDATION_WF, EXECUTION_WF];

@Injectable({ providedIn: 'root' })
export class WorkflowService {
  private readonly store = signal<GmsWorkflow[]>(this.load());
  private seq = 5;

  getWorkflows(): Observable<GmsWorkflow[]> {
    return of(this.store());
  }
  getWorkflow(id: string): Observable<GmsWorkflow | undefined> {
    return of(this.store().find((w) => w.id === id));
  }

  create(payload: CreateWorkflowPayload): Observable<GmsWorkflow> {
    const n = String(++this.seq).padStart(3, '0');
    const now = new Date().toISOString();
    const graph = starterGraph();
    const wf: GmsWorkflow = {
      id: 'wf-' + this.seq,
      code: `WF-2026-${n}`,
      name: payload.name,
      category: payload.category,
      version: 'v1',
      status: 'Draft',
      createdBy: payload.createdBy,
      createdAt: now,
      updatedAt: now,
      usedBy: 0,
      description: payload.description,
      usedModules: [categoryLabel(payload.category)],
      nodes: graph.nodes,
      edges: graph.edges,
      variables: []
    };
    this.store.update((list) => [wf, ...list]);
    this.persist();
    return of(wf);
  }

  private load(): GmsWorkflow[] {
    try {
      const raw = localStorage.getItem(KEY);
      return raw ? (JSON.parse(raw) as GmsWorkflow[]) : SEED;
    } catch {
      return SEED;
    }
  }
  private persist(): void {
    try {
      localStorage.setItem(KEY, JSON.stringify(this.store()));
    } catch {
      /* ignore */
    }
  }
}

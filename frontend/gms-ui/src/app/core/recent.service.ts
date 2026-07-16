import { Injectable, signal } from '@angular/core';
import { IconName } from '../shared/icon/icon';

export type WorkItemType = 'release' | 'change' | 'document' | 'project' | 'asset';

export interface RecentItem {
  id: string;
  type: WorkItemType;
  label: string;
  hint?: string;
  route: string;
  icon: IconName;
  at: number;
}

const KEY = 'gms.recent';
const MAX = 8;

const SEED: RecentItem[] = [
  { id: 'r1', type: 'release', label: 'REL-2026-001', hint: 'EBR Migration · PROD', route: '/releases', icon: 'release', at: 6 },
  { id: 'p1', type: 'project', label: 'EBR Migration', hint: 'Abdi İbrahim', route: '/releases', icon: 'folder', at: 5 },
  { id: 'r2', type: 'release', label: 'REL-2026-002', hint: 'MES Upgrade · UAT', route: '/releases', icon: 'release', at: 4 },
  { id: 'e1', type: 'project', label: 'Çalışanlar', hint: 'Organizasyon', route: '/employees', icon: 'employees', at: 3 }
];

/** Recently-opened work items (releases/changes/documents/projects). Persisted locally. */
@Injectable({ providedIn: 'root' })
export class RecentService {
  private readonly store = signal<RecentItem[]>(this.load());
  readonly items = this.store.asReadonly();

  add(item: Omit<RecentItem, 'at'>): void {
    const at = Date.now();
    this.store.update((list) => {
      const deduped = list.filter((i) => i.id !== item.id);
      return [{ ...item, at }, ...deduped].slice(0, MAX);
    });
    this.persist();
  }

  clear(): void {
    this.store.set([]);
    this.persist();
  }

  private load(): RecentItem[] {
    try {
      const raw = localStorage.getItem(KEY);
      return raw ? (JSON.parse(raw) as RecentItem[]) : SEED;
    } catch {
      return SEED;
    }
  }

  private persist(): void {
    try {
      localStorage.setItem(KEY, JSON.stringify(this.store()));
    } catch {
      /* ignore quota errors in PoC */
    }
  }
}

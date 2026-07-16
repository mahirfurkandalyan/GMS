import { Injectable, signal } from '@angular/core';
import { IconName } from '../shared/icon/icon';
import { WorkItemType } from './recent.service';

export interface FavoriteItem {
  id: string;
  type: WorkItemType;
  label: string;
  hint?: string;
  route: string;
  icon: IconName;
}

const KEY = 'gms.favorites';

const SEED: FavoriteItem[] = [
  { id: 'fav-ebr', type: 'project', label: 'EBR Migration', hint: 'Abdi İbrahim', route: '/releases', icon: 'folder' },
  { id: 'fav-rel1', type: 'release', label: 'REL-2026-001', hint: 'PROD yayını', route: '/releases', icon: 'release' }
];

/** Pinned favorites (projects/releases/changes/documents). Persisted locally. */
@Injectable({ providedIn: 'root' })
export class FavoritesService {
  private readonly store = signal<FavoriteItem[]>(this.load());
  readonly items = this.store.asReadonly();

  isFavorite(id: string): boolean {
    return this.store().some((i) => i.id === id);
  }

  toggle(item: FavoriteItem): void {
    this.store.update((list) =>
      list.some((i) => i.id === item.id)
        ? list.filter((i) => i.id !== item.id)
        : [item, ...list]
    );
    this.persist();
  }

  remove(id: string): void {
    this.store.update((list) => list.filter((i) => i.id !== id));
    this.persist();
  }

  private load(): FavoriteItem[] {
    try {
      const raw = localStorage.getItem(KEY);
      return raw ? (JSON.parse(raw) as FavoriteItem[]) : SEED;
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

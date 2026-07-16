import { Injectable, signal } from '@angular/core';

const KEY = 'gms.searchHistory';
const PIN_KEY = 'gms.searchPinned';
const MAX = 6;

/** Static suggestions until AI-powered suggestions land. */
export const SEARCH_SUGGESTIONS: string[] = [
  'Planlanan yayınlar',
  'PROD ortamı',
  'Bekleyen onaylar',
  'EBR Migration'
];

/** Recent + pinned global searches, persisted locally. */
@Injectable({ providedIn: 'root' })
export class SearchHistoryService {
  private readonly store = signal<string[]>(this.load(KEY));
  private readonly pins = signal<string[]>(this.load(PIN_KEY));

  readonly recent = this.store.asReadonly();
  readonly pinned = this.pins.asReadonly();
  readonly suggestions = signal<string[]>(SEARCH_SUGGESTIONS).asReadonly();

  add(term: string): void {
    const t = term.trim();
    if (!t) return;
    this.store.update((list) => [t, ...list.filter((x) => x.toLowerCase() !== t.toLowerCase())].slice(0, MAX));
    this.persist(KEY, this.store());
  }

  isPinned(term: string): boolean {
    return this.pins().some((p) => p.toLowerCase() === term.toLowerCase());
  }

  togglePin(term: string): void {
    const t = term.trim();
    if (!t) return;
    this.pins.update((list) =>
      list.some((p) => p.toLowerCase() === t.toLowerCase())
        ? list.filter((p) => p.toLowerCase() !== t.toLowerCase())
        : [t, ...list].slice(0, MAX)
    );
    this.persist(PIN_KEY, this.pins());
  }

  clear(): void {
    this.store.set([]);
    this.persist(KEY, []);
  }

  private load(key: string): string[] {
    try {
      const raw = localStorage.getItem(key);
      return raw ? (JSON.parse(raw) as string[]) : [];
    } catch {
      return [];
    }
  }

  private persist(key: string, value: string[]): void {
    try {
      localStorage.setItem(key, JSON.stringify(value));
    } catch {
      /* ignore */
    }
  }
}

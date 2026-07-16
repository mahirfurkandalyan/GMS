import { Injectable } from '@angular/core';

export type SearchCategory = 'Çalışan' | 'Proje' | 'Yayın' | 'Değişiklik' | 'Doküman';

export interface SearchResult {
  category: SearchCategory;
  label: string;
  hint: string;
  route: string | null;
}

// PoC — arama için mock kayıt havuzu.
const INDEX: SearchResult[] = [
  { category: 'Çalışan', label: 'Furkan Demir', hint: 'Yazılım Mimarı', route: '/employees/emp-01' },
  { category: 'Çalışan', label: 'Ayşe Yılmaz', hint: 'Kıdemli Geliştirici', route: '/employees/emp-02' },
  { category: 'Çalışan', label: 'Mehmet Kaya', hint: 'QA Uzmanı', route: '/employees/emp-03' },
  { category: 'Çalışan', label: 'Zeynep Şahin', hint: 'Proje Yöneticisi', route: '/employees/emp-04' },
  { category: 'Proje', label: 'EBR Migration', hint: 'Abdi İbrahim', route: '/releases' },
  { category: 'Proje', label: 'MES Upgrade', hint: 'Bilim İlaç', route: '/releases' },
  { category: 'Yayın', label: 'REL-2026-001', hint: 'EBR Migration · PROD', route: '/releases' },
  { category: 'Yayın', label: 'REL-2026-002', hint: 'MES Upgrade · UAT', route: '/releases' },
  { category: 'Değişiklik', label: 'CHG-2026-014', hint: 'Yakında', route: null },
  { category: 'Doküman', label: 'Validasyon Planı v2', hint: 'Yakında', route: null }
];

@Injectable({ providedIn: 'root' })
export class SearchService {
  search(term: string): SearchResult[] {
    const q = term.trim().toLocaleLowerCase('tr');
    if (!q) {
      return [];
    }
    return INDEX.filter(
      (r) =>
        r.label.toLocaleLowerCase('tr').includes(q) ||
        r.hint.toLocaleLowerCase('tr').includes(q) ||
        r.category.toLocaleLowerCase('tr').includes(q)
    ).slice(0, 8);
  }
}

import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';

export type CalendarType = 'available' | 'meeting' | 'leave' | 'remote' | 'travel';

export interface CalendarEntry {
  id: string;
  person: string;
  department: string;
  type: CalendarType;
  startDate: string; // ISO date
  endDate: string;
  note: string;
}

export interface LeaveBalance {
  person: string;
  entitled: number;
  used: number;
  remaining: number;
}

export const CALENDAR_TYPE_META: Record<CalendarType, { label: string; badge: string; dot: string }> = {
  available: { label: 'Müsait', badge: 'badge--green', dot: 'dot--green' },
  meeting: { label: 'Toplantı', badge: 'badge--amber', dot: 'dot--amber' },
  leave: { label: 'İzinli', badge: 'badge--red', dot: 'dot--red' },
  remote: { label: 'Uzaktan', badge: 'badge--blue', dot: 'dot--green' },
  travel: { label: 'Seyahat', badge: 'badge--gray', dot: 'dot--gray' }
};

// Yönetici/normal kullanıcıya göre görünürlük filtresi için tam liste.
const ENTRIES: CalendarEntry[] = [
  { id: 'cal-01', person: 'Zeynep Şahin', department: 'PMO', type: 'leave', startDate: '2026-07-06', endDate: '2026-07-10', note: 'Yıllık izin' },
  { id: 'cal-02', person: 'Ayşe Yılmaz', department: 'Mühendislik', type: 'meeting', startDate: '2026-07-06', endDate: '2026-07-06', note: 'MES sprint toplantısı' },
  { id: 'cal-03', person: 'Ali Vural', department: 'Altyapı', type: 'remote', startDate: '2026-07-06', endDate: '2026-07-08', note: 'Uzaktan çalışma' },
  { id: 'cal-04', person: 'Furkan Demir', department: 'Mühendislik', type: 'travel', startDate: '2026-07-09', endDate: '2026-07-11', note: 'Müşteri ziyareti — Abdi İbrahim' },
  { id: 'cal-05', person: 'Mehmet Kaya', department: 'Kalite', type: 'available', startDate: '2026-07-06', endDate: '2026-07-06', note: 'Ofiste' },
  { id: 'cal-06', person: 'Elif Aydın', department: 'PMO', type: 'meeting', startDate: '2026-07-07', endDate: '2026-07-07', note: 'Gereksinim çalıştayı' }
];

const BALANCES: LeaveBalance[] = [
  { person: 'Furkan Demir', entitled: 20, used: 6, remaining: 14 },
  { person: 'Ayşe Yılmaz', entitled: 20, used: 9, remaining: 11 },
  { person: 'Mehmet Kaya', entitled: 20, used: 4, remaining: 16 },
  { person: 'Zeynep Şahin', entitled: 22, used: 15, remaining: 7 },
  { person: 'Ali Vural', entitled: 20, used: 2, remaining: 18 },
  { person: 'Elif Aydın', entitled: 20, used: 8, remaining: 12 }
];

// Normal kullanıcıların görebileceği kategoriler.
const USER_VISIBLE: CalendarType[] = ['available', 'meeting', 'leave'];

@Injectable({ providedIn: 'root' })
export class LeaveService {
  /**
   * Rol bazlı görünürlük: yöneticiler tüm kategorileri, normal kullanıcılar
   * yalnızca müsait/toplantı/izin bilgisini görür.
   */
  getCalendar(isManager: boolean): Observable<CalendarEntry[]> {
    if (isManager) {
      return of(ENTRIES);
    }
    return of(ENTRIES.filter((e) => USER_VISIBLE.includes(e.type)));
  }

  /** Yalnızca yönetici/admin için izin bakiyeleri. */
  getBalances(): Observable<LeaveBalance[]> {
    return of(BALANCES);
  }
}

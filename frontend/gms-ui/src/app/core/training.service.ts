import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';

export type TrainingStatus = 'assigned' | 'completed' | 'expired' | 'upcoming';

export interface Training {
  id: string;
  title: string;
  category: string;
  status: TrainingStatus;
  dueDate: string; // ISO
  progress: number; // 0..100
}

export interface Certificate {
  id: string;
  name: string;
  issuedBy: string;
  validUntil: string;
}

export const TRAINING_STATUS_META: Record<TrainingStatus, { labelKey: string; badge: string }> = {
  assigned: { labelKey: 'training.status.assigned', badge: 'badge--blue' },
  completed: { labelKey: 'training.status.completed', badge: 'badge--green' },
  expired: { labelKey: 'training.status.expired', badge: 'badge--red' },
  upcoming: { labelKey: 'training.status.upcoming', badge: 'badge--amber' }
};

const TRAININGS: Training[] = [
  { id: 't-01', title: 'GxP Temelleri', category: 'Uyumluluk', status: 'assigned', dueDate: '2026-08-15T00:00:00', progress: 40 },
  { id: 't-02', title: 'Bilgi Güvenliği Farkındalığı', category: 'Güvenlik', status: 'completed', dueDate: '2026-05-01T00:00:00', progress: 100 },
  { id: 't-03', title: 'Bilgisayarlı Sistem Validasyonu', category: 'Kalite', status: 'upcoming', dueDate: '2026-09-10T00:00:00', progress: 0 },
  { id: 't-04', title: 'KVKK ve Veri Koruma', category: 'Uyumluluk', status: 'expired', dueDate: '2026-03-01T00:00:00', progress: 100 },
  { id: 't-05', title: 'Angular İleri Seviye', category: 'Teknik', status: 'assigned', dueDate: '2026-08-30T00:00:00', progress: 65 },
  { id: 't-06', title: 'Çevik Proje Yönetimi', category: 'Metodoloji', status: 'upcoming', dueDate: '2026-10-05T00:00:00', progress: 0 }
];

const CERTIFICATES: Certificate[] = [
  { id: 'c-01', name: 'Azure Developer Associate', issuedBy: 'Microsoft', validUntil: '2027-01-01' },
  { id: 'c-02', name: 'ISTQB Foundation', issuedBy: 'ISTQB', validUntil: '2028-06-01' }
];

@Injectable({ providedIn: 'root' })
export class TrainingService {
  getTrainings(): Observable<Training[]> {
    return of(TRAININGS);
  }

  getCertificates(): Observable<Certificate[]> {
    return of(CERTIFICATES);
  }
}

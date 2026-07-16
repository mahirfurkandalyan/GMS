import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';

export interface Department {
  id: string;
  name: string;
  lead: string;
  memberCount: number;
  description: string;
}

export interface Team {
  id: string;
  name: string;
  department: string;
  lead: string;
  members: string[];
  focus: string;
}

const DEPARTMENTS: Department[] = [
  { id: 'dep-eng', name: 'Mühendislik', lead: 'Furkan Demir', memberCount: 12, description: 'Ürün ve platform geliştirme.' },
  { id: 'dep-qa', name: 'Kalite', lead: 'Mehmet Kaya', memberCount: 5, description: 'Doğrulama, test ve kalite güvence.' },
  { id: 'dep-pmo', name: 'PMO', lead: 'Zeynep Şahin', memberCount: 4, description: 'Proje ve portföy yönetimi.' },
  { id: 'dep-inf', name: 'Altyapı', lead: 'Ali Vural', memberCount: 6, description: 'Sistem, ağ ve DevOps operasyonları.' }
];

const TEAMS: Team[] = [
  { id: 'team-ebr', name: 'EBR Çekirdek Takımı', department: 'Mühendislik', lead: 'Furkan Demir', members: ['Ayşe Yılmaz', 'Ali Vural', 'Mehmet Kaya'], focus: 'EBR Migration teslimatı' },
  { id: 'team-mes', name: 'MES Takımı', department: 'Mühendislik', lead: 'Ayşe Yılmaz', members: ['Elif Aydın', 'Zeynep Şahin'], focus: 'MES Upgrade projesi' },
  { id: 'team-qa', name: 'Validasyon Takımı', department: 'Kalite', lead: 'Mehmet Kaya', members: ['Elif Aydın'], focus: 'CSV ve doğrulama' }
];

@Injectable({ providedIn: 'root' })
export class OrganizationService {
  getDepartments(): Observable<Department[]> {
    return of(DEPARTMENTS);
  }

  getTeams(): Observable<Team[]> {
    return of(TEAMS);
  }
}

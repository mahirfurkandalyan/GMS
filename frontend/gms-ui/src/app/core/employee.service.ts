import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';

export type Availability = 'available' | 'meeting' | 'leave' | 'offline';

export interface Skill {
  name: string;
  level: number; // 1..5
}

export interface Employee {
  id: string;
  fullName: string;
  title: string;
  department: string;
  email: string;
  phone: string;
  availability: Availability;
  currentProject: string;
  skills: Skill[];
  trainings: string[];
  certificates: string[];
  currentTasks: string[];
}

export const AVAILABILITY_META: Record<Availability, { label: string; dot: string }> = {
  available: { label: 'Müsait', dot: 'dot--green' },
  meeting: { label: 'Toplantıda', dot: 'dot--amber' },
  leave: { label: 'İzinli', dot: 'dot--red' },
  offline: { label: 'Çevrimdışı', dot: 'dot--gray' }
};

const EMPLOYEES: Employee[] = [
  {
    id: 'emp-01',
    fullName: 'Furkan Demir',
    title: 'Yazılım Mimarı',
    department: 'Mühendislik',
    email: 'furkan.demir@gms.local',
    phone: '+90 532 000 0001',
    availability: 'available',
    currentProject: 'EBR Migration',
    skills: [
      { name: 'SQL', level: 5 },
      { name: 'Angular', level: 4 },
      { name: '.NET', level: 5 },
      { name: 'Validation', level: 3 },
      { name: 'Oracle', level: 4 }
    ],
    trainings: ['GxP Temelleri', 'Angular İleri Seviye'],
    certificates: ['Microsoft Certified: Azure Developer'],
    currentTasks: ['EBR PROD yayını hazırlığı', 'Mimari gözden geçirme']
  },
  {
    id: 'emp-02',
    fullName: 'Ayşe Yılmaz',
    title: 'Kıdemli Geliştirici',
    department: 'Mühendislik',
    email: 'ayse.yilmaz@gms.local',
    phone: '+90 532 000 0002',
    availability: 'meeting',
    currentProject: 'MES Upgrade',
    skills: [
      { name: '.NET', level: 5 },
      { name: 'SQL', level: 4 },
      { name: 'Angular', level: 3 },
      { name: 'Azure', level: 4 }
    ],
    trainings: ['Bilgi Güvenliği', 'CI/CD Süreçleri'],
    certificates: ['Scrum Master (PSM I)'],
    currentTasks: ['MES UAT test senaryoları']
  },
  {
    id: 'emp-03',
    fullName: 'Mehmet Kaya',
    title: 'QA Uzmanı',
    department: 'Kalite',
    email: 'mehmet.kaya@gms.local',
    phone: '+90 532 000 0003',
    availability: 'available',
    currentProject: 'EBR Migration',
    skills: [
      { name: 'Validation', level: 5 },
      { name: 'Test Otomasyon', level: 4 },
      { name: 'SQL', level: 3 }
    ],
    trainings: ['Bilgisayarlı Sistem Validasyonu'],
    certificates: ['ISTQB Foundation'],
    currentTasks: ['Doğrulama protokolü hazırlığı']
  },
  {
    id: 'emp-04',
    fullName: 'Zeynep Şahin',
    title: 'Proje Yöneticisi',
    department: 'PMO',
    email: 'zeynep.sahin@gms.local',
    phone: '+90 532 000 0004',
    availability: 'leave',
    currentProject: 'MES Upgrade',
    skills: [
      { name: 'Proje Yönetimi', level: 5 },
      { name: 'Risk Yönetimi', level: 4 },
      { name: 'Raporlama', level: 4 }
    ],
    trainings: ['PMP Hazırlık'],
    certificates: ['PRINCE2 Practitioner'],
    currentTasks: ['Sprint planlama', 'Paydaş toplantısı']
  },
  {
    id: 'emp-05',
    fullName: 'Ali Vural',
    title: 'DevOps Mühendisi',
    department: 'Altyapı',
    email: 'ali.vural@gms.local',
    phone: '+90 532 000 0005',
    availability: 'offline',
    currentProject: 'EBR Migration',
    skills: [
      { name: 'Docker', level: 5 },
      { name: 'SQL Server', level: 4 },
      { name: 'CI/CD', level: 5 },
      { name: 'Azure', level: 4 }
    ],
    trainings: ['Kubernetes Temelleri'],
    certificates: ['Azure Administrator Associate'],
    currentTasks: ['Pipeline optimizasyonu']
  },
  {
    id: 'emp-06',
    fullName: 'Elif Aydın',
    title: 'İş Analisti',
    department: 'PMO',
    email: 'elif.aydin@gms.local',
    phone: '+90 532 000 0006',
    availability: 'available',
    currentProject: 'MES Upgrade',
    skills: [
      { name: 'İş Analizi', level: 5 },
      { name: 'Süreç Modelleme', level: 4 },
      { name: 'SQL', level: 3 }
    ],
    trainings: ['UML ve BPMN'],
    certificates: ['CBAP'],
    currentTasks: ['Gereksinim dokümantasyonu']
  }
];

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  getEmployees(): Observable<Employee[]> {
    return of(EMPLOYEES);
  }

  getEmployee(id: string): Observable<Employee | undefined> {
    return of(EMPLOYEES.find((e) => e.id === id));
  }

  getDepartments(): string[] {
    return [...new Set(EMPLOYEES.map((e) => e.department))];
  }

  getSkills(): string[] {
    return [...new Set(EMPLOYEES.flatMap((e) => e.skills.map((s) => s.name)))].sort();
  }

  getProjects(): string[] {
    return [...new Set(EMPLOYEES.map((e) => e.currentProject))];
  }
}

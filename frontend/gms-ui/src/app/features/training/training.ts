import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslocoPipe, provideTranslocoScope } from '@jsverse/transloco';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import {
  TrainingService,
  Training,
  Certificate,
  TRAINING_STATUS_META,
  TrainingStatus
} from '../../core/training.service';

interface Tab {
  key: TrainingStatus | 'all';
  labelKey: string;
}

@Component({
  selector: 'app-training',
  imports: [DatePipe, GmsIcon, GmsPageHeader, TranslocoPipe],
  providers: [provideTranslocoScope('training')],
  templateUrl: './training.html',
  styleUrl: './training.scss'
})
export class TrainingPage implements OnInit {
  private readonly trainingService = inject(TrainingService);
  protected readonly meta = TRAINING_STATUS_META;

  private readonly all = signal<Training[]>([]);
  protected readonly certificates = signal<Certificate[]>([]);
  protected readonly activeTab = signal<TrainingStatus | 'all'>('all');

  protected readonly tabs: Tab[] = [
    { key: 'all', labelKey: 'training.tab.all' },
    { key: 'assigned', labelKey: 'training.tab.assigned' },
    { key: 'completed', labelKey: 'training.tab.completed' },
    { key: 'upcoming', labelKey: 'training.tab.upcoming' },
    { key: 'expired', labelKey: 'training.tab.expired' }
  ];

  protected readonly filtered = computed(() => {
    const tab = this.activeTab();
    return tab === 'all' ? this.all() : this.all().filter((t) => t.status === tab);
  });

  count(key: TrainingStatus | 'all'): number {
    return key === 'all' ? this.all().length : this.all().filter((t) => t.status === key).length;
  }

  ngOnInit(): void {
    this.trainingService.getTrainings().subscribe((t) => this.all.set(t));
    this.trainingService.getCertificates().subscribe((c) => this.certificates.set(c));
  }
}

import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslocoPipe, provideTranslocoScope } from '@jsverse/transloco';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { AuthService } from '../../core/auth.service';
import {
  LeaveService,
  CalendarEntry,
  CalendarType,
  LeaveBalance,
  CALENDAR_TYPE_META
} from '../../core/leave.service';

@Component({
  selector: 'app-leave-calendar',
  imports: [DatePipe, GmsIcon, GmsPageHeader, TranslocoPipe],
  providers: [provideTranslocoScope('leave')],
  templateUrl: './leave-calendar.html',
  styleUrl: './leave-calendar.scss'
})
export class LeaveCalendar implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly leave = inject(LeaveService);
  protected readonly meta = CALENDAR_TYPE_META;

  // CalendarType, core/leave.service.ts içindeki CALENDAR_TYPE_META yerine
  // burada yerel anahtarlarla çevriliyor (servis dosyasına dokunulmuyor).
  protected readonly calendarTypeLabelKeys: Record<CalendarType, string> = {
    available: 'leave.calendarType.available',
    meeting: 'leave.calendarType.meeting',
    leave: 'leave.calendarType.leave',
    remote: 'leave.calendarType.remote',
    travel: 'leave.calendarType.travel'
  };

  protected readonly isManager = signal(false);
  protected readonly entries = signal<CalendarEntry[]>([]);
  protected readonly balances = signal<LeaveBalance[]>([]);

  // Rol bazlı açıklama (legend) — normal kullanıcı sınırlı kategori görür.
  protected readonly legend = computed<CalendarType[]>(() =>
    this.isManager()
      ? ['available', 'meeting', 'leave', 'remote', 'travel']
      : ['available', 'meeting', 'leave']
  );

  ngOnInit(): void {
    const manager = this.auth.isManager();
    this.isManager.set(manager);

    this.leave.getCalendar(manager).subscribe((e) => this.entries.set(e));

    if (manager) {
      this.leave.getBalances().subscribe((b) => this.balances.set(b));
    }
  }
}

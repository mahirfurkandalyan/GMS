import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import {
  EmployeeService,
  Employee,
  Availability,
  AVAILABILITY_META
} from '../../core/employee.service';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsFilterBar, QuickFilter } from '../../shared/ui/filter-bar/filter-bar';
import { GmsField, GmsInput } from '../../shared/ui/field/field';
import { LanguageService } from '../../core/language.service';

@Component({
  selector: 'app-employee-list',
  imports: [RouterLink, FormsModule, GmsIcon, GmsPageHeader, GmsFilterBar, GmsField, GmsInput, TranslocoPipe],
  providers: [provideTranslocoScope('employees')],
  templateUrl: './employee-list.html',
  styleUrl: './employee-list.scss'
})
export class EmployeeList implements OnInit {
  private readonly employeeService = inject(EmployeeService);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);
  protected readonly meta = AVAILABILITY_META;

  // Availability durumu, core/employee.service.ts içindeki AVAILABILITY_META yerine
  // burada yerel anahtarlarla çevriliyor (servis dosyasına dokunulmuyor).
  protected readonly availabilityLabelKeys: Record<Availability, string> = {
    available: 'employees.availability.available',
    meeting: 'employees.availability.meeting',
    leave: 'employees.availability.leave',
    offline: 'employees.availability.offline'
  };

  private readonly all = signal<Employee[]>([]);
  protected readonly departments = signal<string[]>([]);
  protected readonly skills = signal<string[]>([]);
  protected readonly projects = signal<string[]>([]);

  protected readonly search = signal('');
  protected readonly availability = signal<string[]>([]);
  protected readonly department = signal('');
  protected readonly skill = signal('');
  protected readonly project = signal('');

  protected readonly availabilityFilters = computed<QuickFilter[]>(() => {
    this.language.current();
    return [
      { id: 'available', label: this.transloco.translate('employees.availability.available') },
      { id: 'meeting', label: this.transloco.translate('employees.availability.meeting') },
      { id: 'leave', label: this.transloco.translate('employees.availability.leave') },
      { id: 'offline', label: this.transloco.translate('employees.availability.offline') }
    ];
  });

  protected readonly filtered = computed(() => {
    const q = this.search().trim().toLocaleLowerCase('tr');
    const avail = this.availability();
    const dep = this.department();
    const sk = this.skill();
    const pr = this.project();

    return this.all().filter((e) => {
      const matchesText =
        !q ||
        e.fullName.toLocaleLowerCase('tr').includes(q) ||
        e.title.toLocaleLowerCase('tr').includes(q);
      const matchesAvail = avail.length === 0 || avail.includes(e.availability);
      const matchesDep = !dep || e.department === dep;
      const matchesSkill = !sk || e.skills.some((s) => s.name === sk);
      const matchesProject = !pr || e.currentProject === pr;
      return matchesText && matchesAvail && matchesDep && matchesSkill && matchesProject;
    });
  });

  ngOnInit(): void {
    this.employeeService.getEmployees().subscribe((list) => this.all.set(list));
    this.departments.set(this.employeeService.getDepartments());
    this.skills.set(this.employeeService.getSkills());
    this.projects.set(this.employeeService.getProjects());
  }

  initials(name: string): string {
    return name
      .split(' ')
      .map((p) => p.charAt(0))
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }

  clearFilters(): void {
    this.search.set('');
    this.availability.set([]);
    this.department.set('');
    this.skill.set('');
    this.project.set('');
  }
}

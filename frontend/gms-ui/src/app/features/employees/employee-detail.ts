import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoPipe, provideTranslocoScope } from '@jsverse/transloco';
import {
  EmployeeService,
  Employee,
  Availability,
  AVAILABILITY_META
} from '../../core/employee.service';

@Component({
  selector: 'app-employee-detail',
  imports: [RouterLink, TranslocoPipe],
  providers: [provideTranslocoScope('employees')],
  templateUrl: './employee-detail.html',
  styleUrl: './employee-detail.scss'
})
export class EmployeeDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly employeeService = inject(EmployeeService);
  protected readonly meta = AVAILABILITY_META;

  // Availability durumu, core/employee.service.ts içindeki AVAILABILITY_META yerine
  // burada yerel anahtarlarla çevriliyor (servis dosyasına dokunulmuyor).
  protected readonly availabilityLabelKeys: Record<Availability, string> = {
    available: 'employees.availability.available',
    meeting: 'employees.availability.meeting',
    leave: 'employees.availability.leave',
    offline: 'employees.availability.offline'
  };

  protected readonly employee = signal<Employee | undefined>(undefined);
  protected readonly loaded = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.employeeService.getEmployee(id).subscribe((emp) => {
      this.employee.set(emp);
      this.loaded.set(true);
    });
  }

  initials(name: string): string {
    return name
      .split(' ')
      .map((p) => p.charAt(0))
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }

  stars(level: number): boolean[] {
    return [1, 2, 3, 4, 5].map((n) => n <= level);
  }
}

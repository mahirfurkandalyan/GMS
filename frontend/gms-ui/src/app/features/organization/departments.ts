import { Component, OnInit, inject, signal } from '@angular/core';
import { TranslocoPipe, provideTranslocoScope } from '@jsverse/transloco';
import { OrganizationService, Department } from '../../core/organization.service';
import { GmsIcon } from '../../shared/icon/icon';

@Component({
  selector: 'app-departments',
  imports: [GmsIcon, TranslocoPipe],
  providers: [provideTranslocoScope('organization')],
  templateUrl: './departments.html',
  styleUrl: './organization.scss'
})
export class Departments implements OnInit {
  private readonly org = inject(OrganizationService);
  protected readonly departments = signal<Department[]>([]);

  ngOnInit(): void {
    this.org.getDepartments().subscribe((d) => this.departments.set(d));
  }
}

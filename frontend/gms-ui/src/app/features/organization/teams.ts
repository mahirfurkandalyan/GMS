import { Component, OnInit, inject, signal } from '@angular/core';
import { TranslocoPipe, provideTranslocoScope } from '@jsverse/transloco';
import { OrganizationService, Team } from '../../core/organization.service';
import { GmsIcon } from '../../shared/icon/icon';

@Component({
  selector: 'app-teams',
  imports: [GmsIcon, TranslocoPipe],
  providers: [provideTranslocoScope('organization')],
  templateUrl: './teams.html',
  styleUrl: './organization.scss'
})
export class Teams implements OnInit {
  private readonly org = inject(OrganizationService);
  protected readonly teams = signal<Team[]>([]);

  ngOnInit(): void {
    this.org.getTeams().subscribe((t) => this.teams.set(t));
  }
}

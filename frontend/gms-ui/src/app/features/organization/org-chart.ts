import { Component } from '@angular/core';
import { TranslocoPipe, provideTranslocoScope } from '@jsverse/transloco';

@Component({
  selector: 'app-org-chart',
  imports: [TranslocoPipe],
  providers: [provideTranslocoScope('organization')],
  templateUrl: './org-chart.html',
  styleUrl: './organization.scss'
})
export class OrgChart {}

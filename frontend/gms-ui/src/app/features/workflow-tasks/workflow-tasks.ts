import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { TranslocoPipe, provideTranslocoScope } from '@jsverse/transloco';
import { WorkflowInstanceApiService } from '../../core/workflow/workflow-instance-api.service';
import { WorkflowTaskItem } from '../../core/workflow/workflow.models';
import { workflowStepTypeLabelKey } from '../../core/workflow/workflow-labels';
import { ApiError } from '../../core/api-error';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsState } from '../../shared/ui/state/state';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';

/**
 * "İş Akışı Görevleri" (My Tasks) — the primary action experience. Lists approval/manual tasks
 * assigned to the current user or their roles (GET /api/workflow-instances/tasks/mine). Visibility is
 * enforced server-side (never filtered client-side as a security measure). A task opens its workflow
 * instance, where the user completes or rejects it.
 */
@Component({
  selector: 'app-workflow-tasks',
  imports: [FormsModule, DatePipe, TranslocoPipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsState, GmsContextSection],
  providers: [provideTranslocoScope('workflows')],
  templateUrl: './workflow-tasks.html',
  styleUrl: './workflow-tasks.scss'
})
export class WorkflowTasks implements OnInit {
  private readonly api = inject(WorkflowInstanceApiService);
  private readonly router = inject(Router);

  protected readonly stepTypeKey = workflowStepTypeLabelKey;

  protected readonly tasks = signal<WorkflowTaskItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<ApiError | null>(null);

  protected readonly search = signal('');
  protected readonly onlyOverdue = signal(false);

  protected readonly filtered = computed(() => {
    const q = this.search().trim().toLocaleLowerCase('tr');
    const overdue = this.onlyOverdue();
    return this.tasks().filter((t) => {
      if (overdue && !t.isOverdue) return false;
      if (!q) return true;
      return (t.instanceNo + ' ' + (t.triggerObjectNumber ?? '') + ' ' + t.stepName + ' ' + t.workflowName)
        .toLocaleLowerCase('tr').includes(q);
    });
  });

  protected readonly isEmpty = computed(() => !this.loading() && !this.error() && this.tasks().length === 0);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.myTasks().subscribe({
      next: (t) => { this.tasks.set(t); this.loading.set(false); },
      error: (e: ApiError) => { this.error.set(e); this.loading.set(false); }
    });
  }

  open(task: WorkflowTaskItem): void {
    this.router.navigate(['/workflow-instances', task.instanceId]);
  }

  openChange(task: WorkflowTaskItem, event: Event): void {
    event.stopPropagation();
    if (task.triggerObjectType === 'ChangeRequest') {
      this.router.navigate(['/changes', task.triggerObjectId]);
    }
  }
}

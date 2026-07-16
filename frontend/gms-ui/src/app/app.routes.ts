import { inject } from '@angular/core';
import { Routes } from '@angular/router';
import { authGuard, permissionGuard } from './core/auth/guards';
import { AuthStateService } from './core/auth/auth-state.service';

export const routes: Routes = [
  {
    // Root: after startup session restoration, route to Hub or Login.
    path: '',
    pathMatch: 'full',
    redirectTo: () => (inject(AuthStateService).isAuthenticated() ? '/hub' : '/login')
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login').then((m) => m.Login),
    title: 'Giriş — GMS'
  },
  {
    path: 'forbidden',
    canActivate: [authGuard],
    loadComponent: () => import('./features/errors/forbidden').then((m) => m.Forbidden),
    title: 'Erişim Reddedildi — GMS'
  },
  {
    path: 'hub',
    canActivate: [authGuard],
    loadComponent: () => import('./features/hub/hub').then((m) => m.Hub),
    title: 'GMS Hub'
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () => import('./features/profile/profile').then((m) => m.Profile),
    title: 'Profilim — GMS'
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
    title: 'Gösterge Paneli — GMS'
  },
  {
    path: 'releases',
    canActivate: [permissionGuard],
    data: { permission: 'release.read' },
    loadComponent: () => import('./features/releases/release-list').then((m) => m.ReleaseList),
    title: 'Yayın Yönetimi — GMS'
  },
  {
    path: 'releases/new',
    canActivate: [permissionGuard],
    data: { permission: 'release.create' },
    loadComponent: () => import('./features/releases/release-wizard').then((m) => m.ReleaseWizard),
    title: 'Yeni Yayın Planı — GMS'
  },
  {
    path: 'releases/:id',
    canActivate: [permissionGuard],
    data: { permission: 'release.read' },
    loadComponent: () => import('./features/releases/release-detail').then((m) => m.ReleaseDetail),
    title: 'Yayın Detayı — GMS'
  },
  {
    path: 'changes',
    canActivate: [permissionGuard],
    data: { permission: 'change.read' },
    loadComponent: () => import('./features/changes/change-list').then((m) => m.ChangeList),
    title: 'Değişiklik Yönetimi — GMS'
  },
  {
    path: 'changes/new',
    canActivate: [permissionGuard],
    data: { permission: 'change.create' },
    loadComponent: () => import('./features/changes/change-wizard').then((m) => m.ChangeWizard),
    title: 'Yeni Değişiklik — GMS'
  },
  {
    path: 'changes/:id',
    canActivate: [permissionGuard],
    data: { permission: 'change.read' },
    loadComponent: () => import('./features/changes/change-detail').then((m) => m.ChangeDetail),
    title: 'Değişiklik Detayı — GMS'
  },
  {
    path: 'approvals',
    canActivate: [permissionGuard],
    data: { permission: 'approval.read' },
    loadComponent: () => import('./features/approvals/approval-list').then((m) => m.ApprovalList),
    title: 'Onay Merkezi — GMS'
  },
  {
    path: 'approvals/:id',
    canActivate: [permissionGuard],
    data: { permission: 'approval.read' },
    loadComponent: () => import('./features/approvals/approval-detail').then((m) => m.ApprovalDetail),
    title: 'Onay Detayı — GMS'
  },
  {
    path: 'validation',
    canActivate: [permissionGuard],
    data: { permission: 'validation.read' },
    loadComponent: () => import('./features/validation/validation-list').then((m) => m.ValidationList),
    title: 'Doğrulama Merkezi — GMS'
  },
  {
    path: 'validation/:id',
    canActivate: [permissionGuard],
    data: { permission: 'validation.read' },
    loadComponent: () => import('./features/validation/validation-detail').then((m) => m.ValidationDetail),
    title: 'Doğrulama Detayı — GMS'
  },
  {
    path: 'executions',
    canActivate: [permissionGuard],
    data: { permission: 'execution.read' },
    loadComponent: () => import('./features/executions/execution-list').then((m) => m.ExecutionList),
    title: 'Yürütme Merkezi — GMS'
  },
  {
    path: 'executions/:id',
    canActivate: [permissionGuard],
    data: { permission: 'execution.read' },
    loadComponent: () => import('./features/executions/execution-detail').then((m) => m.ExecutionDetail),
    title: 'Yürütme Detayı — GMS'
  },
  {
    path: 'documents',
    canActivate: [permissionGuard],
    data: { permission: 'document.read' },
    loadComponent: () => import('./features/documents/document-list').then((m) => m.DocumentList),
    title: 'Doküman Merkezi — GMS'
  },
  {
    path: 'documents/:id',
    canActivate: [permissionGuard],
    data: { permission: 'document.read' },
    loadComponent: () => import('./features/documents/document-detail').then((m) => m.DocumentDetail),
    title: 'Doküman Detayı — GMS'
  },
  {
    path: 'assets',
    canActivate: [authGuard],
    loadComponent: () => import('./features/assets/asset-list').then((m) => m.AssetList),
    title: 'Varlık Merkezi — GMS'
  },
  {
    path: 'assets/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/assets/asset-detail').then((m) => m.AssetDetail),
    title: 'Varlık Detayı — GMS'
  },
  {
    path: 'administration',
    canActivate: [permissionGuard],
    data: { permissions: ['admin.users.read', 'admin.roles.read'], permissionMode: 'any' },
    loadComponent: () => import('./features/administration/administration').then((m) => m.Administration),
    title: 'Administration — GMS'
  },
  {
    path: 'reports',
    canActivate: [permissionGuard],
    data: { permission: 'report.read' },
    loadComponent: () => import('./features/reports/reports').then((m) => m.Reports),
    title: 'Reports & Analytics — GMS'
  },
  {
    path: 'audit',
    canActivate: [permissionGuard],
    data: { permission: 'audit.read' },
    loadComponent: () => import('./features/audit/audit-list').then((m) => m.AuditList),
    title: 'Audit Center — GMS'
  },
  {
    path: 'audit/:id',
    canActivate: [permissionGuard],
    data: { permission: 'audit.read' },
    loadComponent: () => import('./features/audit/audit-detail').then((m) => m.AuditDetail),
    title: 'Denetim Kaydı — GMS'
  },
  {
    path: 'tasks',
    canActivate: [permissionGuard],
    data: { permission: 'workflow.task.read' },
    loadComponent: () => import('./features/workflow-tasks/workflow-tasks').then((m) => m.WorkflowTasks),
    title: 'İş Akışı Görevleri — GMS'
  },
  {
    path: 'workflow-instances/:id',
    canActivate: [permissionGuard],
    data: { permission: 'workflow.instance.read' },
    loadComponent: () => import('./features/workflow-tasks/workflow-instance-detail').then((m) => m.WorkflowInstanceDetailPage),
    title: 'İş Akışı Örneği — GMS'
  },
  {
    path: 'workflows',
    canActivate: [permissionGuard],
    data: { permission: 'workflow.definition.read' },
    loadComponent: () => import('./features/workflows/workflow-list').then((m) => m.WorkflowList),
    title: 'Workflow Designer — GMS'
  },
  {
    path: 'workflows/:id',
    canActivate: [permissionGuard],
    data: { permission: 'workflow.definition.read' },
    loadComponent: () => import('./features/workflows/workflow-detail').then((m) => m.WorkflowDetail),
    title: 'İş Akışı Tasarımcısı — GMS'
  },
  {
    path: 'employees',
    canActivate: [authGuard],
    loadComponent: () => import('./features/employees/employee-list').then((m) => m.EmployeeList),
    title: 'Çalışanlar — GMS'
  },
  {
    path: 'employees/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/employees/employee-detail').then((m) => m.EmployeeDetail),
    title: 'Çalışan Profili — GMS'
  },
  {
    path: 'organization/departments',
    canActivate: [authGuard],
    loadComponent: () => import('./features/organization/departments').then((m) => m.Departments),
    title: 'Departmanlar — GMS'
  },
  {
    path: 'organization/teams',
    canActivate: [authGuard],
    loadComponent: () => import('./features/organization/teams').then((m) => m.Teams),
    title: 'Takımlar — GMS'
  },
  {
    path: 'organization/chart',
    canActivate: [authGuard],
    loadComponent: () => import('./features/organization/org-chart').then((m) => m.OrgChart),
    title: 'Organizasyon Şeması — GMS'
  },
  {
    path: 'leave',
    canActivate: [authGuard],
    loadComponent: () => import('./features/leave/leave-calendar').then((m) => m.LeaveCalendar),
    title: 'İzin Takvimi — GMS'
  },
  {
    path: 'training',
    canActivate: [authGuard],
    loadComponent: () => import('./features/training/training').then((m) => m.TrainingPage),
    title: 'Eğitimler — GMS'
  },
  {
    path: 'notifications',
    canActivate: [permissionGuard],
    data: { permission: 'notification.read' },
    loadComponent: () => import('./features/notifications/notifications').then((m) => m.Notifications),
    title: 'Notification Center — GMS'
  },
  {
    path: 'admin/notification-rules',
    canActivate: [permissionGuard],
    data: { permission: 'notification.template.manage' },
    loadComponent: () => import('./features/notification-rules/notification-rules').then((m) => m.NotificationRules),
    title: 'Bildirim Kuralları — GMS'
  },
  { path: '**', redirectTo: 'hub' }
];

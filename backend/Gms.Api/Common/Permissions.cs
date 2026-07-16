namespace Gms.Api.Common;

/// <summary>A seedable permission definition (metadata for the catalog).</summary>
public sealed record PermissionDef(string Code, string Name, string Module, string Description);

/// <summary>Permission modules.</summary>
public static class PermissionModules
{
    public const string Change = "CHANGE";
    public const string Approval = "APPROVAL";
    public const string Release = "RELEASE";
    public const string Execution = "EXECUTION";
    public const string Validation = "VALIDATION";
    public const string Audit = "AUDIT";
    public const string Administration = "ADMINISTRATION";
    public const string Document = "DOCUMENT";
    public const string Notification = "NOTIFICATION";
    public const string Report = "REPORT";
    public const string Workflow = "WORKFLOW";
    public const string Integration = "INTEGRATION";
    public const string Operations = "OPERATIONS";
}

/// <summary>
/// Central catalog of permission codes ("module.action"). Each constant doubles as the
/// authorization policy name. This is the single source of truth for the permission set
/// and the seed data.
/// </summary>
public static class Permissions
{
    // CHANGE
    public const string ChangeRead = "change.read";
    public const string ChangeCreate = "change.create";
    public const string ChangeUpdate = "change.update";
    public const string ChangeSubmit = "change.submit";
    public const string ChangeCancel = "change.cancel";
    public const string ChangeRevisionCreate = "change.revision.create";

    // APPROVAL
    public const string ApprovalRead = "approval.read";
    public const string ApprovalApproveArchitect = "approval.approve.architect";
    public const string ApprovalApproveQa = "approval.approve.qa";
    public const string ApprovalApproveReleaseManager = "approval.approve.release-manager";
    public const string ApprovalApproveAdmin = "approval.approve.admin";
    public const string ApprovalReject = "approval.reject";
    public const string ApprovalRequestRevision = "approval.request-revision";

    // RELEASE
    public const string ReleaseRead = "release.read";
    public const string ReleaseCreate = "release.create";
    public const string ReleaseUpdate = "release.update";
    public const string ReleaseSchedule = "release.schedule";
    public const string ReleaseComplete = "release.complete";
    public const string ReleaseCancel = "release.cancel";

    // EXECUTION
    public const string ExecutionRead = "execution.read";
    public const string ExecutionCreate = "execution.create";
    public const string ExecutionStart = "execution.start";
    public const string ExecutionStepStart = "execution.step.start";
    public const string ExecutionStepComplete = "execution.step.complete";
    public const string ExecutionStepFail = "execution.step.fail";
    public const string ExecutionRollback = "execution.rollback";

    // VALIDATION
    public const string ValidationRead = "validation.read";
    public const string ValidationCreate = "validation.create";
    public const string ValidationStart = "validation.start";
    public const string ValidationCheckExecute = "validation.check.execute";

    // AUDIT
    public const string AuditRead = "audit.read";
    public const string AuditSecurityRead = "audit.security.read";
    public const string AuditExport = "audit.export";

    // REPORT
    public const string ReportRead = "report.read";
    public const string ReportExport = "report.export";
    public const string ReportManage = "report.manage";

    // WORKFLOW — definition
    public const string WorkflowDefinitionRead = "workflow.definition.read";
    public const string WorkflowDefinitionCreate = "workflow.definition.create";
    public const string WorkflowDefinitionUpdate = "workflow.definition.update";
    public const string WorkflowDefinitionPublish = "workflow.definition.publish";
    public const string WorkflowDefinitionActivate = "workflow.definition.activate";
    public const string WorkflowDefinitionArchive = "workflow.definition.archive";
    // WORKFLOW — runtime
    public const string WorkflowInstanceRead = "workflow.instance.read";
    public const string WorkflowInstanceStart = "workflow.instance.start";
    public const string WorkflowInstanceCancel = "workflow.instance.cancel";
    public const string WorkflowInstancePause = "workflow.instance.pause";
    public const string WorkflowInstanceResume = "workflow.instance.resume";
    public const string WorkflowTaskRead = "workflow.task.read";
    public const string WorkflowTaskComplete = "workflow.task.complete";
    public const string WorkflowTaskReject = "workflow.task.reject";
    public const string WorkflowAdminOverride = "workflow.admin.override";

    // ADMINISTRATION
    public const string AdminUsersRead = "admin.users.read";
    public const string AdminUsersManage = "admin.users.manage";
    public const string AdminRolesRead = "admin.roles.read";
    public const string AdminRolesManage = "admin.roles.manage";

    // DOCUMENT (shared document infrastructure)
    public const string DocumentRead = "document.read";
    public const string DocumentCreate = "document.create";
    public const string DocumentUpload = "document.upload";
    public const string DocumentDownload = "document.download";
    public const string DocumentVersionCreate = "document.version.create";
    public const string DocumentUpdate = "document.update";
    public const string DocumentArchive = "document.archive";
    public const string DocumentDelete = "document.delete";
    public const string DocumentLink = "document.link";
    public const string DocumentUnlink = "document.unlink";
    public const string DocumentAuditRead = "document.audit.read";

    // NOTIFICATION (central notification engine)
    public const string NotificationRead = "notification.read";
    public const string NotificationManage = "notification.manage";
    public const string NotificationPreference = "notification.preference";
    public const string NotificationArchive = "notification.archive";
    public const string NotificationBroadcast = "notification.broadcast";
    public const string NotificationTemplateManage = "notification.template.manage";

    // INTEGRATION
    public const string IntegrationRead = "integration.read";
    public const string IntegrationCreate = "integration.create";
    public const string IntegrationUpdate = "integration.update";
    public const string IntegrationActivate = "integration.activate";
    public const string IntegrationCredentialManage = "integration.credential.manage";
    public const string IntegrationEndpointManage = "integration.endpoint.manage";
    public const string IntegrationSubscriptionManage = "integration.subscription.manage";
    public const string IntegrationExecute = "integration.execute";
    public const string IntegrationRetry = "integration.retry";
    public const string IntegrationCancel = "integration.cancel";
    public const string IntegrationAuditRead = "integration.audit.read";
    public const string IntegrationLinkManage = "integration.link.manage";

    // OPERATIONS
    public const string OperationsRead = "operations.read";
    public const string OperationsManage = "operations.manage";

    /// <summary>Every permission code (used for policy registration and Admin grant).</summary>
    public static readonly IReadOnlyList<PermissionDef> Catalog = new[]
    {
        new PermissionDef(ChangeRead, "Değişiklik görüntüleme", PermissionModules.Change, "Değişiklikleri listeleme ve görüntüleme."),
        new PermissionDef(ChangeCreate, "Değişiklik oluşturma", PermissionModules.Change, "Yeni değişiklik oluşturma."),
        new PermissionDef(ChangeUpdate, "Değişiklik güncelleme", PermissionModules.Change, "Değişiklik alanlarını güncelleme."),
        new PermissionDef(ChangeSubmit, "Değişiklik gönderme", PermissionModules.Change, "Değişikliği incelemeye gönderme."),
        new PermissionDef(ChangeCancel, "Değişiklik iptali", PermissionModules.Change, "Değişikliği iptal etme."),
        new PermissionDef(ChangeRevisionCreate, "Revizyon oluşturma", PermissionModules.Change, "Değişiklik revizyonu ekleme."),

        new PermissionDef(ApprovalRead, "Onay görüntüleme", PermissionModules.Approval, "Onay taleplerini görüntüleme."),
        new PermissionDef(ApprovalApproveArchitect, "Mimari onayı", PermissionModules.Approval, "Mimari onay adımını onaylama."),
        new PermissionDef(ApprovalApproveQa, "QA onayı", PermissionModules.Approval, "QA onay adımını onaylama."),
        new PermissionDef(ApprovalApproveReleaseManager, "Yayın yöneticisi onayı", PermissionModules.Approval, "Yayın yöneticisi onay adımını onaylama."),
        new PermissionDef(ApprovalApproveAdmin, "Admin onayı", PermissionModules.Approval, "Admin onay adımını onaylama."),
        new PermissionDef(ApprovalReject, "Onay reddi", PermissionModules.Approval, "Onay adımını reddetme."),
        new PermissionDef(ApprovalRequestRevision, "Revizyon talebi", PermissionModules.Approval, "Onayda revizyon talep etme."),

        new PermissionDef(ReleaseRead, "Yayın görüntüleme", PermissionModules.Release, "Yayın planlarını görüntüleme."),
        new PermissionDef(ReleaseCreate, "Yayın oluşturma", PermissionModules.Release, "Yeni yayın planı oluşturma."),
        new PermissionDef(ReleaseUpdate, "Yayın güncelleme", PermissionModules.Release, "Yayın planını güncelleme."),
        new PermissionDef(ReleaseSchedule, "Yayın zamanlama", PermissionModules.Release, "Yayını zamanlama."),
        new PermissionDef(ReleaseComplete, "Yayın tamamlama", PermissionModules.Release, "Yayını manuel tamamlama."),
        new PermissionDef(ReleaseCancel, "Yayın iptali", PermissionModules.Release, "Yayını iptal etme."),

        new PermissionDef(ExecutionRead, "Yürütme görüntüleme", PermissionModules.Execution, "Yürütmeleri görüntüleme."),
        new PermissionDef(ExecutionCreate, "Yürütme oluşturma", PermissionModules.Execution, "Yürütme oluşturma."),
        new PermissionDef(ExecutionStart, "Yürütme başlatma", PermissionModules.Execution, "Yürütmeyi başlatma."),
        new PermissionDef(ExecutionStepStart, "Adım başlatma", PermissionModules.Execution, "Yürütme adımını başlatma."),
        new PermissionDef(ExecutionStepComplete, "Adım tamamlama", PermissionModules.Execution, "Yürütme adımını tamamlama."),
        new PermissionDef(ExecutionStepFail, "Adım başarısız", PermissionModules.Execution, "Yürütme adımını başarısız işaretleme."),
        new PermissionDef(ExecutionRollback, "Yürütme geri alma", PermissionModules.Execution, "Yürütmeyi geri alma."),

        new PermissionDef(ValidationRead, "Doğrulama görüntüleme", PermissionModules.Validation, "Doğrulamaları görüntüleme."),
        new PermissionDef(ValidationCreate, "Doğrulama oluşturma", PermissionModules.Validation, "Doğrulama oluşturma."),
        new PermissionDef(ValidationStart, "Doğrulama başlatma", PermissionModules.Validation, "Doğrulamayı başlatma."),
        new PermissionDef(ValidationCheckExecute, "Kontrol yürütme", PermissionModules.Validation, "Doğrulama kontrolünü yürütme (pass/fail)."),

        new PermissionDef(AuditRead, "Denetim görüntüleme", PermissionModules.Audit, "Birleşik denetim kayıtlarını görüntüleme."),
        new PermissionDef(AuditSecurityRead, "Güvenlik denetimi", PermissionModules.Audit, "Güvenlik denetim kayıtlarını görüntüleme."),
        new PermissionDef(AuditExport, "Denetim dışa aktarma", PermissionModules.Audit, "Denetim kayıtlarını CSV dışa aktarma."),
        new PermissionDef(ReportRead, "Rapor görüntüleme", PermissionModules.Report, "Raporları ve metrikleri görüntüleme."),
        new PermissionDef(ReportExport, "Rapor dışa aktarma", PermissionModules.Report, "Rapor veri kümelerini CSV dışa aktarma."),
        new PermissionDef(ReportManage, "Rapor yönetimi", PermissionModules.Report, "Rapor tanımlarını yönetme."),

        new PermissionDef(AdminUsersRead, "Kullanıcı görüntüleme", PermissionModules.Administration, "Kullanıcıları görüntüleme."),
        new PermissionDef(AdminUsersManage, "Kullanıcı yönetimi", PermissionModules.Administration, "Kullanıcı oluşturma/güncelleme/rol atama."),
        new PermissionDef(AdminRolesRead, "Rol görüntüleme", PermissionModules.Administration, "Rolleri görüntüleme."),
        new PermissionDef(AdminRolesManage, "Rol yönetimi", PermissionModules.Administration, "Rol/izin yönetimi."),

        new PermissionDef(DocumentRead, "Doküman görüntüleme", PermissionModules.Document, "Dokümanları listeleme ve görüntüleme."),
        new PermissionDef(DocumentCreate, "Doküman oluşturma", PermissionModules.Document, "Yeni doküman (metadata) oluşturma."),
        new PermissionDef(DocumentUpload, "Doküman yükleme", PermissionModules.Document, "Dokümana ilk dosyayı yükleme."),
        new PermissionDef(DocumentDownload, "Doküman indirme", PermissionModules.Document, "Doküman sürümlerini indirme."),
        new PermissionDef(DocumentVersionCreate, "Yeni sürüm", PermissionModules.Document, "Dokümana yeni sürüm ekleme."),
        new PermissionDef(DocumentUpdate, "Doküman güncelleme", PermissionModules.Document, "Doküman metadata güncelleme."),
        new PermissionDef(DocumentArchive, "Doküman arşivleme", PermissionModules.Document, "Dokümanı arşivleme."),
        new PermissionDef(DocumentDelete, "Doküman silme", PermissionModules.Document, "Dokümanı yumuşak silme (soft delete)."),
        new PermissionDef(DocumentLink, "Doküman bağlama", PermissionModules.Document, "Dokümanı bir iş nesnesine bağlama."),
        new PermissionDef(DocumentUnlink, "Doküman bağını kaldırma", PermissionModules.Document, "Doküman bağını kaldırma."),
        new PermissionDef(DocumentAuditRead, "Doküman denetimi", PermissionModules.Document, "Doküman denetim ve indirme geçmişini görüntüleme."),

        new PermissionDef(NotificationRead, "Bildirim görüntüleme", PermissionModules.Notification, "Kendi bildirimlerini görüntüleme ve okundu işaretleme."),
        new PermissionDef(NotificationManage, "Bildirim yönetimi", PermissionModules.Notification, "Bildirimleri yönetme (yönetici)."),
        new PermissionDef(NotificationPreference, "Bildirim tercihleri", PermissionModules.Notification, "Kendi bildirim tercihlerini yönetme."),
        new PermissionDef(NotificationArchive, "Bildirim arşivleme", PermissionModules.Notification, "Kendi bildirimlerini arşivleme."),
        new PermissionDef(NotificationBroadcast, "Toplu bildirim", PermissionModules.Notification, "Tüm kullanıcılara/role toplu bildirim gönderme."),
        new PermissionDef(NotificationTemplateManage, "Şablon yönetimi", PermissionModules.Notification, "Bildirim şablonlarını görüntüleme/güncelleme."),

        new PermissionDef(WorkflowDefinitionRead, "Workflow tanımı görüntüleme", PermissionModules.Workflow, "Workflow tanımlarını/versiyonlarını görüntüleme."),
        new PermissionDef(WorkflowDefinitionCreate, "Workflow tanımı oluşturma", PermissionModules.Workflow, "Workflow tanımı/taslak versiyon oluşturma."),
        new PermissionDef(WorkflowDefinitionUpdate, "Workflow tanımı güncelleme", PermissionModules.Workflow, "Taslak versiyon adım/geçişlerini düzenleme."),
        new PermissionDef(WorkflowDefinitionPublish, "Workflow yayınlama", PermissionModules.Workflow, "Bir versiyonu doğrulayıp yayınlama (immutable)."),
        new PermissionDef(WorkflowDefinitionActivate, "Workflow aktifleştirme", PermissionModules.Workflow, "Yayınlanmış versiyonu aktif yapma."),
        new PermissionDef(WorkflowDefinitionArchive, "Workflow arşivleme", PermissionModules.Workflow, "Workflow tanımını arşivleme."),
        new PermissionDef(WorkflowInstanceRead, "Workflow örneği görüntüleme", PermissionModules.Workflow, "Workflow örneklerini görüntüleme."),
        new PermissionDef(WorkflowInstanceStart, "Workflow başlatma", PermissionModules.Workflow, "Workflow örneği başlatma."),
        new PermissionDef(WorkflowInstanceCancel, "Workflow iptali", PermissionModules.Workflow, "Workflow örneğini iptal etme."),
        new PermissionDef(WorkflowInstancePause, "Workflow duraklatma", PermissionModules.Workflow, "Workflow örneğini duraklatma."),
        new PermissionDef(WorkflowInstanceResume, "Workflow devam", PermissionModules.Workflow, "Duraklatılmış workflow'u sürdürme."),
        new PermissionDef(WorkflowTaskRead, "Workflow görevi görüntüleme", PermissionModules.Workflow, "Atanan görevleri görüntüleme."),
        new PermissionDef(WorkflowTaskComplete, "Workflow görevi tamamlama", PermissionModules.Workflow, "Atanan onay/görev adımını tamamlama."),
        new PermissionDef(WorkflowTaskReject, "Workflow görevi reddi", PermissionModules.Workflow, "Atanan onay adımını reddetme."),
        new PermissionDef(WorkflowAdminOverride, "Workflow yönetici override", PermissionModules.Workflow, "Atama dışı yönetici müdahalesi (denetlenir)."),

        new PermissionDef(IntegrationRead, "Entegrasyon görüntüleme", PermissionModules.Integration, "Entegrasyon tanımlarını ve yürütmelerini görüntüleme."),
        new PermissionDef(IntegrationCreate, "Entegrasyon oluşturma", PermissionModules.Integration, "Yeni entegrasyon tanımı oluşturma."),
        new PermissionDef(IntegrationUpdate, "Entegrasyon güncelleme", PermissionModules.Integration, "Entegrasyon tanımı/uç nokta/abonelik güncelleme."),
        new PermissionDef(IntegrationActivate, "Entegrasyon aktifleştirme", PermissionModules.Integration, "Entegrasyonu aktif/pasif yapma ve bağlantı testi."),
        new PermissionDef(IntegrationCredentialManage, "Kimlik bilgisi yönetimi", PermissionModules.Integration, "Şifreli kimlik bilgisi ekleme/döndürme/silme (hassas)."),
        new PermissionDef(IntegrationEndpointManage, "Uç nokta yönetimi", PermissionModules.Integration, "Entegrasyon uç noktalarını yönetme."),
        new PermissionDef(IntegrationSubscriptionManage, "Abonelik yönetimi", PermissionModules.Integration, "Olay aboneliklerini yönetme."),
        new PermissionDef(IntegrationExecute, "Entegrasyon yürütme", PermissionModules.Integration, "Giden çağrı yürütme / bekleyenleri gönderme."),
        new PermissionDef(IntegrationRetry, "Yürütme yeniden deneme", PermissionModules.Integration, "Başarısız yürütmeyi yeniden deneme."),
        new PermissionDef(IntegrationCancel, "Yürütme iptali", PermissionModules.Integration, "Bekleyen/başarısız yürütmeyi iptal etme."),
        new PermissionDef(IntegrationAuditRead, "Entegrasyon denetimi", PermissionModules.Integration, "Entegrasyon denetim kayıtlarını görüntüleme."),
        new PermissionDef(IntegrationLinkManage, "Dış nesne bağı yönetimi", PermissionModules.Integration, "Dış nesne bağlarını oluşturma/kaldırma."),

        new PermissionDef(OperationsRead, "Operasyon durumu görüntüleme", PermissionModules.Operations, "Arka plan işleme birikimlerini ve worker durumunu görüntüleme."),
        new PermissionDef(OperationsManage, "Operasyon yönetimi", PermissionModules.Operations, "Kontrollü teşhis: worker'ı elle bir kez çalıştırma."),
    };

    public static IEnumerable<string> AllCodes => Catalog.Select(p => p.Code);
}

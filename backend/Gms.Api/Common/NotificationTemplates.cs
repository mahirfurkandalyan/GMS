namespace Gms.Api.Common;

/// <summary>A seedable notification template definition ({{placeholder}} tokens in subject/body).</summary>
public sealed record NotificationTemplateDef(string Code, string Name, string Module, string Subject, string Body);

/// <summary>
/// Central catalog of notification template codes — the single source of truth for the
/// template set and its seed data. Each code doubles as the notification Type.
/// </summary>
public static class NotificationTemplates
{
    // CHANGE
    public const string ChangeSubmitted = "ChangeSubmitted";
    // APPROVAL
    public const string ApprovalRequired = "ApprovalRequired";
    public const string ApprovalApproved = "ApprovalApproved";
    public const string ApprovalRejected = "ApprovalRejected";
    // RELEASE
    public const string ReleaseScheduled = "ReleaseScheduled";
    public const string ReleaseCompleted = "ReleaseCompleted";
    // EXECUTION
    public const string ExecutionStarted = "ExecutionStarted";
    public const string ExecutionCompleted = "ExecutionCompleted";
    public const string ExecutionFailed = "ExecutionFailed";
    // VALIDATION
    public const string ValidationPassed = "ValidationPassed";
    public const string ValidationFailed = "ValidationFailed";
    // DOCUMENT
    public const string DocumentUploaded = "DocumentUploaded";
    public const string DocumentVersionCreated = "DocumentVersionCreated";
    // SECURITY
    public const string SecurityLoginFailed = "SecurityLoginFailed";
    public const string SecurityLockedOut = "SecurityLockedOut";
    public const string PasswordChanged = "PasswordChanged";
    // WORKFLOW
    public const string WorkflowTaskAssigned = "WorkflowTaskAssigned";
    public const string WorkflowTaskDueSoon = "WorkflowTaskDueSoon";
    public const string WorkflowTaskOverdue = "WorkflowTaskOverdue";
    public const string WorkflowCompleted = "WorkflowCompleted";
    public const string WorkflowRejected = "WorkflowRejected";
    public const string WorkflowCancelled = "WorkflowCancelled";
    // INTEGRATION
    public const string IntegrationConnectionFailed = "IntegrationConnectionFailed";
    public const string IntegrationExecutionFailed = "IntegrationExecutionFailed";
    public const string IntegrationDeadLettered = "IntegrationDeadLettered";
    public const string IntegrationCredentialRotated = "IntegrationCredentialRotated";
    public const string IncomingWebhookRejected = "IncomingWebhookRejected";

    public static readonly IReadOnlyList<NotificationTemplateDef> Catalog = new[]
    {
        new NotificationTemplateDef(ChangeSubmitted, "Değişiklik gönderildi", NotificationModules.Change,
            "Yeni değişiklik incelemede: {{ChangeNo}}", "{{ChangeNo}} numaralı '{{Title}}' değişikliği incelemeye gönderildi."),
        new NotificationTemplateDef(ApprovalRequired, "Onay bekleniyor", NotificationModules.Approval,
            "Onayınız bekleniyor: {{ApprovalNo}}", "{{ApprovalNo}} onay talebinde '{{StepName}}' adımı onayınızı bekliyor."),
        new NotificationTemplateDef(ApprovalApproved, "Değişiklik onaylandı", NotificationModules.Approval,
            "Değişiklik onaylandı: {{ChangeNo}}", "{{ChangeNo}} numaralı değişiklik tüm onay adımlarından geçti ve onaylandı."),
        new NotificationTemplateDef(ApprovalRejected, "Onay reddedildi", NotificationModules.Approval,
            "Onay reddedildi: {{ChangeNo}}", "{{ChangeNo}} numaralı değişikliğin onayı reddedildi."),
        new NotificationTemplateDef(ReleaseScheduled, "Yayın zamanlandı", NotificationModules.Release,
            "Yayın zamanlandı: {{ReleaseNo}}", "{{ReleaseNo}} numaralı '{{Name}}' yayını zamanlandı."),
        new NotificationTemplateDef(ReleaseCompleted, "Yayın tamamlandı", NotificationModules.Release,
            "Yayın tamamlandı: {{ReleaseNo}}", "{{ReleaseNo}} numaralı yayın tamamlandı."),
        new NotificationTemplateDef(ExecutionStarted, "Yürütme başladı", NotificationModules.Execution,
            "Yürütme başladı: {{ExecutionNo}}", "{{ExecutionNo}} numaralı yürütme başlatıldı."),
        new NotificationTemplateDef(ExecutionCompleted, "Yürütme tamamlandı", NotificationModules.Execution,
            "Yürütme tamamlandı: {{ExecutionNo}}", "{{ExecutionNo}} numaralı yürütme başarıyla tamamlandı."),
        new NotificationTemplateDef(ExecutionFailed, "Yürütme başarısız", NotificationModules.Execution,
            "Yürütme başarısız: {{ExecutionNo}}", "{{ExecutionNo}} numaralı yürütme başarısız oldu; geri alma gerekebilir."),
        new NotificationTemplateDef(ValidationPassed, "Doğrulama geçti", NotificationModules.Validation,
            "Doğrulama geçti: {{ValidationNo}}", "{{ValidationNo}} numaralı doğrulama başarıyla tamamlandı; yayın kabul edildi."),
        new NotificationTemplateDef(ValidationFailed, "Doğrulama başarısız", NotificationModules.Validation,
            "Doğrulama başarısız: {{ValidationNo}}", "{{ValidationNo}} numaralı doğrulama başarısız oldu."),
        new NotificationTemplateDef(DocumentUploaded, "Doküman yüklendi", NotificationModules.Document,
            "Doküman yüklendi: {{DocumentNo}}", "{{DocumentNo}} dokümanına yeni bir dosya yüklendi."),
        new NotificationTemplateDef(DocumentVersionCreated, "Yeni doküman sürümü", NotificationModules.Document,
            "Yeni sürüm: {{DocumentNo}}", "{{DocumentNo}} dokümanına yeni sürüm ({{Version}}) eklendi."),
        new NotificationTemplateDef(SecurityLoginFailed, "Başarısız giriş denemesi", NotificationModules.Security,
            "Hesabınızda başarısız giriş", "Hesabınızda başarısız bir giriş denemesi yapıldı."),
        new NotificationTemplateDef(SecurityLockedOut, "Hesap kilitlendi", NotificationModules.Security,
            "Hesabınız kilitlendi", "Çok sayıda başarısız denemeden sonra hesabınız geçici olarak kilitlendi."),
        new NotificationTemplateDef(PasswordChanged, "Parola değiştirildi", NotificationModules.Security,
            "Parolanız değiştirildi", "Hesabınızın parolası değiştirildi. Bu işlemi siz yapmadıysanız yöneticinize başvurun."),
        new NotificationTemplateDef(WorkflowTaskAssigned, "Workflow görevi atandı", NotificationModules.Workflow,
            "Göreviniz bekliyor: {{StepName}}", "{{ChangeNo}} numaralı değişikliğin '{{StepName}}' adımı ({{WorkflowName}}) sizin aksiyonunuzu bekliyor."),
        new NotificationTemplateDef(WorkflowTaskDueSoon, "Workflow görevi süresi yaklaşıyor", NotificationModules.Workflow,
            "Görev süresi yaklaşıyor: {{StepName}}", "{{ChangeNo}} değişikliğinin '{{StepName}}' adımının son tarihi ({{DueAt}}) yaklaşıyor."),
        new NotificationTemplateDef(WorkflowTaskOverdue, "Workflow görevi gecikti", NotificationModules.Workflow,
            "Görev gecikti: {{StepName}}", "{{ChangeNo}} değişikliğinin '{{StepName}}' adımı son tarihini ({{DueAt}}) geçti."),
        new NotificationTemplateDef(WorkflowCompleted, "Workflow tamamlandı", NotificationModules.Workflow,
            "Akış tamamlandı: {{ChangeNo}}", "{{ChangeNo}} numaralı değişikliğin onay akışı ({{WorkflowName}}) tamamlandı ve değişiklik onaylandı."),
        new NotificationTemplateDef(WorkflowRejected, "Workflow reddedildi", NotificationModules.Workflow,
            "Akış reddedildi: {{ChangeNo}}", "{{ChangeNo}} numaralı değişikliğin onay akışı '{{StepName}}' adımında reddedildi."),
        new NotificationTemplateDef(WorkflowCancelled, "Workflow iptal edildi", NotificationModules.Workflow,
            "Akış iptal edildi: {{ChangeNo}}", "{{ChangeNo}} numaralı değişikliğin onay akışı ({{WorkflowName}}) iptal edildi."),
        new NotificationTemplateDef(IntegrationConnectionFailed, "Entegrasyon bağlantısı başarısız", NotificationModules.Integration,
            "Bağlantı başarısız: {{IntegrationNo}}", "{{IntegrationNo}} ({{IntegrationName}}) entegrasyonunun bağlantı testi başarısız oldu: {{Error}}"),
        new NotificationTemplateDef(IntegrationExecutionFailed, "Entegrasyon yürütmesi başarısız", NotificationModules.Integration,
            "Yürütme başarısız: {{ExecutionNo}}", "{{ExecutionNo}} yürütmesi ({{IntegrationName}}) başarısız oldu: {{Error}}"),
        new NotificationTemplateDef(IntegrationDeadLettered, "Entegrasyon ölü mektup kutusuna alındı", NotificationModules.Integration,
            "Ölü mektup: {{ExecutionNo}}", "{{ExecutionNo}} yürütmesi ({{IntegrationName}}) azami deneme sonrası ölü mektup kutusuna alındı."),
        new NotificationTemplateDef(IntegrationCredentialRotated, "Kimlik bilgisi döndürüldü", NotificationModules.Integration,
            "Kimlik bilgisi döndürüldü: {{IntegrationNo}}", "{{IntegrationName}} entegrasyonunun '{{KeyName}}' kimlik bilgisi döndürüldü."),
        new NotificationTemplateDef(IncomingWebhookRejected, "Gelen webhook reddedildi", NotificationModules.Integration,
            "Webhook reddedildi: {{IntegrationNo}}", "{{IntegrationName}} entegrasyonuna gelen bir webhook reddedildi: {{Reason}}"),
    };

    /// <summary>Renders a template body/subject by replacing {{key}} tokens.</summary>
    public static string Render(string template, IReadOnlyDictionary<string, string>? data)
    {
        if (data is null || data.Count == 0) return template;
        var result = template;
        foreach (var (key, value) in data)
            result = result.Replace("{{" + key + "}}", value);
        return result;
    }
}

using Gms.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Data;

/// <summary>
/// GMS (Kurumsal Yönetişim Yönetim Sistemi) veritabanı bağlamı.
/// PoC — kimlik/rol modeli ve mock seed verileri.
/// </summary>
public class GmsDbContext : DbContext
{
    public GmsDbContext(DbContextOptions<GmsDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    // Identity / RBAC
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    // İş modeli (Release Management temeli)
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<AppEnvironment> Environments => Set<AppEnvironment>();

    // Release Planning domaini (System of Record)
    public DbSet<ReleasePlan> ReleasePlans => Set<ReleasePlan>();
    public DbSet<ReleasePlanItem> ReleasePlanItems => Set<ReleasePlanItem>();
    public DbSet<ReleaseDeploymentPlan> ReleaseDeploymentPlans => Set<ReleaseDeploymentPlan>();
    public DbSet<ReleaseDocument> ReleaseDocuments => Set<ReleaseDocument>();
    public DbSet<ReleaseAuditEvent> ReleaseAuditEvents => Set<ReleaseAuditEvent>();

    // Change Management domaini
    public DbSet<ChangeRequest> ChangeRequests => Set<ChangeRequest>();
    public DbSet<ChangeRevision> ChangeRevisions => Set<ChangeRevision>();
    public DbSet<ChangeAffectedAsset> ChangeAffectedAssets => Set<ChangeAffectedAsset>();
    public DbSet<ChangeDocument> ChangeDocuments => Set<ChangeDocument>();
    public DbSet<ChangeAuditEvent> ChangeAuditEvents => Set<ChangeAuditEvent>();

    // Approval Management domaini
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<ApprovalDecision> ApprovalDecisions => Set<ApprovalDecision>();
    public DbSet<ApprovalAuditEvent> ApprovalAuditEvents => Set<ApprovalAuditEvent>();

    // Execution domaini (Release Planning üzerine kurulur)
    public DbSet<DeploymentRun> DeploymentRuns => Set<DeploymentRun>();
    public DbSet<DeploymentStep> DeploymentSteps => Set<DeploymentStep>();
    public DbSet<DeploymentEvent> DeploymentEvents => Set<DeploymentEvent>();

    // Validation domaini (Execution üzerine kurulur)
    public DbSet<ValidationRun> ValidationRuns => Set<ValidationRun>();
    public DbSet<ValidationCheck> ValidationChecks => Set<ValidationCheck>();
    public DbSet<ValidationEvidence> ValidationEvidences => Set<ValidationEvidence>();
    public DbSet<ValidationEvent> ValidationEvents => Set<ValidationEvent>();

    // Document Management domaini (tüm platformun tek doküman sistemi)
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<DocumentLink> DocumentLinks => Set<DocumentLink>();
    public DbSet<DocumentAuditEvent> DocumentAuditEvents => Set<DocumentAuditEvent>();
    public DbSet<DocumentDownload> DocumentDownloads => Set<DocumentDownload>();

    // Notification Engine (tüm platformun tek bildirim altyapısı)
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<NotificationEvent> NotificationEvents => Set<NotificationEvent>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();

    // Workflow Engine (reusable, versioned governance workflows)
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowVersion> WorkflowVersions => Set<WorkflowVersion>();
    public DbSet<WorkflowStepDefinition> WorkflowStepDefinitions => Set<WorkflowStepDefinition>();
    public DbSet<WorkflowTransitionDefinition> WorkflowTransitionDefinitions => Set<WorkflowTransitionDefinition>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowStepInstance> WorkflowStepInstances => Set<WorkflowStepInstance>();
    public DbSet<WorkflowEvent> WorkflowEvents => Set<WorkflowEvent>();

    // Integration Hub (central external-integration infrastructure)
    public DbSet<IntegrationDefinition> IntegrationDefinitions => Set<IntegrationDefinition>();
    public DbSet<IntegrationCredential> IntegrationCredentials => Set<IntegrationCredential>();
    public DbSet<IntegrationEndpoint> IntegrationEndpoints => Set<IntegrationEndpoint>();
    public DbSet<IntegrationSubscription> IntegrationSubscriptions => Set<IntegrationSubscription>();
    public DbSet<IntegrationExecution> IntegrationExecutions => Set<IntegrationExecution>();
    public DbSet<IntegrationExecutionAttempt> IntegrationExecutionAttempts => Set<IntegrationExecutionAttempt>();
    public DbSet<IntegrationEvent> IntegrationEvents => Set<IntegrationEvent>();
    public DbSet<ExternalObjectLink> ExternalObjectLinks => Set<ExternalObjectLink>();

    // Background processing (worker liveness)
    public DbSet<WorkerHeartbeat> WorkerHeartbeats => Set<WorkerHeartbeat>();

    // Unified Audit read model (keyless view over the domain audit tables — read-only)
    public DbSet<UnifiedAuditRecord> UnifiedAuditRecords => Set<UnifiedAuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- AppUser ----
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).ValueGeneratedNever();
            entity.Property(u => u.RowVersion).IsRowVersion();
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
            entity.Property(u => u.NormalizedEmail).IsRequired().HasMaxLength(200);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(u => u.Status).IsRequired().HasMaxLength(30);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.NormalizedEmail).IsUnique();
        });

        // ---- Role ----
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedNever();
            entity.Property(r => r.Name).IsRequired().HasMaxLength(50);
            entity.Property(r => r.Description).HasMaxLength(250);
            entity.HasIndex(r => r.Name).IsUnique();
        });

        // ---- UserRole ----
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => ur.Id);
            entity.Property(ur => ur.Id).ValueGeneratedNever();

            entity.HasOne(ur => ur.AppUser)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(ur => new { ur.AppUserId, ur.RoleId }).IsUnique();
        });

        // ---- Permission ----
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).ValueGeneratedNever();
            entity.Property(p => p.Code).IsRequired().HasMaxLength(80);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(150);
            entity.Property(p => p.Description).HasMaxLength(300);
            entity.Property(p => p.Module).IsRequired().HasMaxLength(40);
            entity.HasIndex(p => p.Code).IsUnique();
        });

        // ---- RolePermission ----
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => rp.Id);
            entity.Property(rp => rp.Id).ValueGeneratedNever();

            entity.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
        });

        // ---- RefreshToken ----
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).ValueGeneratedNever();
            entity.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
            entity.Property(t => t.CreatedByIp).HasMaxLength(64);
            entity.Property(t => t.RevokedByIp).HasMaxLength(64);
            entity.Property(t => t.ReasonRevoked).HasMaxLength(200);

            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.HasIndex(t => t.AppUserId);
            entity.HasIndex(t => t.ExpiresAt);

            entity.HasOne(t => t.AppUser)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---- SecurityAuditEvent ----
        modelBuilder.Entity<SecurityAuditEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Result).IsRequired().HasMaxLength(20);
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.UserAgent).HasMaxLength(400);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EventType);
        });

        // ---- Customer ----
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
            entity.Property(c => c.Code).IsRequired().HasMaxLength(30);
            entity.Property(c => c.Status).IsRequired().HasMaxLength(30);
            entity.HasIndex(c => c.Code).IsUnique();
        });

        // ---- Project ----
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(150);
            entity.Property(p => p.Code).IsRequired().HasMaxLength(30);
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.Property(p => p.Status).IsRequired().HasMaxLength(30);
            entity.HasIndex(p => p.Code).IsUnique();

            entity.HasOne(p => p.Customer)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---- AppEnvironment (tablo: Environments) ----
        modelBuilder.Entity<AppEnvironment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(30);

            entity.HasOne(e => e.Project)
                .WithMany(p => p.Environments)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        ConfigureChangeDomain(modelBuilder);
        ConfigureApprovalDomain(modelBuilder);
        ConfigureReleaseDomain(modelBuilder);
        ConfigureExecutionDomain(modelBuilder);
        ConfigureValidationDomain(modelBuilder);
        ConfigureDocumentDomain(modelBuilder);
        ConfigureNotificationDomain(modelBuilder);
        ConfigureWorkflowDomain(modelBuilder);
        ConfigureIntegrationDomain(modelBuilder);
        ConfigureBackgroundDomain(modelBuilder);

        // Unified audit read model — keyless, mapped to a SQL view (created via migration SQL).
        modelBuilder.Entity<UnifiedAuditRecord>().HasNoKey().ToView("vw_UnifiedAuditRecords");

        SeedData(modelBuilder);
        SeedNotificationTemplates(modelBuilder);
        SeedBusinessData(modelBuilder);
        SeedChangeData(modelBuilder);
        SeedWorkflowData(modelBuilder);
    }

    /// <summary>
    /// Release Planning entity configuration. ReleasePlan is the System of Record.
    /// Parent FKs (Customer/Project/Environment/User) are Restrict; child collections
    /// cascade. Item → ChangeRequest is Restrict (change is owned by its own domain).
    /// Guid PKs are ValueGeneratedNever (assigned in code).
    /// </summary>
    private static void ConfigureReleaseDomain(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReleasePlan>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedNever();
            entity.Property(r => r.RowVersion).IsRowVersion();
            entity.Property(r => r.ReleaseNo).IsRequired().HasMaxLength(30);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(150);
            entity.Property(r => r.Version).IsRequired().HasMaxLength(50);
            entity.Property(r => r.ReleaseType).IsRequired().HasMaxLength(30);
            entity.Property(r => r.Status).IsRequired().HasMaxLength(30);
            entity.Property(r => r.RiskLevel).HasMaxLength(20);
            entity.Property(r => r.RollbackWindow).HasMaxLength(120);
            entity.Property(r => r.BusinessOwner).HasMaxLength(150);
            entity.Property(r => r.TechnicalOwner).HasMaxLength(150);
            entity.Property(r => r.Description).HasMaxLength(2000);

            entity.HasIndex(r => r.ReleaseNo).IsUnique();
            entity.HasIndex(r => r.CustomerId);
            entity.HasIndex(r => r.ProjectId);
            entity.HasIndex(r => r.EnvironmentId);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => r.CreatedAt);

            entity.HasOne(r => r.Customer).WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Project).WithMany().HasForeignKey(r => r.ProjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Environment).WithMany().HasForeignKey(r => r.EnvironmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.ReleaseManagerUser).WithMany().HasForeignKey(r => r.ReleaseManagerUserId).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.DeploymentPlan)
                .WithOne(d => d.ReleasePlan!)
                .HasForeignKey<ReleaseDeploymentPlan>(d => d.ReleasePlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReleaseDeploymentPlan>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id).ValueGeneratedNever();
            entity.Property(d => d.DeploymentStrategy).HasMaxLength(120);
            entity.Property(d => d.CommunicationPlan).HasMaxLength(2000);
            entity.Property(d => d.RollbackStrategy).HasMaxLength(2000);
            entity.Property(d => d.Notes).HasMaxLength(2000);
            entity.HasIndex(d => d.ReleasePlanId).IsUnique();
        });

        modelBuilder.Entity<ReleasePlanItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Id).ValueGeneratedNever();

            entity.HasIndex(i => new { i.ReleasePlanId, i.ChangeRequestId }).IsUnique();
            entity.HasIndex(i => new { i.ReleasePlanId, i.DeploymentOrder }).IsUnique();

            entity.HasOne(i => i.ReleasePlan)
                .WithMany(r => r.Items)
                .HasForeignKey(i => i.ReleasePlanId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.ChangeRequest)
                .WithMany()
                .HasForeignKey(i => i.ChangeRequestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReleaseDocument>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id).ValueGeneratedNever();
            entity.Property(d => d.DocumentType).IsRequired().HasMaxLength(50);
            entity.Property(d => d.DocumentName).IsRequired().HasMaxLength(250);
            entity.Property(d => d.Version).HasMaxLength(30);

            entity.HasIndex(d => d.ReleasePlanId);

            entity.HasOne(d => d.ReleasePlan)
                .WithMany(r => r.Documents)
                .HasForeignKey(d => d.ReleasePlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReleaseAuditEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasIndex(e => e.ReleasePlanId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.ReleasePlan)
                .WithMany(r => r.AuditEvents)
                .HasForeignKey(e => e.ReleasePlanId)
                .OnDelete(DeleteBehavior.Restrict); // audit immutability

            entity.HasOne(e => e.ActorUser)
                .WithMany()
                .HasForeignKey(e => e.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// Execution entity configuration. DeploymentRun is the aggregate root (RowVersion
    /// concurrency token); Steps/Events cascade from the run. Step → ReleasePlanItem and
    /// all user FKs are Restrict. Guid PKs are ValueGeneratedNever (assigned in code).
    /// </summary>
    private static void ConfigureExecutionDomain(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeploymentRun>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedNever();
            entity.Property(r => r.RowVersion).IsRowVersion();
            entity.Property(r => r.ExecutionNo).IsRequired().HasMaxLength(30);
            entity.Property(r => r.Status).IsRequired().HasMaxLength(30);
            entity.Property(r => r.OverallResult).HasMaxLength(30);
            entity.Property(r => r.Notes).HasMaxLength(2000);

            entity.HasIndex(r => r.ExecutionNo).IsUnique();
            entity.HasIndex(r => r.ReleasePlanId);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => r.CreatedAt);

            entity.HasOne(r => r.ReleasePlan)
                .WithMany()
                .HasForeignKey(r => r.ReleasePlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.ExecutedByUser)
                .WithMany()
                .HasForeignKey(r => r.ExecutedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DeploymentStep>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedNever();
            entity.Property(s => s.Title).IsRequired().HasMaxLength(250);
            entity.Property(s => s.Status).IsRequired().HasMaxLength(30);
            entity.Property(s => s.ExecutionResult).HasMaxLength(30);
            entity.Property(s => s.Notes).HasMaxLength(2000);

            entity.HasIndex(s => new { s.DeploymentRunId, s.StepOrder }).IsUnique();

            entity.HasOne(s => s.DeploymentRun)
                .WithMany(r => r.Steps)
                .HasForeignKey(s => s.DeploymentRunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.ReleasePlanItem)
                .WithMany()
                .HasForeignKey(s => s.ReleasePlanItemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.ExecutedByUser)
                .WithMany()
                .HasForeignKey(s => s.ExecutedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DeploymentEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasIndex(e => e.DeploymentRunId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.DeploymentRun)
                .WithMany(r => r.Events)
                .HasForeignKey(e => e.DeploymentRunId)
                .OnDelete(DeleteBehavior.Restrict); // audit immutability

            entity.HasOne(e => e.ActorUser)
                .WithMany()
                .HasForeignKey(e => e.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// Validation entity configuration. ValidationRun is the aggregate root (RowVersion
    /// concurrency token); Checks/Evidence/Events cascade from the run. Run → DeploymentRun
    /// and all user FKs are Restrict. Guid PKs are ValueGeneratedNever (assigned in code).
    /// </summary>
    private static void ConfigureValidationDomain(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ValidationRun>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedNever();
            entity.Property(r => r.RowVersion).IsRowVersion();
            entity.Property(r => r.ValidationNo).IsRequired().HasMaxLength(30);
            entity.Property(r => r.Status).IsRequired().HasMaxLength(30);
            entity.Property(r => r.ValidationType).IsRequired().HasMaxLength(30);
            entity.Property(r => r.OverallResult).HasMaxLength(30);
            entity.Property(r => r.Summary).HasMaxLength(2000);

            entity.HasIndex(r => r.ValidationNo).IsUnique();
            entity.HasIndex(r => r.DeploymentRunId);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => r.CreatedAt);

            entity.HasOne(r => r.DeploymentRun)
                .WithMany()
                .HasForeignKey(r => r.DeploymentRunId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.ValidatedByUser)
                .WithMany()
                .HasForeignKey(r => r.ValidatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ValidationCheck>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).ValueGeneratedNever();
            entity.Property(c => c.Title).IsRequired().HasMaxLength(250);
            entity.Property(c => c.ExpectedResult).HasMaxLength(1000);
            entity.Property(c => c.ActualResult).HasMaxLength(1000);
            entity.Property(c => c.Status).IsRequired().HasMaxLength(30);
            entity.Property(c => c.Notes).HasMaxLength(2000);

            entity.HasIndex(c => new { c.ValidationRunId, c.CheckOrder }).IsUnique();

            entity.HasOne(c => c.ValidationRun)
                .WithMany(r => r.Checks)
                .HasForeignKey(c => c.ValidationRunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.ExecutedByUser)
                .WithMany()
                .HasForeignKey(c => c.ExecutedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ValidationEvidence>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.EvidenceType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(250);
            entity.Property(e => e.Version).HasMaxLength(30);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasIndex(e => e.ValidationRunId);

            entity.HasOne(e => e.ValidationRun)
                .WithMany(r => r.Evidence)
                .HasForeignKey(e => e.ValidationRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ValidationEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasIndex(e => e.ValidationRunId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.ValidationRun)
                .WithMany(r => r.Events)
                .HasForeignKey(e => e.ValidationRunId)
                .OnDelete(DeleteBehavior.Restrict); // audit immutability

            entity.HasOne(e => e.ActorUser)
                .WithMany()
                .HasForeignKey(e => e.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// Document Management entity configuration. Document is the aggregate root (RowVersion
    /// concurrency); Versions/Links/AuditEvents/Downloads cascade from it. CurrentVersionId
    /// and Download.VersionId are plain references (no FK) to avoid cyclic cascade paths.
    /// User FKs are Restrict. Guid PKs are ValueGeneratedNever (assigned in code).
    /// </summary>
    private static void ConfigureDocumentDomain(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id).ValueGeneratedNever();
            entity.Property(d => d.RowVersion).IsRowVersion();
            entity.Property(d => d.DocumentNo).IsRequired().HasMaxLength(30);
            entity.Property(d => d.Title).IsRequired().HasMaxLength(250);
            entity.Property(d => d.Description).HasMaxLength(2000);
            entity.Property(d => d.Category).IsRequired().HasMaxLength(50);
            entity.Property(d => d.Status).IsRequired().HasMaxLength(30);
            entity.Property(d => d.HashAlgorithm).HasMaxLength(20);
            entity.Property(d => d.CurrentHash).HasMaxLength(128);

            entity.HasIndex(d => d.DocumentNo).IsUnique();
            entity.HasIndex(d => d.Category);
            entity.HasIndex(d => d.Status);
            entity.HasIndex(d => d.OwnerUserId);
            entity.HasIndex(d => d.CreatedAt);

            entity.HasOne(d => d.OwnerUser)
                .WithMany()
                .HasForeignKey(d => d.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Id).ValueGeneratedNever();
            entity.Property(v => v.StoragePath).IsRequired().HasMaxLength(400);
            entity.Property(v => v.OriginalFileName).IsRequired().HasMaxLength(260);
            entity.Property(v => v.StoredFileName).IsRequired().HasMaxLength(100);
            entity.Property(v => v.Extension).HasMaxLength(20);
            entity.Property(v => v.MimeType).HasMaxLength(150);
            entity.Property(v => v.Sha256Hash).IsRequired().HasMaxLength(128);
            entity.Property(v => v.Comment).HasMaxLength(1000);

            entity.HasIndex(v => new { v.DocumentId, v.VersionNumber }).IsUnique();

            entity.HasOne(v => v.Document)
                .WithMany(d => d.Versions)
                .HasForeignKey(v => v.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.UploadedByUser)
                .WithMany()
                .HasForeignKey(v => v.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DocumentLink>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Id).ValueGeneratedNever();
            entity.Property(l => l.ObjectType).IsRequired().HasMaxLength(40);

            entity.HasIndex(l => new { l.DocumentId, l.ObjectType, l.ObjectId }).IsUnique();
            entity.HasIndex(l => new { l.ObjectType, l.ObjectId });

            entity.HasOne(l => l.Document)
                .WithMany(d => d.Links)
                .HasForeignKey(l => l.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentAuditEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Document)
                .WithMany(d => d.AuditEvents)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Restrict); // audit immutability
        });

        modelBuilder.Entity<DocumentDownload>(entity =>
        {
            entity.HasKey(dl => dl.Id);
            entity.Property(dl => dl.Id).ValueGeneratedNever();
            entity.Property(dl => dl.IpAddress).HasMaxLength(64);

            entity.HasIndex(dl => dl.DocumentId);
            entity.HasIndex(dl => dl.DownloadedAt);

            entity.HasOne(dl => dl.Document)
                .WithMany(d => d.Downloads)
                .HasForeignKey(dl => dl.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Notification Engine entity configuration. Notification is the aggregate root
    /// (RowVersion); Events/Deliveries cascade from it. CreatedByUserId is a plain nullable
    /// reference (system-generated → null). User FKs are Restrict; Guid PKs ValueGeneratedNever.
    /// </summary>
    private static void ConfigureNotificationDomain(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Id).ValueGeneratedNever();
            entity.Property(n => n.RowVersion).IsRowVersion();
            entity.Property(n => n.NotificationNo).IsRequired().HasMaxLength(30);
            entity.Property(n => n.Title).IsRequired().HasMaxLength(250);
            entity.Property(n => n.Message).HasMaxLength(2000);
            entity.Property(n => n.Type).IsRequired().HasMaxLength(80);
            entity.Property(n => n.Severity).IsRequired().HasMaxLength(20);
            entity.Property(n => n.Module).IsRequired().HasMaxLength(40);
            entity.Property(n => n.Status).IsRequired().HasMaxLength(20);
            entity.Property(n => n.RecipientRole).HasMaxLength(50);

            entity.HasIndex(n => n.NotificationNo).IsUnique();
            entity.HasIndex(n => new { n.RecipientUserId, n.Status });
            entity.HasIndex(n => n.CreatedAt);

            entity.HasOne(n => n.RecipientUser)
                .WithMany()
                .HasForeignKey(n => n.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).ValueGeneratedNever();
            entity.Property(t => t.Code).IsRequired().HasMaxLength(80);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(150);
            entity.Property(t => t.SubjectTemplate).IsRequired().HasMaxLength(300);
            entity.Property(t => t.BodyTemplate).IsRequired().HasMaxLength(2000);
            entity.Property(t => t.Module).IsRequired().HasMaxLength(40);
            entity.HasIndex(t => t.Code).IsUnique();
        });

        modelBuilder.Entity<NotificationPreference>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).ValueGeneratedNever();
            entity.Property(p => p.Module).IsRequired().HasMaxLength(40);
            entity.HasIndex(p => new { p.UserId, p.Module }).IsUnique();

            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasIndex(e => e.NotificationId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Notification)
                .WithMany(n => n.Events)
                .HasForeignKey(e => e.NotificationId)
                .OnDelete(DeleteBehavior.Restrict); // audit immutability
        });

        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.HasKey(dl => dl.Id);
            entity.Property(dl => dl.Id).ValueGeneratedNever();
            entity.Property(dl => dl.Channel).IsRequired().HasMaxLength(20);
            entity.Property(dl => dl.Status).IsRequired().HasMaxLength(20);
            entity.Property(dl => dl.FailureReason).HasMaxLength(500);

            entity.HasIndex(dl => dl.NotificationId);

            entity.HasOne(dl => dl.Notification)
                .WithMany(n => n.Deliveries)
                .HasForeignKey(dl => dl.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Workflow Engine entity configuration. Definition → Versions → Steps/Transitions cascade
    /// (one owning path). Instance → StepInstances cascade; Instance → Events is Restrict (audit
    /// immutability). ActiveVersionId / CurrentStepInstanceId / StepDefinitionId /
    /// WorkflowStepInstanceId are plain references (no FK) to avoid cyclic cascade paths.
    /// Instance → Definition/Version are Restrict. Guid PKs are ValueGeneratedNever.
    /// </summary>
    private static void ConfigureWorkflowDomain(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowDefinition>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id).ValueGeneratedNever();
            entity.Property(d => d.RowVersion).IsRowVersion();
            entity.Property(d => d.Code).IsRequired().HasMaxLength(80);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(150);
            entity.Property(d => d.Description).HasMaxLength(1000);
            entity.Property(d => d.Category).IsRequired().HasMaxLength(50);
            entity.Property(d => d.TriggerObjectType).IsRequired().HasMaxLength(50);
            entity.Property(d => d.TriggerEvent).IsRequired().HasMaxLength(50);
            entity.Property(d => d.ChangeClass).HasMaxLength(30);
            entity.Property(d => d.Status).IsRequired().HasMaxLength(20);

            entity.HasIndex(d => d.Code).IsUnique();
            entity.HasIndex(d => d.Category);
            entity.HasIndex(d => d.Status);
            entity.HasIndex(d => new { d.TriggerObjectType, d.ChangeClass });
        });

        modelBuilder.Entity<WorkflowVersion>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Id).ValueGeneratedNever();
            entity.Property(v => v.RowVersion).IsRowVersion();
            entity.Property(v => v.Status).IsRequired().HasMaxLength(20);
            entity.Property(v => v.StartStepKey).HasMaxLength(40);
            entity.Property(v => v.Notes).HasMaxLength(1000);

            entity.HasIndex(v => new { v.WorkflowDefinitionId, v.VersionNumber }).IsUnique();
            entity.HasIndex(v => v.Status);

            entity.HasOne(v => v.WorkflowDefinition)
                .WithMany(d => d.Versions)
                .HasForeignKey(v => v.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowStepDefinition>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedNever();
            entity.Property(s => s.StepKey).IsRequired().HasMaxLength(40);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(150);
            entity.Property(s => s.StepType).IsRequired().HasMaxLength(20);
            entity.Property(s => s.AssignedRole).HasMaxLength(50);
            entity.Property(s => s.NotificationTemplateCode).HasMaxLength(80);
            entity.Property(s => s.NotificationRole).HasMaxLength(50);
            entity.Property(s => s.Description).HasMaxLength(1000);

            entity.HasIndex(s => new { s.WorkflowVersionId, s.StepKey }).IsUnique();
            entity.HasIndex(s => new { s.WorkflowVersionId, s.StepOrder });

            entity.HasOne(s => s.WorkflowVersion)
                .WithMany(v => v.Steps)
                .HasForeignKey(s => s.WorkflowVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowTransitionDefinition>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).ValueGeneratedNever();
            entity.Property(t => t.FromStepKey).IsRequired().HasMaxLength(40);
            entity.Property(t => t.ToStepKey).IsRequired().HasMaxLength(40);
            entity.Property(t => t.ConditionType).IsRequired().HasMaxLength(20);
            entity.Property(t => t.ConditionField).HasMaxLength(40);
            entity.Property(t => t.Operator).HasMaxLength(20);
            entity.Property(t => t.ExpectedValue).HasMaxLength(120);
            entity.Property(t => t.Description).HasMaxLength(1000);

            entity.HasIndex(t => t.WorkflowVersionId);
            entity.HasIndex(t => new { t.WorkflowVersionId, t.FromStepKey, t.Priority });

            entity.HasOne(t => t.WorkflowVersion)
                .WithMany(v => v.Transitions)
                .HasForeignKey(t => t.WorkflowVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowInstance>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Id).ValueGeneratedNever();
            entity.Property(i => i.RowVersion).IsRowVersion();
            entity.Property(i => i.InstanceNo).IsRequired().HasMaxLength(30);
            entity.Property(i => i.TriggerObjectType).IsRequired().HasMaxLength(50);
            entity.Property(i => i.TriggerObjectNumber).HasMaxLength(50);
            entity.Property(i => i.Status).IsRequired().HasMaxLength(20);
            entity.Property(i => i.ContextJson).HasMaxLength(4000);
            entity.Property(i => i.Outcome).HasMaxLength(1000);

            entity.HasIndex(i => i.InstanceNo).IsUnique();
            entity.HasIndex(i => new { i.TriggerObjectType, i.TriggerObjectId });
            entity.HasIndex(i => i.Status);
            entity.HasIndex(i => i.WorkflowDefinitionId);
            entity.HasIndex(i => i.CreatedAt);

            entity.HasOne(i => i.WorkflowDefinition)
                .WithMany()
                .HasForeignKey(i => i.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.WorkflowVersion)
                .WithMany()
                .HasForeignKey(i => i.WorkflowVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkflowStepInstance>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedNever();
            entity.Property(s => s.StepKey).IsRequired().HasMaxLength(40);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(150);
            entity.Property(s => s.StepType).IsRequired().HasMaxLength(20);
            entity.Property(s => s.Status).IsRequired().HasMaxLength(20);
            entity.Property(s => s.AssignedRole).HasMaxLength(50);
            entity.Property(s => s.Result).HasMaxLength(20);
            entity.Property(s => s.Comment).HasMaxLength(2000);

            entity.HasIndex(s => s.WorkflowInstanceId);
            entity.HasIndex(s => new { s.WorkflowInstanceId, s.Status });
            entity.HasIndex(s => s.AssignedUserId);
            entity.HasIndex(s => s.AssignedRole);

            entity.HasOne(s => s.WorkflowInstance)
                .WithMany(i => i.StepInstances)
                .HasForeignKey(s => s.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasIndex(e => e.WorkflowInstanceId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.WorkflowInstance)
                .WithMany(i => i.Events)
                .HasForeignKey(e => e.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Restrict); // audit immutability
        });
    }

    /// <summary>
    /// Seeds the default governance workflows from <see cref="Common.WorkflowCatalog"/>. Each
    /// catalog entry becomes a WorkflowDefinition (Active) + one Published v1 (with its steps and
    /// transitions), and the definition's ActiveVersionId points at that version. All ids are
    /// deterministic (derived from stable keys) so migrations stay reproducible.
    /// </summary>
    private static void SeedWorkflowData(ModelBuilder modelBuilder)
    {
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var admin = new Guid("b2222222-2222-2222-2222-222222222205");

        var definitions = new List<WorkflowDefinition>();
        var versions = new List<WorkflowVersion>();
        var steps = new List<WorkflowStepDefinition>();
        var transitions = new List<WorkflowTransitionDefinition>();

        foreach (var wf in Common.WorkflowCatalog.All)
        {
            var defId = DeterministicGuid($"wfdef:{wf.Code}");
            var verId = DeterministicGuid($"wfver:{wf.Code}:1");
            var startKey = wf.Steps.First(step => step.StepType == Common.WorkflowStepTypes.Start).StepKey;

            definitions.Add(new WorkflowDefinition
            {
                Id = defId, Code = wf.Code, Name = wf.Name, Description = wf.Description,
                Category = wf.Category, TriggerObjectType = wf.TriggerObjectType, TriggerEvent = wf.TriggerEvent,
                ChangeClass = wf.ChangeClass, Status = Common.WorkflowDefinitionStatuses.Active,
                ActiveVersionId = verId, IsSystem = true, CreatedByUserId = admin, CreatedAt = createdAt
            });

            versions.Add(new WorkflowVersion
            {
                Id = verId, WorkflowDefinitionId = defId, VersionNumber = 1,
                Status = Common.WorkflowVersionStatuses.Published, StartStepKey = startKey,
                Notes = "İlk yayınlanan sürüm (seed).", CreatedByUserId = admin, CreatedAt = createdAt,
                PublishedAt = createdAt, PublishedByUserId = admin
            });

            foreach (var s in wf.Steps)
            {
                steps.Add(new WorkflowStepDefinition
                {
                    Id = DeterministicGuid($"wfstep:{wf.Code}:1:{s.StepKey}"),
                    WorkflowVersionId = verId, StepKey = s.StepKey, Name = s.Name, StepType = s.StepType,
                    StepOrder = s.StepOrder, AssignedRole = s.AssignedRole, IsRequired = s.IsRequired,
                    DueInHours = s.DueInHours
                });
            }

            var ti = 0;
            foreach (var t in wf.Transitions)
            {
                transitions.Add(new WorkflowTransitionDefinition
                {
                    Id = DeterministicGuid($"wftr:{wf.Code}:1:{t.FromStepKey}->{t.ToStepKey}:{ti++}"),
                    WorkflowVersionId = verId, FromStepKey = t.FromStepKey, ToStepKey = t.ToStepKey,
                    ConditionType = t.ConditionType, Priority = t.Priority, ConditionField = t.ConditionField,
                    Operator = t.Operator, ExpectedValue = t.ExpectedValue
                });
            }
        }

        modelBuilder.Entity<WorkflowDefinition>().HasData(definitions);
        modelBuilder.Entity<WorkflowVersion>().HasData(versions);
        modelBuilder.Entity<WorkflowStepDefinition>().HasData(steps);
        modelBuilder.Entity<WorkflowTransitionDefinition>().HasData(transitions);
    }

    /// <summary>
    /// Integration Hub entity configuration. Definition → Credentials/Endpoints/Subscriptions
    /// cascade; Definition → Events and Execution → Events are Restrict (audit immutability, and to
    /// avoid multiple cascade paths). Execution → Attempts cascade. Subscription.TargetEndpointId
    /// and Execution/Event.* user ids are plain references (no FK). Guid PKs are ValueGeneratedNever.
    /// </summary>
    private static void ConfigureIntegrationDomain(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IntegrationDefinition>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id).ValueGeneratedNever();
            entity.Property(d => d.RowVersion).IsRowVersion();
            entity.Property(d => d.IntegrationNo).IsRequired().HasMaxLength(30);
            entity.Property(d => d.Code).IsRequired().HasMaxLength(60);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(150);
            entity.Property(d => d.Description).HasMaxLength(1000);
            entity.Property(d => d.Provider).IsRequired().HasMaxLength(40);
            entity.Property(d => d.Category).IsRequired().HasMaxLength(40);
            entity.Property(d => d.Status).IsRequired().HasMaxLength(20);
            entity.Property(d => d.BaseUrl).HasMaxLength(500);
            entity.Property(d => d.AuthenticationType).IsRequired().HasMaxLength(40);

            entity.HasIndex(d => d.IntegrationNo).IsUnique();
            entity.HasIndex(d => d.Code).IsUnique();
            entity.HasIndex(d => d.Provider);
            entity.HasIndex(d => d.Status);
            entity.HasIndex(d => d.Category);
        });

        modelBuilder.Entity<IntegrationCredential>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).ValueGeneratedNever();
            entity.Property(c => c.CredentialType).IsRequired().HasMaxLength(40);
            entity.Property(c => c.KeyName).IsRequired().HasMaxLength(60);
            entity.Property(c => c.EncryptedValue).IsRequired().HasMaxLength(4000);
            entity.Property(c => c.MaskedValue).HasMaxLength(120);

            entity.HasIndex(c => c.IntegrationDefinitionId);
            entity.HasIndex(c => new { c.IntegrationDefinitionId, c.KeyName }).IsUnique();

            entity.HasOne(c => c.IntegrationDefinition)
                .WithMany(d => d.Credentials)
                .HasForeignKey(c => c.IntegrationDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IntegrationEndpoint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Direction).IsRequired().HasMaxLength(20);
            entity.Property(e => e.RelativePath).HasMaxLength(500);
            entity.Property(e => e.HttpMethod).IsRequired().HasMaxLength(10);

            entity.HasIndex(e => e.IntegrationDefinitionId);
            entity.HasIndex(e => e.Direction);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.IntegrationDefinition)
                .WithMany(d => d.Endpoints)
                .HasForeignKey(e => e.IntegrationDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IntegrationSubscription>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedNever();
            entity.Property(s => s.EventType).IsRequired().HasMaxLength(60);
            entity.Property(s => s.ObjectType).HasMaxLength(50);

            entity.HasIndex(s => s.EventType);
            entity.HasIndex(s => new { s.IntegrationDefinitionId, s.IsActive });

            entity.HasOne(s => s.IntegrationDefinition)
                .WithMany(d => d.Subscriptions)
                .HasForeignKey(s => s.IntegrationDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            // TargetEndpointId is a plain reference (no FK) — validated in the service.
        });

        modelBuilder.Entity<IntegrationExecution>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.Property(x => x.ExecutionNo).IsRequired().HasMaxLength(30);
            entity.Property(x => x.Direction).IsRequired().HasMaxLength(20);
            entity.Property(x => x.Operation).IsRequired().HasMaxLength(80);
            entity.Property(x => x.ObjectType).HasMaxLength(50);
            entity.Property(x => x.CorrelationId).IsRequired().HasMaxLength(80);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20);
            entity.Property(x => x.RequestSummary).HasMaxLength(2000);
            entity.Property(x => x.ResponseSummary).HasMaxLength(2000);
            entity.Property(x => x.ErrorCode).HasMaxLength(60);
            entity.Property(x => x.ErrorMessage).HasMaxLength(1000);

            entity.HasIndex(x => x.ExecutionNo).IsUnique();
            entity.HasIndex(x => x.IntegrationDefinitionId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => new { x.ObjectType, x.ObjectId });

            entity.HasOne(x => x.IntegrationDefinition)
                .WithMany()
                .HasForeignKey(x => x.IntegrationDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IntegrationExecutionAttempt>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).ValueGeneratedNever();
            entity.Property(a => a.Status).IsRequired().HasMaxLength(20);
            entity.Property(a => a.ErrorMessage).HasMaxLength(1000);

            entity.HasIndex(a => new { a.IntegrationExecutionId, a.AttemptNo }).IsUnique();
            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.CreatedAt);

            entity.HasOne(a => a.IntegrationExecution)
                .WithMany(x => x.Attempts)
                .HasForeignKey(a => a.IntegrationExecutionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IntegrationEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasIndex(e => new { e.IntegrationDefinitionId, e.CreatedAt });
            entity.HasIndex(e => new { e.IntegrationExecutionId, e.CreatedAt });

            entity.HasOne(e => e.IntegrationDefinition)
                .WithMany(d => d.Events)
                .HasForeignKey(e => e.IntegrationDefinitionId)
                .OnDelete(DeleteBehavior.Restrict); // audit immutability

            entity.HasOne(e => e.IntegrationExecution)
                .WithMany(x => x.Events)
                .HasForeignKey(e => e.IntegrationExecutionId)
                .OnDelete(DeleteBehavior.Restrict); // nullable; avoid extra cascade path
        });

        modelBuilder.Entity<ExternalObjectLink>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Id).ValueGeneratedNever();
            entity.Property(l => l.InternalObjectType).IsRequired().HasMaxLength(50);
            entity.Property(l => l.ExternalObjectType).IsRequired().HasMaxLength(50);
            entity.Property(l => l.ExternalObjectId).IsRequired().HasMaxLength(120);
            entity.Property(l => l.ExternalObjectKey).HasMaxLength(120);
            entity.Property(l => l.ExternalUrl).HasMaxLength(500);

            entity.HasIndex(l => new { l.InternalObjectType, l.InternalObjectId });
            entity.HasIndex(l => new { l.ExternalObjectType, l.ExternalObjectId });
            entity.HasIndex(l => new { l.IntegrationDefinitionId, l.InternalObjectType, l.InternalObjectId, l.ExternalObjectType, l.ExternalObjectId })
                .IsUnique();

            entity.HasOne(l => l.IntegrationDefinition)
                .WithMany()
                .HasForeignKey(l => l.IntegrationDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// Background-processing configuration: lease/backoff columns, worker-query indexes and the
    /// WorkerHeartbeat table. Configures additional facets of already-mapped entities
    /// (IntegrationExecution / NotificationDelivery / WorkflowStepInstance) plus WorkerHeartbeat.
    /// </summary>
    private static void ConfigureBackgroundDomain(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IntegrationExecution>(entity =>
        {
            entity.Property(x => x.LockedBy).HasMaxLength(100);
            // Worker claim query: dispatchable rows ordered by CreatedAt.
            entity.HasIndex(x => new { x.Status, x.NextAttemptAt });
            entity.HasIndex(x => x.LockedUntil);
        });

        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.Property(dl => dl.RowVersion).IsRowVersion();
            entity.Property(dl => dl.LockedBy).HasMaxLength(100);
            // Worker claim query: pending Email deliveries.
            entity.HasIndex(dl => new { dl.Channel, dl.Status });
            entity.HasIndex(dl => dl.NextAttemptAt);
        });

        modelBuilder.Entity<WorkflowStepInstance>(entity =>
        {
            // SLA worker query: active steps with a due date.
            entity.HasIndex(s => new { s.Status, s.DueAt });
        });

        modelBuilder.Entity<WorkerHeartbeat>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Id).ValueGeneratedNever();
            entity.Property(h => h.RowVersion).IsRowVersion();
            entity.Property(h => h.WorkerName).IsRequired().HasMaxLength(80);
            entity.Property(h => h.InstanceId).IsRequired().HasMaxLength(100);
            entity.Property(h => h.LastError).HasMaxLength(1000);
            entity.HasIndex(h => new { h.WorkerName, h.InstanceId }).IsUnique();
        });
    }

    /// <summary>Seeds the system notification templates from the central catalog (deterministic).</summary>
    private static void SeedNotificationTemplates(ModelBuilder modelBuilder)
    {
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<NotificationTemplate>().HasData(Common.NotificationTemplates.Catalog.Select(t => new NotificationTemplate
        {
            Id = DeterministicGuid($"ntpl:{t.Code}"),
            Code = t.Code, Name = t.Name, Module = t.Module,
            SubjectTemplate = t.Subject, BodyTemplate = t.Body,
            IsSystem = true, CreatedAt = createdAt
        }));
    }

    /// <summary>
    /// Approval Management entity configuration. Request → Steps/Decisions/AuditEvents
    /// cascade; Decision → Step is Restrict to avoid a second cascade path to Decisions.
    /// User FKs are Restrict. Guid PKs are ValueGeneratedNever (assigned in code).
    /// </summary>
    private static void ConfigureApprovalDomain(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApprovalRequest>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).ValueGeneratedNever();
            entity.Property(a => a.RowVersion).IsRowVersion();
            entity.Property(a => a.ApprovalNo).IsRequired().HasMaxLength(30);
            entity.Property(a => a.RelatedObjectType).IsRequired().HasMaxLength(50);
            entity.Property(a => a.Title).IsRequired().HasMaxLength(200);
            entity.Property(a => a.Description).HasMaxLength(2000);
            entity.Property(a => a.Status).IsRequired().HasMaxLength(30);
            entity.Property(a => a.Priority).HasMaxLength(20);

            entity.HasIndex(a => a.ApprovalNo).IsUnique();
            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => new { a.RelatedObjectType, a.RelatedObjectId });
            entity.HasIndex(a => a.RequestedByUserId);
            entity.HasIndex(a => a.CreatedAt);

            entity.HasOne(a => a.RequestedByUser)
                .WithMany()
                .HasForeignKey(a => a.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApprovalStep>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedNever();
            entity.Property(s => s.StepName).IsRequired().HasMaxLength(120);
            entity.Property(s => s.ApproverRole).IsRequired().HasMaxLength(50);
            entity.Property(s => s.Status).IsRequired().HasMaxLength(30);

            entity.HasIndex(s => new { s.ApprovalRequestId, s.StepNo }).IsUnique();

            entity.HasOne(s => s.ApprovalRequest)
                .WithMany(a => a.Steps)
                .HasForeignKey(s => s.ApprovalRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.ApproverUser)
                .WithMany()
                .HasForeignKey(s => s.ApproverUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApprovalDecision>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id).ValueGeneratedNever();
            entity.Property(d => d.Decision).IsRequired().HasMaxLength(30);
            entity.Property(d => d.Comment).HasMaxLength(2000);
            entity.Property(d => d.SignatureMeaning).HasMaxLength(400);

            entity.HasIndex(d => d.ApprovalRequestId);

            entity.HasOne(d => d.ApprovalRequest)
                .WithMany(a => a.Decisions)
                .HasForeignKey(d => d.ApprovalRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict to avoid a second cascade path (Request -> Steps -> Decisions).
            entity.HasOne(d => d.ApprovalStep)
                .WithMany()
                .HasForeignKey(d => d.ApprovalStepId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.SignedByUser)
                .WithMany()
                .HasForeignKey(d => d.SignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApprovalAuditEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasIndex(e => e.ApprovalRequestId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.ApprovalRequest)
                .WithMany(a => a.AuditEvents)
                .HasForeignKey(e => e.ApprovalRequestId)
                .OnDelete(DeleteBehavior.Restrict); // audit immutability

            entity.HasOne(e => e.ActorUser)
                .WithMany()
                .HasForeignKey(e => e.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// Change Management entity configuration. Parent FKs (Customer/Project/
    /// Environment/User) use Restrict to avoid multiple cascade paths; the child
    /// collections cascade from ChangeRequest (single owning path).
    /// </summary>
    private static void ConfigureChangeDomain(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChangeRequest>(entity =>
        {
            entity.HasKey(c => c.Id);
            // Keys are always assigned client-side (Guid.NewGuid()); ValueGeneratedNever
            // makes EF add new children to a tracked parent's collection as INSERT (not UPDATE).
            entity.Property(c => c.Id).ValueGeneratedNever();
            entity.Property(c => c.RowVersion).IsRowVersion();
            entity.Property(c => c.ChangeNo).IsRequired().HasMaxLength(30);
            entity.Property(c => c.Title).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Description).HasMaxLength(2000);
            entity.Property(c => c.BusinessReason).HasMaxLength(2000);
            entity.Property(c => c.ChangeClass).IsRequired().HasMaxLength(30);
            entity.Property(c => c.ChangeType).IsRequired().HasMaxLength(50);
            entity.Property(c => c.Priority).IsRequired().HasMaxLength(20);
            entity.Property(c => c.Status).IsRequired().HasMaxLength(30);
            entity.Property(c => c.RiskLevel).IsRequired().HasMaxLength(20);
            entity.Property(c => c.SourceSystem).HasMaxLength(80);
            entity.Property(c => c.SourceReference).HasMaxLength(120);

            entity.HasIndex(c => c.ChangeNo).IsUnique();
            entity.HasIndex(c => c.Status);
            entity.HasIndex(c => c.ProjectId);
            entity.HasIndex(c => c.EnvironmentId);
            entity.HasIndex(c => c.CreatedAt);
            entity.HasIndex(c => c.RiskLevel);

            entity.HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Project)
                .WithMany()
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Environment)
                .WithMany()
                .HasForeignKey(c => c.EnvironmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ChangeRevision>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedNever();
            entity.Property(r => r.TechnicalSummary).HasMaxLength(2000);
            entity.Property(r => r.ImplementationNotes).HasMaxLength(4000);
            entity.Property(r => r.DeploymentInstructions).HasMaxLength(4000);
            entity.Property(r => r.SqlScript).HasMaxLength(8000);
            entity.Property(r => r.RollbackScript).HasMaxLength(8000);
            entity.Property(r => r.RollbackStrategy).HasMaxLength(1000);
            entity.Property(r => r.RollbackOwner).HasMaxLength(150);

            entity.HasIndex(r => new { r.ChangeRequestId, r.RevisionNo }).IsUnique();

            entity.HasOne(r => r.ChangeRequest)
                .WithMany(c => c.Revisions)
                .HasForeignKey(r => r.ChangeRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChangeAffectedAsset>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).ValueGeneratedNever();
            entity.Property(a => a.AssetType).IsRequired().HasMaxLength(50);
            entity.Property(a => a.AssetName).IsRequired().HasMaxLength(200);
            entity.Property(a => a.Criticality).IsRequired().HasMaxLength(20);
            entity.Property(a => a.Description).HasMaxLength(1000);

            entity.HasIndex(a => a.ChangeRequestId);

            entity.HasOne(a => a.ChangeRequest)
                .WithMany(c => c.Assets)
                .HasForeignKey(a => a.ChangeRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChangeDocument>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id).ValueGeneratedNever();
            entity.Property(d => d.DocumentType).IsRequired().HasMaxLength(50);
            entity.Property(d => d.DocumentName).IsRequired().HasMaxLength(250);
            entity.Property(d => d.Version).HasMaxLength(30);
            entity.Property(d => d.Status).HasMaxLength(30);

            entity.HasIndex(d => d.ChangeRequestId);

            entity.HasOne(d => d.ChangeRequest)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.ChangeRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChangeAuditEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasIndex(e => e.ChangeRequestId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.ChangeRequest)
                .WithMany(c => c.AuditEvents)
                .HasForeignKey(e => e.ChangeRequestId)
                .OnDelete(DeleteBehavior.Restrict); // audit immutability: never cascade-erase audit
        });
    }

    /// <summary>Stable GUID derived from a string — lets us seed permissions/grants
    /// deterministically without hand-writing dozens of GUIDs.</summary>
    private static Guid DeterministicGuid(string input)
    {
        var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }

    /// <summary>
    /// Identity/RBAC seed. Roles, permissions and role-permission grants are fully
    /// deterministic (fixed/derived GUIDs). User password hashes are NOT seeded here —
    /// a Development-only startup seeder sets them (see DevDataSeeder), keeping raw
    /// passwords and non-deterministic hashes out of migration source.
    /// </summary>
    private static void SeedData(ModelBuilder modelBuilder)
    {
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // ---- Roles (8 system roles; existing 5 GUIDs preserved) ----
        var roleIds = new Dictionary<string, Guid>
        {
            [Common.SystemRoles.Requester] = new Guid("a1111111-1111-1111-1111-111111111101"),
            [Common.SystemRoles.Architect] = new Guid("a1111111-1111-1111-1111-111111111102"),
            [Common.SystemRoles.Executor] = new Guid("a1111111-1111-1111-1111-111111111103"),
            [Common.SystemRoles.QA] = new Guid("a1111111-1111-1111-1111-111111111104"),
            [Common.SystemRoles.Admin] = new Guid("a1111111-1111-1111-1111-111111111105"),
            [Common.SystemRoles.ReleaseManager] = new Guid("a1111111-1111-1111-1111-111111111106"),
            [Common.SystemRoles.Validator] = new Guid("a1111111-1111-1111-1111-111111111107"),
            [Common.SystemRoles.Auditor] = new Guid("a1111111-1111-1111-1111-111111111108"),
        };
        var roleDescriptions = new Dictionary<string, string>
        {
            [Common.SystemRoles.Requester] = "Değişiklik/sürüm talebi oluşturan kullanıcı.",
            [Common.SystemRoles.Architect] = "Teknik tasarım ve mimari onay yapan kullanıcı.",
            [Common.SystemRoles.Executor] = "Onaylanan değişiklikleri yürüten kullanıcı.",
            [Common.SystemRoles.QA] = "Kalite onayı ve doğrulama yapan kullanıcı.",
            [Common.SystemRoles.Admin] = "Sistem yöneticisi — tüm yetkiler.",
            [Common.SystemRoles.ReleaseManager] = "Yayın planlama ve yayın onayı yapan kullanıcı.",
            [Common.SystemRoles.Validator] = "Yürütme sonrası doğrulama yapan kullanıcı.",
            [Common.SystemRoles.Auditor] = "Denetim kayıtlarını okuyan kullanıcı.",
        };
        modelBuilder.Entity<Role>().HasData(roleIds.Select(kv => new Role
        {
            Id = kv.Value, Name = kv.Key, Description = roleDescriptions[kv.Key],
            IsSystemRole = true, IsActive = true, CreatedAt = createdAt
        }));

        // ---- Permissions (from the central catalog) ----
        modelBuilder.Entity<Permission>().HasData(Common.Permissions.Catalog.Select(p => new Permission
        {
            Id = DeterministicGuid($"perm:{p.Code}"),
            Code = p.Code, Name = p.Name, Module = p.Module, Description = p.Description, CreatedAt = createdAt
        }));

        // ---- Role-permission grants (from the central matrix) ----
        var rolePermissions = new List<RolePermission>();
        foreach (var (roleName, codes) in Common.RolePermissionMatrix.Map)
        {
            foreach (var code in codes)
            {
                rolePermissions.Add(new RolePermission
                {
                    Id = DeterministicGuid($"rp:{roleName}:{code}"),
                    RoleId = roleIds[roleName],
                    PermissionId = DeterministicGuid($"perm:{code}"),
                    AssignedAt = createdAt
                });
            }
        }
        modelBuilder.Entity<RolePermission>().HasData(rolePermissions);

        // ---- Users (existing 5 GUIDs preserved + 3 new; PasswordHash set at runtime) ----
        var users = new (Guid Id, string Name, string Email, string Role)[]
        {
            (new Guid("b2222222-2222-2222-2222-222222222201"), "Requester User", "requester@gms.local", Common.SystemRoles.Requester),
            (new Guid("b2222222-2222-2222-2222-222222222202"), "Architect User", "architect@gms.local", Common.SystemRoles.Architect),
            (new Guid("b2222222-2222-2222-2222-222222222203"), "Executor User", "executor@gms.local", Common.SystemRoles.Executor),
            (new Guid("b2222222-2222-2222-2222-222222222204"), "QA Specialist", "qa@gms.local", Common.SystemRoles.QA),
            (new Guid("b2222222-2222-2222-2222-222222222205"), "System Administrator", "admin@gms.local", Common.SystemRoles.Admin),
            (new Guid("b2222222-2222-2222-2222-222222222206"), "Release Manager", "release.manager@gms.local", Common.SystemRoles.ReleaseManager),
            (new Guid("b2222222-2222-2222-2222-222222222207"), "Validator User", "validator@gms.local", Common.SystemRoles.Validator),
            (new Guid("b2222222-2222-2222-2222-222222222208"), "Auditor User", "auditor@gms.local", Common.SystemRoles.Auditor),
        };
        modelBuilder.Entity<AppUser>().HasData(users.Select(u => new AppUser
        {
            Id = u.Id, FullName = u.Name, Email = u.Email, NormalizedEmail = u.Email.ToUpperInvariant(),
            PasswordHash = string.Empty, Status = "Active", IsActive = true, FailedLoginCount = 0, CreatedAt = createdAt
        }));

        // ---- User-role assignments (existing 5 GUIDs preserved + 3 new) ----
        var userRoleIds = new Dictionary<string, Guid>
        {
            ["requester@gms.local"] = new Guid("c3333333-3333-3333-3333-333333333301"),
            ["architect@gms.local"] = new Guid("c3333333-3333-3333-3333-333333333302"),
            ["executor@gms.local"] = new Guid("c3333333-3333-3333-3333-333333333303"),
            ["qa@gms.local"] = new Guid("c3333333-3333-3333-3333-333333333304"),
            ["admin@gms.local"] = new Guid("c3333333-3333-3333-3333-333333333305"),
            ["release.manager@gms.local"] = new Guid("c3333333-3333-3333-3333-333333333306"),
            ["validator@gms.local"] = new Guid("c3333333-3333-3333-3333-333333333307"),
            ["auditor@gms.local"] = new Guid("c3333333-3333-3333-3333-333333333308"),
        };
        modelBuilder.Entity<UserRole>().HasData(users.Select(u => new UserRole
        {
            Id = userRoleIds[u.Email], AppUserId = u.Id, RoleId = roleIds[u.Role], AssignedAt = createdAt
        }));
    }

    /// <summary>
    /// İş modeli örnek verileri (müşteri, proje, ortam, yayın).
    /// Determinist migrasyon için sabit GUID ve tarih kullanılır.
    /// </summary>
    private static void SeedBusinessData(ModelBuilder modelBuilder)
    {
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // ---- Müşteriler ----
        var custAbdi = new Guid("d4444444-4444-4444-4444-444444444401");
        var custBilim = new Guid("d4444444-4444-4444-4444-444444444402");

        modelBuilder.Entity<Customer>().HasData(
            new Customer { Id = custAbdi, Name = "Abdi İbrahim", Code = "ABDI", Status = "Active", CreatedAt = createdAt },
            new Customer { Id = custBilim, Name = "Bilim İlaç", Code = "BILIM", Status = "Active", CreatedAt = createdAt }
        );

        // ---- Projeler ----
        var projEbr = new Guid("e5555555-5555-5555-5555-555555555501");
        var projMes = new Guid("e5555555-5555-5555-5555-555555555502");

        modelBuilder.Entity<Project>().HasData(
            new Project { Id = projEbr, CustomerId = custAbdi, Name = "EBR Migration", Code = "EBR-MIG", Description = "Elektronik Batch Record geçiş projesi.", Status = "Active", CreatedAt = createdAt },
            new Project { Id = projMes, CustomerId = custBilim, Name = "MES Upgrade", Code = "MES-UPG", Description = "MES sürüm yükseltme projesi.", Status = "Active", CreatedAt = createdAt }
        );

        // ---- Ortamlar (her projeye DEV/TEST/UAT/PROD) ----
        var envEbrDev = new Guid("f6666666-6666-6666-6666-666666666601");
        var envEbrTest = new Guid("f6666666-6666-6666-6666-666666666602");
        var envEbrUat = new Guid("f6666666-6666-6666-6666-666666666603");
        var envEbrProd = new Guid("f6666666-6666-6666-6666-666666666604");
        var envMesDev = new Guid("f6666666-6666-6666-6666-666666666611");
        var envMesTest = new Guid("f6666666-6666-6666-6666-666666666612");
        var envMesUat = new Guid("f6666666-6666-6666-6666-666666666613");
        var envMesProd = new Guid("f6666666-6666-6666-6666-666666666614");

        modelBuilder.Entity<AppEnvironment>().HasData(
            new AppEnvironment { Id = envEbrDev, ProjectId = projEbr, Name = "DEV", Type = "Geliştirme", Status = "Active", CreatedAt = createdAt },
            new AppEnvironment { Id = envEbrTest, ProjectId = projEbr, Name = "TEST", Type = "Test", Status = "Active", CreatedAt = createdAt },
            new AppEnvironment { Id = envEbrUat, ProjectId = projEbr, Name = "UAT", Type = "Kullanıcı Kabul", Status = "Active", CreatedAt = createdAt },
            new AppEnvironment { Id = envEbrProd, ProjectId = projEbr, Name = "PROD", Type = "Üretim", Status = "Active", CreatedAt = createdAt },
            new AppEnvironment { Id = envMesDev, ProjectId = projMes, Name = "DEV", Type = "Geliştirme", Status = "Active", CreatedAt = createdAt },
            new AppEnvironment { Id = envMesTest, ProjectId = projMes, Name = "TEST", Type = "Test", Status = "Active", CreatedAt = createdAt },
            new AppEnvironment { Id = envMesUat, ProjectId = projMes, Name = "UAT", Type = "Kullanıcı Kabul", Status = "Active", CreatedAt = createdAt },
            new AppEnvironment { Id = envMesProd, ProjectId = projMes, Name = "PROD", Type = "Üretim", Status = "Active", CreatedAt = createdAt }
        );

        // Yayın (ReleasePlan) seed'i yoktur — yayınlar yalnızca onaylı değişikliklerden
        // runtime'da (POST /api/releases) oluşturulur.
    }

    /// <summary>
    /// Change Management örnek verileri. Risk skorları backend risk kurallarına göre
    /// elle hesaplanıp gömülmüştür (deterministik migrasyon için sabit değerler).
    /// </summary>
    private static void SeedChangeData(ModelBuilder modelBuilder)
    {
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var custAbdi = new Guid("d4444444-4444-4444-4444-444444444401");
        var projEbr = new Guid("e5555555-5555-5555-5555-555555555501");
        var envEbrProd = new Guid("f6666666-6666-6666-6666-666666666604");
        var userRequester = new Guid("b2222222-2222-2222-2222-222222222201");
        var userAdmin = new Guid("b2222222-2222-2222-2222-222222222205");

        var ch1 = new Guid("11110001-0000-0000-0000-000000000001");
        var ch2 = new Guid("11110002-0000-0000-0000-000000000002");
        var ch3 = new Guid("11110003-0000-0000-0000-000000000003");

        modelBuilder.Entity<ChangeRequest>().HasData(
            // 1) Normal Application Deployment — Medium (PROD 30 + AppDeploy 15 = 45)
            new ChangeRequest
            {
                Id = ch1, ChangeNo = "CHG-2026-000001",
                Title = "Operatör arayüzü dağıtımı",
                Description = "Operatör ekranlarında kullanılabilirlik güncellemesi.",
                BusinessReason = "Operatör verimliliğini artırmak için arayüz iyileştirmesi gereklidir.",
                CustomerId = custAbdi, ProjectId = projEbr, EnvironmentId = envEbrProd,
                ChangeClass = "Normal", ChangeType = "ApplicationDeployment",
                Priority = "Medium", Status = "Draft", RiskLevel = "Medium", RiskScore = 45,
                PlannedImplementationDate = new DateTime(2026, 8, 1, 2, 0, 0, DateTimeKind.Utc),
                SourceSystem = "JIRA", SourceReference = "JIRA-2001",
                CreatedByUserId = userRequester, CreatedAt = createdAt
            },
            // 2) Emergency SQL Data Fix — Critical (PROD 30 + Emergency 30 + SqlDataFix 25 + Critical asset 25 = 110)
            new ChangeRequest
            {
                Id = ch2, ChangeNo = "CHG-2026-000002",
                Title = "Üretim veri düzeltmesi",
                Description = "Yanlış işaretlenmiş parti kayıtlarının acil düzeltmesi.",
                BusinessReason = "Yanlış durum bilgisi üretim raporlamasını bozuyor; acil düzeltme gerekli.",
                CustomerId = custAbdi, ProjectId = projEbr, EnvironmentId = envEbrProd,
                ChangeClass = "Emergency", ChangeType = "SqlDataFix",
                Priority = "Critical", Status = "Submitted", RiskLevel = "Critical", RiskScore = 110,
                PlannedImplementationDate = new DateTime(2026, 7, 20, 2, 0, 0, DateTimeKind.Utc),
                PlannedRollbackDate = new DateTime(2026, 7, 20, 4, 0, 0, DateTimeKind.Utc),
                SourceSystem = "JIRA", SourceReference = "JIRA-2002",
                CreatedByUserId = userAdmin, CreatedAt = createdAt
            },
            // 3) Database Schema Change — High (PROD 30 + DbSchema 25 + rollback missing 20 = 75)
            new ChangeRequest
            {
                Id = ch3, ChangeNo = "CHG-2026-000003",
                Title = "Veritabanı şeması güncellemesi",
                Description = "batch_record tablosuna reviewed_by kolonu eklenecek.",
                BusinessReason = "Denetim izlenebilirliği için inceleyen kullanıcı bilgisi tutulmalı.",
                CustomerId = custAbdi, ProjectId = projEbr, EnvironmentId = envEbrProd,
                ChangeClass = "Normal", ChangeType = "DatabaseSchemaChange",
                Priority = "High", Status = "Draft", RiskLevel = "High", RiskScore = 75,
                PlannedImplementationDate = new DateTime(2026, 7, 25, 2, 0, 0, DateTimeKind.Utc),
                SourceSystem = "JIRA", SourceReference = "JIRA-2003",
                CreatedByUserId = userRequester, CreatedAt = createdAt
            }
        );

        modelBuilder.Entity<ChangeRevision>().HasData(
            new ChangeRevision
            {
                Id = new Guid("21110001-0000-0000-0000-000000000001"), ChangeRequestId = ch1, RevisionNo = 1,
                TechnicalSummary = "Angular derlemesi PROD'a dağıtılacak.",
                DeploymentInstructions = "CI paketini PROD ortamına dağıt.",
                RollbackScript = "Önceki artifact sürümüne geri dön (v1.1).",
                RollbackStrategy = "Artifact geri alma", RollbackOwner = "Ali Vural",
                EstimatedDurationMinutes = 30, CreatedByUserId = userRequester, CreatedAt = createdAt
            },
            new ChangeRevision
            {
                Id = new Guid("21110002-0000-0000-0000-000000000002"), ChangeRequestId = ch2, RevisionNo = 1,
                TechnicalSummary = "Parti durum düzeltmesi (UPDATE) uygulanacak.",
                SqlScript = "UPDATE batch_record SET status='REVIEWED' WHERE status='PENDING';",
                RollbackScript = "UPDATE batch_record SET status='PENDING' WHERE status='REVIEWED' AND updated_today=1;",
                RollbackStrategy = "Ters UPDATE + yedekten geri yükleme", RollbackOwner = "System Administrator",
                EstimatedDurationMinutes = 20, CreatedByUserId = userAdmin, CreatedAt = createdAt
            },
            new ChangeRevision
            {
                Id = new Guid("21110003-0000-0000-0000-000000000003"), ChangeRequestId = ch3, RevisionNo = 1,
                TechnicalSummary = "ALTER TABLE ile kolon eklenecek.",
                SqlScript = "ALTER TABLE batch_record ADD reviewed_by NVARCHAR(100) NULL;",
                RollbackScript = "", // eksik rollback → risk +20 (High)
                RollbackStrategy = "", RollbackOwner = "",
                EstimatedDurationMinutes = 45, CreatedByUserId = userRequester, CreatedAt = createdAt
            }
        );

        modelBuilder.Entity<ChangeAffectedAsset>().HasData(
            new ChangeAffectedAsset { Id = new Guid("31110001-0000-0000-0000-000000000001"), ChangeRequestId = ch1, AssetType = "Application", AssetName = "Operatör Arayüzü", Criticality = "High", Description = "Operatör web uygulaması." },
            new ChangeAffectedAsset { Id = new Guid("31110002-0000-0000-0000-000000000002"), ChangeRequestId = ch2, AssetType = "Database", AssetName = "EBR Üretim Veritabanı", Criticality = "Critical", Description = "Birincil üretim veritabanı." },
            new ChangeAffectedAsset { Id = new Guid("31110003-0000-0000-0000-000000000003"), ChangeRequestId = ch3, AssetType = "Table", AssetName = "batch_record Tablosu", Criticality = "High", Description = "Parti kayıt tablosu." }
        );

        modelBuilder.Entity<ChangeDocument>().HasData(
            new ChangeDocument { Id = new Guid("41110001-0000-0000-0000-000000000001"), ChangeRequestId = ch1, DocumentType = "TestEvidence", DocumentName = "UAT Test Kanıtı", Version = "v1", Status = "Active", CreatedAt = createdAt },
            new ChangeDocument { Id = new Guid("41110002-0000-0000-0000-000000000002"), ChangeRequestId = ch2, DocumentType = "SqlScript", DocumentName = "Veri Düzeltme Betiği", Version = "v1", Status = "Active", CreatedAt = createdAt },
            new ChangeDocument { Id = new Guid("41110003-0000-0000-0000-000000000003"), ChangeRequestId = ch3, DocumentType = "SqlScript", DocumentName = "Şema Değişikliği Betiği", Version = "v1", Status = "Active", CreatedAt = createdAt }
        );

        modelBuilder.Entity<ChangeAuditEvent>().HasData(
            new ChangeAuditEvent { Id = new Guid("51110001-0000-0000-0000-000000000001"), ChangeRequestId = ch1, EventType = "ChangeCreated", Description = "Değişiklik oluşturuldu.", ActorUserId = userRequester, CreatedAt = createdAt },
            new ChangeAuditEvent { Id = new Guid("51110002-0000-0000-0000-000000000002"), ChangeRequestId = ch2, EventType = "ChangeCreated", Description = "Değişiklik oluşturuldu.", ActorUserId = userAdmin, CreatedAt = createdAt },
            new ChangeAuditEvent { Id = new Guid("51110003-0000-0000-0000-000000000003"), ChangeRequestId = ch3, EventType = "ChangeCreated", Description = "Değişiklik oluşturuldu.", ActorUserId = userRequester, CreatedAt = createdAt }
        );
    }
}

using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services.Workflow;

/// <summary>
/// Owns the workflow DEFINITION side: creating definitions/draft versions, editing draft graphs,
/// validating and publishing (published versions become immutable), activating a published
/// version, cloning into a new draft, and archiving. All structural validation lives here so a
/// version can never be published in a state the runtime engine cannot safely execute. These are
/// standalone admin operations, so the service saves its own changes.
/// </summary>
public sealed class WorkflowDefinitionService
{
    private readonly GmsDbContext _db;
    private readonly ICurrentUser _currentUser;

    public WorkflowDefinitionService(GmsDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Creates a new definition (Draft) with its first Draft version graph. Saves.</summary>
    public async Task<WorkflowDefinition> CreateAsync(CreateWorkflowDefinitionDto dto, CancellationToken ct = default)
    {
        var code = (dto.Code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code)) throw new AuthValidationException("Workflow kodu zorunludur.");
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new AuthValidationException("Workflow adı zorunludur.");
        if (await _db.WorkflowDefinitions.AnyAsync(d => d.Code == code, ct))
            throw new AuthValidationException($"'{code}' kodlu bir workflow tanımı zaten mevcut.");
        if (!string.IsNullOrWhiteSpace(dto.ChangeClass) && !ChangeClasses.All.Contains(dto.ChangeClass))
            throw new AuthValidationException("Geçersiz değişiklik sınıfı (ChangeClass).");

        var now = DateTime.UtcNow;
        var actor = _currentUser.RequireUserId();
        var defId = Guid.NewGuid();
        var verId = Guid.NewGuid();

        var definition = new WorkflowDefinition
        {
            Id = defId, Code = code, Name = dto.Name.Trim(), Description = dto.Description?.Trim() ?? string.Empty,
            Category = string.IsNullOrWhiteSpace(dto.Category) ? WorkflowCategories.ChangeManagement : dto.Category.Trim(),
            TriggerObjectType = string.IsNullOrWhiteSpace(dto.TriggerObjectType) ? WorkflowTriggers.ChangeRequestObject : dto.TriggerObjectType.Trim(),
            TriggerEvent = string.IsNullOrWhiteSpace(dto.TriggerEvent) ? WorkflowTriggers.ChangeSubmittedEvent : dto.TriggerEvent.Trim(),
            ChangeClass = string.IsNullOrWhiteSpace(dto.ChangeClass) ? null : dto.ChangeClass,
            Status = WorkflowDefinitionStatuses.Draft, IsSystem = false,
            CreatedByUserId = actor, CreatedAt = now
        };

        var version = new WorkflowVersion
        {
            Id = verId, WorkflowDefinitionId = defId, VersionNumber = 1,
            Status = WorkflowVersionStatuses.Draft, CreatedByUserId = actor, CreatedAt = now
        };
        ApplyGraph(version, dto.Steps, dto.Transitions);
        definition.Versions.Add(version);

        _db.WorkflowDefinitions.Add(definition);
        await _db.SaveChangesAsync(ct);
        return definition;
    }

    /// <summary>Replaces the step/transition graph of a Draft version. Published versions are immutable. Saves.</summary>
    public async Task<WorkflowVersion> UpdateDraftVersionAsync(Guid versionId, UpdateWorkflowVersionDto dto, CancellationToken ct = default)
    {
        var version = await LoadVersionGraphAsync(versionId, ct);
        if (version.Status != WorkflowVersionStatuses.Draft)
            throw new AuthValidationException("Yalnızca taslak (Draft) sürümler düzenlenebilir; yayınlanmış sürümler değişmezdir.");

        _db.WorkflowStepDefinitions.RemoveRange(version.Steps);
        _db.WorkflowTransitionDefinitions.RemoveRange(version.Transitions);
        version.Steps.Clear();
        version.Transitions.Clear();
        version.Notes = dto.Notes?.Trim();
        ApplyGraph(version, dto.Steps, dto.Transitions);

        await _db.SaveChangesAsync(ct);
        return version;
    }

    /// <summary>Validates a version's graph without changing anything (publish gate preview).</summary>
    public async Task<WorkflowValidationResultDto> ValidateVersionAsync(Guid versionId, CancellationToken ct = default)
    {
        var version = await LoadVersionGraphAsync(versionId, ct);
        return ValidateGraph(version.Steps, version.Transitions);
    }

    /// <summary>Validates then publishes a Draft version (immutable afterwards). Saves.</summary>
    public async Task<WorkflowVersion> PublishAsync(Guid versionId, CancellationToken ct = default)
    {
        var version = await LoadVersionGraphAsync(versionId, ct);
        if (version.Status == WorkflowVersionStatuses.Published)
            throw new AuthValidationException("Sürüm zaten yayınlanmış.");
        if (version.Status == WorkflowVersionStatuses.Retired)
            throw new AuthValidationException("Emekliye ayrılmış sürüm yayınlanamaz.");

        var result = ValidateGraph(version.Steps, version.Transitions);
        if (!result.IsValid)
            throw new AuthValidationException("Workflow doğrulaması başarısız: " + string.Join(" | ", result.Errors));

        var now = DateTime.UtcNow;
        var actor = _currentUser.RequireUserId();
        version.Status = WorkflowVersionStatuses.Published;
        version.StartStepKey = version.Steps.First(s => s.StepType == WorkflowStepTypes.Start).StepKey;
        version.PublishedAt = now;
        version.PublishedByUserId = actor;

        await _db.SaveChangesAsync(ct);
        return version;
    }

    /// <summary>Activates a Published version as the definition's live version. Saves.</summary>
    public async Task<WorkflowDefinition> ActivateAsync(Guid definitionId, Guid versionId, CancellationToken ct = default)
    {
        var definition = await _db.WorkflowDefinitions.Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == definitionId, ct)
            ?? throw new KeyNotFoundException("Workflow tanımı bulunamadı.");
        var version = definition.Versions.FirstOrDefault(v => v.Id == versionId)
            ?? throw new KeyNotFoundException("Sürüm bu tanıma ait değil veya bulunamadı.");
        if (version.Status != WorkflowVersionStatuses.Published)
            throw new AuthValidationException("Yalnızca yayınlanmış (Published) sürüm aktifleştirilebilir.");

        var now = DateTime.UtcNow;
        definition.ActiveVersionId = version.Id;
        if (definition.Status == WorkflowDefinitionStatuses.Draft || definition.Status == WorkflowDefinitionStatuses.Inactive)
            definition.TransitionTo(WorkflowDefinitionStatuses.Active);
        definition.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);
        return definition;
    }

    /// <summary>Clones the definition's latest version into a new editable Draft version. Saves.</summary>
    public async Task<WorkflowVersion> CloneLatestAsync(Guid definitionId, CancellationToken ct = default)
    {
        var definition = await _db.WorkflowDefinitions.Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == definitionId, ct)
            ?? throw new KeyNotFoundException("Workflow tanımı bulunamadı.");
        if (definition.Status == WorkflowDefinitionStatuses.Archived)
            throw new AuthValidationException("Arşivlenmiş tanım için yeni sürüm oluşturulamaz.");

        var source = definition.Versions.OrderByDescending(v => v.VersionNumber).First();
        var sourceGraph = await LoadVersionGraphAsync(source.Id, ct);

        var now = DateTime.UtcNow;
        var actor = _currentUser.RequireUserId();
        var nextNumber = definition.Versions.Max(v => v.VersionNumber) + 1;

        var clone = new WorkflowVersion
        {
            Id = Guid.NewGuid(), WorkflowDefinitionId = definition.Id, VersionNumber = nextNumber,
            Status = WorkflowVersionStatuses.Draft, Notes = $"v{source.VersionNumber} sürümünden kopyalandı.",
            CreatedByUserId = actor, CreatedAt = now
        };

        foreach (var s in sourceGraph.Steps.OrderBy(s => s.StepOrder))
            clone.Steps.Add(new WorkflowStepDefinition
            {
                Id = Guid.NewGuid(), WorkflowVersionId = clone.Id, StepKey = s.StepKey, Name = s.Name,
                StepType = s.StepType, StepOrder = s.StepOrder, AssignedRole = s.AssignedRole,
                AssignedUserId = s.AssignedUserId, IsRequired = s.IsRequired, DueInHours = s.DueInHours,
                NotificationTemplateCode = s.NotificationTemplateCode, NotificationRole = s.NotificationRole,
                Description = s.Description
            });
        foreach (var t in sourceGraph.Transitions)
            clone.Transitions.Add(new WorkflowTransitionDefinition
            {
                Id = Guid.NewGuid(), WorkflowVersionId = clone.Id, FromStepKey = t.FromStepKey, ToStepKey = t.ToStepKey,
                ConditionType = t.ConditionType, Priority = t.Priority, ConditionField = t.ConditionField,
                Operator = t.Operator, ExpectedValue = t.ExpectedValue, Description = t.Description
            });

        _db.WorkflowVersions.Add(clone);
        await _db.SaveChangesAsync(ct);
        return clone;
    }

    /// <summary>Archives a definition (terminal). System definitions cannot be archived. Saves.</summary>
    public async Task<WorkflowDefinition> ArchiveAsync(Guid definitionId, CancellationToken ct = default)
    {
        var definition = await _db.WorkflowDefinitions.FirstOrDefaultAsync(d => d.Id == definitionId, ct)
            ?? throw new KeyNotFoundException("Workflow tanımı bulunamadı.");
        if (definition.IsSystem)
            throw new AuthValidationException("Sistem tarafından tanımlanan varsayılan workflow arşivlenemez.");

        var hasActive = await _db.WorkflowInstances.AnyAsync(i => i.WorkflowDefinitionId == definitionId
            && (i.Status == WorkflowInstanceStatuses.Running || i.Status == WorkflowInstanceStatuses.Waiting
                || i.Status == WorkflowInstanceStatuses.Created), ct);
        if (hasActive)
            throw new AuthValidationException("Devam eden örnekleri olan workflow arşivlenemez.");

        definition.TransitionTo(WorkflowDefinitionStatuses.Archived);
        definition.ActiveVersionId = null;
        definition.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return definition;
    }

    /* ── validation ─────────────────────────────────────── */

    /// <summary>
    /// Structural validation applied before publish. Guarantees the runtime engine can execute
    /// the graph: exactly one Start, ≥1 End, unique keys, valid types, manual steps assigned,
    /// notification steps templated, every transition endpoint exists, conditions use allowlisted
    /// fields/operators, every non-End step has an outgoing edge, and all steps reachable from Start.
    /// </summary>
    public static WorkflowValidationResultDto ValidateGraph(
        ICollection<WorkflowStepDefinition> steps, ICollection<WorkflowTransitionDefinition> transitions)
    {
        var result = new WorkflowValidationResultDto();

        if (steps.Count == 0)
        {
            result.Errors.Add("Workflow en az bir adım içermelidir.");
            return result;
        }

        // Unique keys
        var dupKeys = steps.GroupBy(s => s.StepKey, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        foreach (var k in dupKeys) result.Errors.Add($"Yinelenen adım anahtarı: '{k}'.");

        // Valid step types
        foreach (var s in steps.Where(s => !WorkflowStepTypes.All.Contains(s.StepType)))
            result.Errors.Add($"Geçersiz adım türü '{s.StepType}' (adım {s.StepKey}).");

        // Exactly one Start, at least one End
        var startCount = steps.Count(s => s.StepType == WorkflowStepTypes.Start);
        if (startCount != 1) result.Errors.Add($"Tam olarak bir Start adımı olmalıdır (bulunan: {startCount}).");
        if (steps.All(s => s.StepType != WorkflowStepTypes.End))
            result.Errors.Add("En az bir End adımı olmalıdır.");

        // Manual steps must be assigned; notification steps must have a template
        foreach (var s in steps)
        {
            if (WorkflowStepTypes.Manual.Contains(s.StepType)
                && string.IsNullOrWhiteSpace(s.AssignedRole) && s.AssignedUserId is null)
                result.Errors.Add($"Manuel adım '{s.StepKey}' bir rol veya kullanıcıya atanmalıdır.");

            if (s.StepType == WorkflowStepTypes.Notification && string.IsNullOrWhiteSpace(s.NotificationTemplateCode))
                result.Errors.Add($"Bildirim adımı '{s.StepKey}' için şablon kodu zorunludur.");

            if (!string.IsNullOrWhiteSpace(s.AssignedRole) && !SystemRoles.All.Contains(s.AssignedRole))
                result.Warnings.Add($"Adım '{s.StepKey}' bilinmeyen bir role atanmış: '{s.AssignedRole}'.");
        }

        var keys = new HashSet<string>(steps.Select(s => s.StepKey), StringComparer.OrdinalIgnoreCase);

        // Transition endpoints exist + condition validity
        foreach (var t in transitions)
        {
            if (!keys.Contains(t.FromStepKey)) result.Errors.Add($"Geçiş kaynağı bulunamadı: '{t.FromStepKey}'.");
            if (!keys.Contains(t.ToStepKey)) result.Errors.Add($"Geçiş hedefi bulunamadı: '{t.ToStepKey}'.");
            if (!WorkflowConditionTypes.All.Contains(t.ConditionType))
                result.Errors.Add($"Geçersiz koşul türü '{t.ConditionType}' ({t.FromStepKey}→{t.ToStepKey}).");

            if (t.ConditionType != WorkflowConditionTypes.Always)
            {
                if (string.IsNullOrWhiteSpace(t.ConditionField) || !WorkflowChangeFields.All.Contains(t.ConditionField))
                    result.Errors.Add($"Koşul alanı izinli listede değil: '{t.ConditionField}' ({t.FromStepKey}→{t.ToStepKey}).");
                if (string.IsNullOrWhiteSpace(t.Operator) || !WorkflowOperators.All.Contains(t.Operator))
                    result.Errors.Add($"Geçersiz operatör '{t.Operator}' ({t.FromStepKey}→{t.ToStepKey}).");
                if (string.IsNullOrWhiteSpace(t.ExpectedValue))
                    result.Errors.Add($"Koşul için beklenen değer zorunludur ({t.FromStepKey}→{t.ToStepKey}).");

                // Ordering operators only valid on numeric fields.
                var ordering = new[] { WorkflowOperators.GreaterThan, WorkflowOperators.GreaterThanOrEqual,
                    WorkflowOperators.LessThan, WorkflowOperators.LessThanOrEqual };
                if (t.ConditionField is not null && ordering.Contains(t.Operator) && !WorkflowChangeFields.Numeric.Contains(t.ConditionField))
                    result.Errors.Add($"Sıralama operatörü yalnızca sayısal alanlarda kullanılabilir ('{t.ConditionField}').");
            }
        }

        // Every non-End step must have an outgoing transition; End steps must not.
        var outByStep = transitions.GroupBy(t => t.FromStepKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        foreach (var s in steps)
        {
            var hasOut = outByStep.ContainsKey(s.StepKey);
            if (s.StepType == WorkflowStepTypes.End && hasOut)
                result.Errors.Add($"End adımı '{s.StepKey}' çıkış geçişine sahip olamaz.");
            if (s.StepType != WorkflowStepTypes.End && !hasOut)
                result.Errors.Add($"'{s.StepKey}' adımının en az bir çıkış geçişi olmalıdır.");

            // A Condition step should have an Always fallback so routing can't dead-end.
            if (s.StepType == WorkflowStepTypes.Condition && hasOut
                && outByStep[s.StepKey].All(t => t.ConditionType != WorkflowConditionTypes.Always))
                result.Warnings.Add($"Koşul adımı '{s.StepKey}' için bir 'Always' yedek geçişi önerilir.");
        }

        // Reachability from Start (only if a single Start exists).
        if (startCount == 1 && dupKeys.Count == 0)
        {
            var start = steps.First(s => s.StepType == WorkflowStepTypes.Start).StepKey;
            var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { start };
            var queue = new Queue<string>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (!outByStep.TryGetValue(cur, out var outs)) continue;
                foreach (var t in outs)
                    if (keys.Contains(t.ToStepKey) && reachable.Add(t.ToStepKey))
                        queue.Enqueue(t.ToStepKey);
            }
            foreach (var s in steps.Where(s => !reachable.Contains(s.StepKey)))
                result.Errors.Add($"'{s.StepKey}' adımına Start'tan ulaşılamıyor.");
        }

        return result;
    }

    /* ── helpers ────────────────────────────────────────── */

    private static void ApplyGraph(WorkflowVersion version,
        IEnumerable<CreateWorkflowStepDto> steps, IEnumerable<CreateWorkflowTransitionDto> transitions)
    {
        foreach (var s in steps)
            version.Steps.Add(new WorkflowStepDefinition
            {
                Id = Guid.NewGuid(), WorkflowVersionId = version.Id,
                StepKey = (s.StepKey ?? string.Empty).Trim(), Name = (s.Name ?? string.Empty).Trim(),
                StepType = s.StepType, StepOrder = s.StepOrder, AssignedRole = s.AssignedRole,
                AssignedUserId = s.AssignedUserId, IsRequired = s.IsRequired, DueInHours = s.DueInHours,
                NotificationTemplateCode = s.NotificationTemplateCode, NotificationRole = s.NotificationRole,
                Description = s.Description
            });

        foreach (var t in transitions)
            version.Transitions.Add(new WorkflowTransitionDefinition
            {
                Id = Guid.NewGuid(), WorkflowVersionId = version.Id,
                FromStepKey = (t.FromStepKey ?? string.Empty).Trim(), ToStepKey = (t.ToStepKey ?? string.Empty).Trim(),
                ConditionType = string.IsNullOrWhiteSpace(t.ConditionType) ? WorkflowConditionTypes.Always : t.ConditionType,
                Priority = t.Priority, ConditionField = t.ConditionField, Operator = t.Operator,
                ExpectedValue = t.ExpectedValue, Description = t.Description
            });
    }

    private async Task<WorkflowVersion> LoadVersionGraphAsync(Guid versionId, CancellationToken ct)
    {
        return await _db.WorkflowVersions
            .Include(v => v.Steps)
            .Include(v => v.Transitions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.Id == versionId, ct)
            ?? throw new KeyNotFoundException("Workflow sürümü bulunamadı.");
    }
}

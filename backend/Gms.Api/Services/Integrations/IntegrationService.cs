using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Gms.Api.Services.Integrations.Providers;
using Gms.Api.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gms.Api.Services.Integrations;

/// <summary>Result of handling an incoming webhook (the controller returns StatusCode).</summary>
public sealed record IncomingWebhookResult(int StatusCode, string Message, Guid? ExecutionId);

/// <summary>
/// Core Integration Hub service: definition/credential/endpoint/subscription lifecycle, connection
/// testing, external object links and incoming webhook handling. Secrets are protected on write and
/// never returned; decryption happens only inside provider calls. Standalone admin operations save
/// their own changes. Controllers stay thin.
/// </summary>
public interface IIntegrationService
{
    Task<IntegrationDefinition> CreateAsync(CreateIntegrationDto dto, CancellationToken ct = default);
    Task<IntegrationDefinition> UpdateAsync(Guid id, UpdateIntegrationDto dto, CancellationToken ct = default);
    Task<IntegrationDefinition> ActivateAsync(Guid id, CancellationToken ct = default);
    Task<IntegrationDefinition> DeactivateAsync(Guid id, CancellationToken ct = default);
    Task<ConnectionTestResultDto> TestConnectionAsync(Guid id, CancellationToken ct = default);

    Task<IntegrationCredential> AddCredentialAsync(Guid id, CreateCredentialDto dto, CancellationToken ct = default);
    Task<IntegrationCredential> RotateCredentialAsync(Guid id, Guid credentialId, UpdateCredentialDto dto, CancellationToken ct = default);
    Task DeleteCredentialAsync(Guid id, Guid credentialId, CancellationToken ct = default);

    Task<IntegrationEndpoint> AddEndpointAsync(Guid id, CreateEndpointDto dto, CancellationToken ct = default);
    Task<IntegrationEndpoint> UpdateEndpointAsync(Guid id, Guid endpointId, UpdateEndpointDto dto, CancellationToken ct = default);
    Task DeleteEndpointAsync(Guid id, Guid endpointId, CancellationToken ct = default);

    Task<IntegrationSubscription> AddSubscriptionAsync(Guid id, CreateSubscriptionDto dto, CancellationToken ct = default);
    Task<IntegrationSubscription> UpdateSubscriptionAsync(Guid id, Guid subscriptionId, UpdateSubscriptionDto dto, CancellationToken ct = default);
    Task DeleteSubscriptionAsync(Guid id, Guid subscriptionId, CancellationToken ct = default);

    Task<ExternalObjectLink> CreateLinkAsync(Guid id, CreateExternalLinkDto dto, CancellationToken ct = default);
    Task RemoveLinkAsync(Guid id, Guid linkId, CancellationToken ct = default);

    Task CancelExecutionAsync(Guid executionId, CancellationToken ct = default);
    Task<IncomingWebhookResult> HandleIncomingWebhookAsync(string integrationCode, string body, string? contentType,
        IReadOnlyDictionary<string, string> headers, CancellationToken ct = default);
}

public sealed class IntegrationService : IIntegrationService
{
    private readonly GmsDbContext _db;
    private readonly IIntegrationProviderResolver _resolver;
    private readonly ISecretProtector _secrets;
    private readonly SequentialNumberGenerator _numbers;
    private readonly NotificationService _notifications;
    private readonly ICurrentUser _currentUser;
    private readonly IntegrationOptions _options;

    public IntegrationService(GmsDbContext db, IIntegrationProviderResolver resolver, ISecretProtector secrets,
        SequentialNumberGenerator numbers, NotificationService notifications, ICurrentUser currentUser,
        IOptions<IntegrationOptions> options)
    {
        _db = db;
        _resolver = resolver;
        _secrets = secrets;
        _numbers = numbers;
        _notifications = notifications;
        _currentUser = currentUser;
        _options = options.Value;
    }

    /* ── definition ─────────────────────────────────────── */

    public async Task<IntegrationDefinition> CreateAsync(CreateIntegrationDto dto, CancellationToken ct = default)
    {
        var code = (dto.Code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code)) throw new IntegrationValidationException("Entegrasyon kodu zorunludur.");
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new IntegrationValidationException("Entegrasyon adı zorunludur.");
        if (!IntegrationProviders.All.Contains(dto.Provider)) throw new IntegrationValidationException("Geçersiz sağlayıcı.");
        if (!IntegrationProviders.Implemented.Contains(dto.Provider))
            throw new IntegrationValidationException($"'{dto.Provider}' sağlayıcısı bu sürümde henüz desteklenmiyor.");
        var category = string.IsNullOrWhiteSpace(dto.Category) ? IntegrationCategories.Generic : dto.Category;
        if (!IntegrationCategories.All.Contains(category)) throw new IntegrationValidationException("Geçersiz kategori.");
        var auth = string.IsNullOrWhiteSpace(dto.AuthenticationType) ? IntegrationAuthTypes.None : dto.AuthenticationType;
        if (!IntegrationAuthTypes.All.Contains(auth)) throw new IntegrationValidationException("Geçersiz kimlik doğrulama türü.");
        if (await _db.IntegrationDefinitions.AnyAsync(d => d.Code == code, ct))
            throw new IntegrationValidationException($"'{code}' kodlu bir entegrasyon zaten mevcut.");

        var now = DateTime.UtcNow;
        var actor = _currentUser.RequireUserId();
        var id = Guid.NewGuid();
        var no = await _numbers.NextAsync($"INT-{now.Year}-", _db.IntegrationDefinitions.Select(d => d.IntegrationNo), ct);

        var def = new IntegrationDefinition
        {
            Id = id, IntegrationNo = no, Code = code, Name = dto.Name.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty, Provider = dto.Provider, Category = category,
            Status = IntegrationStatuses.Draft, BaseUrl = string.IsNullOrWhiteSpace(dto.BaseUrl) ? null : dto.BaseUrl.Trim(),
            AuthenticationType = auth, IsSystem = false, CreatedByUserId = actor, CreatedAt = now
        };
        def.Events.Add(AuditFactory.Integration(IntegrationEventTypes.IntegrationCreated,
            $"Entegrasyon oluşturuldu ({no}, {dto.Provider}).", id, actor, now));

        _db.IntegrationDefinitions.Add(def);
        await _db.SaveChangesAsync(ct);
        return def;
    }

    public async Task<IntegrationDefinition> UpdateAsync(Guid id, UpdateIntegrationDto dto, CancellationToken ct = default)
    {
        var def = await LoadAsync(id, ct);
        if (!string.IsNullOrWhiteSpace(dto.RowVersion))
            _db.Entry(def).Property(d => d.RowVersion).OriginalValue = Convert.FromBase64String(dto.RowVersion);

        if (dto.Category is not null)
        {
            if (!IntegrationCategories.All.Contains(dto.Category)) throw new IntegrationValidationException("Geçersiz kategori.");
            def.Category = dto.Category;
        }
        if (dto.AuthenticationType is not null)
        {
            if (!IntegrationAuthTypes.All.Contains(dto.AuthenticationType)) throw new IntegrationValidationException("Geçersiz kimlik doğrulama türü.");
            def.AuthenticationType = dto.AuthenticationType;
        }
        if (!string.IsNullOrWhiteSpace(dto.Name)) def.Name = dto.Name.Trim();
        if (dto.Description is not null) def.Description = dto.Description.Trim();
        if (dto.BaseUrl is not null) def.BaseUrl = string.IsNullOrWhiteSpace(dto.BaseUrl) ? null : dto.BaseUrl.Trim();
        def.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return def;
    }

    public async Task<IntegrationDefinition> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var def = await LoadAsync(id, ct, includeCredentials: true);
        var provider = _resolver.Resolve(def.Provider);

        var config = provider.ValidateConfiguration(def);
        if (!config.IsValid)
            throw new IntegrationValidationException("Yapılandırma geçersiz: " + string.Join(" | ", config.Errors));
        ValidateRequiredCredentials(def);

        var now = DateTime.UtcNow;
        var actor = _currentUser.RequireUserId();
        def.TransitionTo(IntegrationStatuses.Active);
        def.UpdatedAt = now;
        def.Events.Add(AuditFactory.Integration(IntegrationEventTypes.IntegrationActivated, "Entegrasyon aktifleştirildi.", id, actor, now));
        await _db.SaveChangesAsync(ct);
        return def;
    }

    public async Task<IntegrationDefinition> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var def = await LoadAsync(id, ct);
        var now = DateTime.UtcNow;
        var actor = _currentUser.RequireUserId();
        def.TransitionTo(IntegrationStatuses.Inactive);
        def.UpdatedAt = now;
        def.Events.Add(AuditFactory.Integration(IntegrationEventTypes.IntegrationDeactivated, "Entegrasyon pasifleştirildi.", id, actor, now));
        await _db.SaveChangesAsync(ct);
        return def;
    }

    public async Task<ConnectionTestResultDto> TestConnectionAsync(Guid id, CancellationToken ct = default)
    {
        var def = await LoadAsync(id, ct, includeCredentials: true);
        var provider = _resolver.Resolve(def.Provider);
        var now = DateTime.UtcNow;
        var actor = _currentUser.UserId;
        def.Events.Add(AuditFactory.Integration(IntegrationEventTypes.ConnectionTestStarted, "Bağlantı testi başlatıldı.", id, actor, now));

        var creds = DecryptCredentials(def);
        var result = await provider.TestConnectionAsync(def, creds, ct);
        var when = DateTime.UtcNow;

        if (result.Success)
        {
            def.LastSuccessfulConnectionAt = when;
            def.Events.Add(AuditFactory.Integration(IntegrationEventTypes.ConnectionTestSucceeded,
                $"Bağlantı testi başarılı ({result.HttpStatusCode}).", id, actor, when));
        }
        else
        {
            def.LastFailedConnectionAt = when;
            def.Events.Add(AuditFactory.Integration(IntegrationEventTypes.ConnectionTestFailed,
                $"Bağlantı testi başarısız: {result.Message}", id, actor, when));
            await _notifications.NotifyRoleAsync(SystemRoles.Admin, NotificationTemplates.IntegrationConnectionFailed,
                NotificationSeverities.Error, new Dictionary<string, string>
                {
                    ["IntegrationNo"] = def.IntegrationNo, ["IntegrationName"] = def.Name, ["Error"] = result.Message
                }, actor, ct);
        }

        await _db.SaveChangesAsync(ct);
        return new ConnectionTestResultDto
        {
            Success = result.Success, HttpStatusCode = result.HttpStatusCode,
            Message = result.Message, DurationMilliseconds = result.DurationMilliseconds
        };
    }

    /* ── credentials ────────────────────────────────────── */

    public async Task<IntegrationCredential> AddCredentialAsync(Guid id, CreateCredentialDto dto, CancellationToken ct = default)
    {
        var def = await LoadAsync(id, ct, includeCredentials: true);
        var keyName = (dto.KeyName ?? string.Empty).Trim();
        if (!IntegrationCredentialKeys.All.Contains(keyName)) throw new IntegrationValidationException("Geçersiz kimlik bilgisi anahtarı.");
        if (string.IsNullOrEmpty(dto.Value)) throw new IntegrationValidationException("Kimlik bilgisi değeri boş olamaz.");
        if (def.Credentials.Any(c => c.KeyName == keyName))
            throw new IntegrationValidationException($"'{keyName}' anahtarı zaten mevcut (döndürme kullanın).");

        var now = DateTime.UtcNow;
        var actor = _currentUser.RequireUserId();
        var credential = new IntegrationCredential
        {
            Id = Guid.NewGuid(), IntegrationDefinitionId = id,
            CredentialType = string.IsNullOrWhiteSpace(dto.CredentialType) ? keyName : dto.CredentialType!,
            KeyName = keyName, EncryptedValue = _secrets.Protect(dto.Value), MaskedValue = _secrets.Mask(dto.Value),
            CreatedAt = now
        };
        _db.IntegrationCredentials.Add(credential);
        def.Events.Add(AuditFactory.Integration(IntegrationEventTypes.CredentialAdded, $"Kimlik bilgisi eklendi: {keyName}.", id, actor, now));
        await _db.SaveChangesAsync(ct);
        return credential;
    }

    public async Task<IntegrationCredential> RotateCredentialAsync(Guid id, Guid credentialId, UpdateCredentialDto dto, CancellationToken ct = default)
    {
        var def = await LoadAsync(id, ct);
        var credential = await _db.IntegrationCredentials.FirstOrDefaultAsync(c => c.Id == credentialId && c.IntegrationDefinitionId == id, ct)
            ?? throw new KeyNotFoundException("Kimlik bilgisi bulunamadı.");
        if (string.IsNullOrEmpty(dto.Value)) throw new IntegrationValidationException("Yeni değer boş olamaz.");

        var now = DateTime.UtcNow;
        var actor = _currentUser.RequireUserId();
        credential.EncryptedValue = _secrets.Protect(dto.Value);
        credential.MaskedValue = _secrets.Mask(dto.Value);
        credential.RotatedAt = now;
        credential.UpdatedAt = now;
        def.Events.Add(AuditFactory.Integration(IntegrationEventTypes.CredentialRotated, $"Kimlik bilgisi döndürüldü: {credential.KeyName}.", id, actor, now));

        await _notifications.NotifyRoleAsync(SystemRoles.Admin, NotificationTemplates.IntegrationCredentialRotated,
            NotificationSeverities.Warning, new Dictionary<string, string>
            {
                ["IntegrationNo"] = def.IntegrationNo, ["IntegrationName"] = def.Name, ["KeyName"] = credential.KeyName
            }, actor, ct);

        await _db.SaveChangesAsync(ct);
        return credential;
    }

    public async Task DeleteCredentialAsync(Guid id, Guid credentialId, CancellationToken ct = default)
    {
        var credential = await _db.IntegrationCredentials.FirstOrDefaultAsync(c => c.Id == credentialId && c.IntegrationDefinitionId == id, ct)
            ?? throw new KeyNotFoundException("Kimlik bilgisi bulunamadı.");
        _db.IntegrationCredentials.Remove(credential);
        await _db.SaveChangesAsync(ct);
    }

    /* ── endpoints ──────────────────────────────────────── */

    public async Task<IntegrationEndpoint> AddEndpointAsync(Guid id, CreateEndpointDto dto, CancellationToken ct = default)
    {
        await EnsureExistsAsync(id, ct);
        if (!IntegrationDirections.All.Contains(dto.Direction)) throw new IntegrationValidationException("Geçersiz yön (Direction).");
        var method = string.IsNullOrWhiteSpace(dto.HttpMethod) ? "POST" : dto.HttpMethod.ToUpperInvariant();
        if (!IntegrationHttpMethods.All.Contains(method)) throw new IntegrationValidationException("Geçersiz HTTP metodu.");

        var endpoint = new IntegrationEndpoint
        {
            Id = Guid.NewGuid(), IntegrationDefinitionId = id, Name = dto.Name.Trim(), Direction = dto.Direction,
            RelativePath = dto.RelativePath?.Trim() ?? string.Empty, HttpMethod = method,
            TimeoutSeconds = Math.Clamp(dto.TimeoutSeconds, 1, 120), IsActive = dto.IsActive, CreatedAt = DateTime.UtcNow
        };
        _db.IntegrationEndpoints.Add(endpoint);
        await _db.SaveChangesAsync(ct);
        return endpoint;
    }

    public async Task<IntegrationEndpoint> UpdateEndpointAsync(Guid id, Guid endpointId, UpdateEndpointDto dto, CancellationToken ct = default)
    {
        var endpoint = await _db.IntegrationEndpoints.FirstOrDefaultAsync(e => e.Id == endpointId && e.IntegrationDefinitionId == id, ct)
            ?? throw new KeyNotFoundException("Uç nokta bulunamadı.");
        if (dto.HttpMethod is not null)
        {
            var method = dto.HttpMethod.ToUpperInvariant();
            if (!IntegrationHttpMethods.All.Contains(method)) throw new IntegrationValidationException("Geçersiz HTTP metodu.");
            endpoint.HttpMethod = method;
        }
        if (!string.IsNullOrWhiteSpace(dto.Name)) endpoint.Name = dto.Name.Trim();
        if (dto.RelativePath is not null) endpoint.RelativePath = dto.RelativePath.Trim();
        if (dto.TimeoutSeconds.HasValue) endpoint.TimeoutSeconds = Math.Clamp(dto.TimeoutSeconds.Value, 1, 120);
        if (dto.IsActive.HasValue) endpoint.IsActive = dto.IsActive.Value;
        endpoint.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return endpoint;
    }

    public async Task DeleteEndpointAsync(Guid id, Guid endpointId, CancellationToken ct = default)
    {
        var endpoint = await _db.IntegrationEndpoints.FirstOrDefaultAsync(e => e.Id == endpointId && e.IntegrationDefinitionId == id, ct)
            ?? throw new KeyNotFoundException("Uç nokta bulunamadı.");
        _db.IntegrationEndpoints.Remove(endpoint);
        await _db.SaveChangesAsync(ct);
    }

    /* ── subscriptions ──────────────────────────────────── */

    public async Task<IntegrationSubscription> AddSubscriptionAsync(Guid id, CreateSubscriptionDto dto, CancellationToken ct = default)
    {
        await EnsureExistsAsync(id, ct);
        if (!IntegrationSubscriptionEvents.All.Contains(dto.EventType)) throw new IntegrationValidationException("Desteklenmeyen olay türü.");
        var endpoint = await _db.IntegrationEndpoints.FirstOrDefaultAsync(e => e.Id == dto.TargetEndpointId && e.IntegrationDefinitionId == id, ct)
            ?? throw new IntegrationValidationException("Hedef uç nokta bu entegrasyona ait değil.");
        if (endpoint.Direction != IntegrationDirections.Outgoing) throw new IntegrationValidationException("Hedef uç nokta 'Outgoing' olmalıdır.");

        var sub = new IntegrationSubscription
        {
            Id = Guid.NewGuid(), IntegrationDefinitionId = id, EventType = dto.EventType,
            ObjectType = dto.ObjectType, TargetEndpointId = dto.TargetEndpointId, IsActive = dto.IsActive, CreatedAt = DateTime.UtcNow
        };
        _db.IntegrationSubscriptions.Add(sub);
        await _db.SaveChangesAsync(ct);
        return sub;
    }

    public async Task<IntegrationSubscription> UpdateSubscriptionAsync(Guid id, Guid subscriptionId, UpdateSubscriptionDto dto, CancellationToken ct = default)
    {
        var sub = await _db.IntegrationSubscriptions.FirstOrDefaultAsync(s => s.Id == subscriptionId && s.IntegrationDefinitionId == id, ct)
            ?? throw new KeyNotFoundException("Abonelik bulunamadı.");
        if (dto.EventType is not null)
        {
            if (!IntegrationSubscriptionEvents.All.Contains(dto.EventType)) throw new IntegrationValidationException("Desteklenmeyen olay türü.");
            sub.EventType = dto.EventType;
        }
        if (dto.TargetEndpointId.HasValue)
        {
            var endpoint = await _db.IntegrationEndpoints.FirstOrDefaultAsync(e => e.Id == dto.TargetEndpointId.Value && e.IntegrationDefinitionId == id, ct)
                ?? throw new IntegrationValidationException("Hedef uç nokta bulunamadı.");
            if (endpoint.Direction != IntegrationDirections.Outgoing) throw new IntegrationValidationException("Hedef uç nokta 'Outgoing' olmalıdır.");
            sub.TargetEndpointId = dto.TargetEndpointId.Value;
        }
        if (dto.ObjectType is not null) sub.ObjectType = dto.ObjectType;
        if (dto.IsActive.HasValue) sub.IsActive = dto.IsActive.Value;
        sub.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return sub;
    }

    public async Task DeleteSubscriptionAsync(Guid id, Guid subscriptionId, CancellationToken ct = default)
    {
        var sub = await _db.IntegrationSubscriptions.FirstOrDefaultAsync(s => s.Id == subscriptionId && s.IntegrationDefinitionId == id, ct)
            ?? throw new KeyNotFoundException("Abonelik bulunamadı.");
        _db.IntegrationSubscriptions.Remove(sub);
        await _db.SaveChangesAsync(ct);
    }

    /* ── external links ─────────────────────────────────── */

    public async Task<ExternalObjectLink> CreateLinkAsync(Guid id, CreateExternalLinkDto dto, CancellationToken ct = default)
    {
        var def = await LoadAsync(id, ct);
        if (string.IsNullOrWhiteSpace(dto.InternalObjectType)) throw new IntegrationValidationException("Dahili nesne türü zorunludur.");
        if (dto.InternalObjectId == Guid.Empty) throw new IntegrationValidationException("Dahili nesne kimliği zorunludur.");
        if (string.IsNullOrWhiteSpace(dto.ExternalReference)) throw new IntegrationValidationException("Dış referans zorunludur.");

        var provider = _resolver.Resolve(def.Provider);
        var reference = provider.NormalizeReference(dto.ExternalReference, dto.ExternalObjectType)
            ?? new ExternalReference(dto.ExternalObjectType ?? "Generic", dto.ExternalReference, dto.ExternalReference, dto.ExternalUrl);

        var exists = await _db.ExternalObjectLinks.AnyAsync(l => l.IntegrationDefinitionId == id
            && l.InternalObjectType == dto.InternalObjectType && l.InternalObjectId == dto.InternalObjectId
            && l.ExternalObjectType == reference.ExternalObjectType && l.ExternalObjectId == reference.ExternalObjectId, ct);
        if (exists) throw new IntegrationDuplicateException("Bu dış nesne bağı zaten mevcut.");

        var now = DateTime.UtcNow;
        var actor = _currentUser.RequireUserId();
        var link = new ExternalObjectLink
        {
            Id = Guid.NewGuid(), IntegrationDefinitionId = id,
            InternalObjectType = dto.InternalObjectType.Trim(), InternalObjectId = dto.InternalObjectId,
            ExternalObjectType = reference.ExternalObjectType, ExternalObjectId = reference.ExternalObjectId,
            ExternalObjectKey = reference.ExternalObjectKey, ExternalUrl = dto.ExternalUrl ?? reference.ExternalUrl,
            CreatedByUserId = actor, CreatedAt = now
        };
        _db.ExternalObjectLinks.Add(link);
        _db.IntegrationEvents.Add(AuditFactory.Integration(IntegrationEventTypes.ExternalObjectLinked,
            $"Dış nesne bağlandı: {dto.InternalObjectType} ↔ {reference.ExternalObjectType} {reference.ExternalObjectKey}.", id, actor, now));
        await _db.SaveChangesAsync(ct);
        return link;
    }

    public async Task RemoveLinkAsync(Guid id, Guid linkId, CancellationToken ct = default)
    {
        var link = await _db.ExternalObjectLinks.FirstOrDefaultAsync(l => l.Id == linkId && l.IntegrationDefinitionId == id, ct)
            ?? throw new KeyNotFoundException("Bağ bulunamadı.");
        var actor = _currentUser.RequireUserId();
        _db.ExternalObjectLinks.Remove(link);
        _db.IntegrationEvents.Add(AuditFactory.Integration(IntegrationEventTypes.ExternalObjectUnlinked,
            $"Dış nesne bağı kaldırıldı: {link.ExternalObjectType} {link.ExternalObjectKey}.", id, actor, DateTime.UtcNow));
        await _db.SaveChangesAsync(ct);
    }

    /* ── execution cancel ───────────────────────────────── */

    public async Task CancelExecutionAsync(Guid executionId, CancellationToken ct = default)
    {
        var exec = await _db.IntegrationExecutions.Include(x => x.Events).FirstOrDefaultAsync(x => x.Id == executionId, ct)
            ?? throw new KeyNotFoundException("Yürütme bulunamadı.");
        if (IntegrationExecutionStatuses.Terminal.Contains(exec.Status))
            throw new IntegrationValidationException("Sonlanmış yürütme iptal edilemez.");
        exec.TransitionTo(IntegrationExecutionStatuses.Cancelled);
        exec.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    /* ── incoming webhook ───────────────────────────────── */

    public async Task<IncomingWebhookResult> HandleIncomingWebhookAsync(string integrationCode, string body, string? contentType,
        IReadOnlyDictionary<string, string> headers, CancellationToken ct = default)
    {
        var def = await _db.IntegrationDefinitions.Include(d => d.Credentials).Include(d => d.Events)
            .FirstOrDefaultAsync(d => d.Code == integrationCode, ct)
            ?? throw new KeyNotFoundException("Entegrasyon bulunamadı.");

        var provider = _resolver.Resolve(def.Provider);
        if (def.Status != IntegrationStatuses.Active || !provider.SupportsIncoming)
        {
            await RecordRejectionAsync(def, "Entegrasyon aktif değil veya gelen webhook desteklemiyor.", ct);
            throw new IntegrationValidationException("Entegrasyon aktif değil veya gelen webhook kabul etmiyor.");
        }
        if (!string.IsNullOrEmpty(contentType) && !contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            await RecordRejectionAsync(def, "Geçersiz Content-Type.", ct);
            throw new IntegrationValidationException("İçerik türü application/json olmalıdır.");
        }

        var secret = def.Credentials.FirstOrDefault(c => c.KeyName == IntegrationCredentialKeys.WebhookSecret);
        var decryptedSecret = secret is null ? null : SafeUnprotect(secret.EncryptedValue);
        var validation = provider.ValidateIncoming(new IncomingWebhookContext
        {
            Definition = def, Body = body, ContentType = contentType, Headers = headers, WebhookSecret = decryptedSecret
        });

        if (!validation.Accepted)
        {
            await RecordRejectionAsync(def, validation.Reason ?? "Reddedildi.", ct);
            return validation.StatusCode switch
            {
                401 => throw new IntegrationSignatureException(validation.Reason ?? "İmza geçersiz."),
                409 => throw new IntegrationDuplicateException(validation.Reason ?? "Yinelenen teslimat."),
                _ => throw new IntegrationValidationException(validation.Reason ?? "Geçersiz webhook.")
            };
        }

        // Replay/duplicate detection via delivery id (stored as CorrelationId on the incoming execution).
        var correlation = validation.DeliveryId ?? Guid.NewGuid().ToString("N");
        if (validation.DeliveryId is not null)
        {
            var dup = await _db.IntegrationExecutions.AnyAsync(x => x.IntegrationDefinitionId == def.Id
                && x.Direction == IntegrationDirections.Incoming && x.CorrelationId == correlation, ct);
            if (dup)
            {
                await RecordRejectionAsync(def, "Yinelenen teslimat.", ct);
                throw new IntegrationDuplicateException("Bu teslimat zaten işlenmiş.");
            }
        }

        var now = DateTime.UtcNow;
        var execNo = await _numbers.NextAsync($"INX-{now.Year}-", _db.IntegrationExecutions.Select(x => x.ExecutionNo), ct);
        var exec = new IntegrationExecution
        {
            Id = Guid.NewGuid(), ExecutionNo = execNo, IntegrationDefinitionId = def.Id,
            Direction = IntegrationDirections.Incoming, Operation = validation.MappedAction ?? "IncomingWebhook",
            CorrelationId = correlation, Status = IntegrationExecutionStatuses.Running,
            RequestSummary = $"Gelen webhook kabul edildi (mapping={validation.MappedAction ?? "yok"}, {body.Length} bayt).",
            StartedAt = now, CreatedAt = now
        };
        _db.IntegrationExecutions.Add(exec);
        def.Events.Add(AuditFactory.Integration(IntegrationEventTypes.IncomingWebhookAccepted,
            $"Gelen webhook kabul edildi ({execNo}).", def.Id, null, now, exec.Id));

        // Optionally update an existing external link's LastSyncedAt and prepare (but do not perform)
        // a workflow trigger — behind an explicit, default-off configuration flag.
        await ApplyIncomingReferenceAsync(def, exec, validation, now, ct);

        exec.TransitionTo(IntegrationExecutionStatuses.Succeeded);
        exec.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return new IncomingWebhookResult(202, "Kabul edildi.", exec.Id);
    }

    /* ── helpers ────────────────────────────────────────── */

    private async Task ApplyIncomingReferenceAsync(IntegrationDefinition def, IntegrationExecution exec,
        IncomingWebhookValidationResult validation, DateTime now, CancellationToken ct)
    {
        if (validation.Reference is null) return;
        var link = await _db.ExternalObjectLinks.FirstOrDefaultAsync(l => l.IntegrationDefinitionId == def.Id
            && l.ExternalObjectType == validation.Reference.ExternalObjectType
            && l.ExternalObjectId == validation.Reference.ExternalObjectId, ct);
        if (link is not null)
        {
            link.LastSyncedAt = now;
            exec.ObjectType = link.InternalObjectType;
            exec.ObjectId = link.InternalObjectId;
        }

        // Allowlisted mapping + linked object → workflow trigger is only allowed when explicitly enabled.
        if (validation.MappedAction is not null && IntegrationWebhookMappings.All.Contains(validation.MappedAction))
        {
            var note = _options.EnableWorkflowTrigger && link is not null
                ? $"İzinli eşleme '{validation.MappedAction}' iş akışı sinyali için uygun (bağlı nesne: {link.InternalObjectType})."
                : $"İzinli eşleme '{validation.MappedAction}' alındı; otomatik iş akışı tetikleme devre dışı (yalnızca kayıt).";
            def.Events.Add(AuditFactory.Integration(IntegrationEventTypes.IncomingWebhookAccepted, note, def.Id, null, now, exec.Id));
        }
    }

    private async Task RecordRejectionAsync(IntegrationDefinition def, string reason, CancellationToken ct)
    {
        def.Events.Add(AuditFactory.Integration(IntegrationEventTypes.IncomingWebhookRejected,
            $"Gelen webhook reddedildi: {reason}", def.Id, null, DateTime.UtcNow));
        await _notifications.NotifyRoleAsync(SystemRoles.Admin, NotificationTemplates.IncomingWebhookRejected,
            NotificationSeverities.Warning, new Dictionary<string, string>
            {
                ["IntegrationNo"] = def.IntegrationNo, ["IntegrationName"] = def.Name, ["Reason"] = reason
            }, null, ct);
        await _db.SaveChangesAsync(ct);
    }

    private void ValidateRequiredCredentials(IntegrationDefinition def)
    {
        bool Has(string key) => def.Credentials.Any(c => c.KeyName == key);
        var missing = def.AuthenticationType switch
        {
            IntegrationAuthTypes.ApiKey => !Has(IntegrationCredentialKeys.ApiKey),
            IntegrationAuthTypes.BearerToken => !Has(IntegrationCredentialKeys.BearerToken),
            IntegrationAuthTypes.Basic => !(Has(IntegrationCredentialKeys.BasicUsername) && Has(IntegrationCredentialKeys.BasicPassword)),
            IntegrationAuthTypes.OAuth2ClientCredentials => !(Has(IntegrationCredentialKeys.ClientId) && Has(IntegrationCredentialKeys.ClientSecret)),
            IntegrationAuthTypes.WebhookSecret => !Has(IntegrationCredentialKeys.WebhookSecret),
            _ => false
        };
        if (missing) throw new IntegrationValidationException("Seçilen kimlik doğrulama türü için gerekli kimlik bilgisi eksik.");
    }

    private Dictionary<string, string> DecryptCredentials(IntegrationDefinition def)
    {
        var map = new Dictionary<string, string>();
        foreach (var c in def.Credentials)
        {
            var v = SafeUnprotect(c.EncryptedValue);
            if (v is not null) map[c.KeyName] = v;
        }
        return map;
    }

    private string? SafeUnprotect(string encrypted)
    {
        try { return _secrets.Unprotect(encrypted); }
        catch { return null; }
    }

    private async Task<IntegrationDefinition> LoadAsync(Guid id, CancellationToken ct, bool includeCredentials = false)
    {
        var q = _db.IntegrationDefinitions.Include(d => d.Events).AsQueryable();
        if (includeCredentials) q = q.Include(d => d.Credentials);
        return await q.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new KeyNotFoundException("Entegrasyon bulunamadı.");
    }

    private async Task EnsureExistsAsync(Guid id, CancellationToken ct)
    {
        if (!await _db.IntegrationDefinitions.AnyAsync(d => d.Id == id, ct))
            throw new KeyNotFoundException("Entegrasyon bulunamadı.");
    }
}

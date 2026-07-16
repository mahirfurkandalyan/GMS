namespace Gms.Api.Services.Integrations;

/// <summary>
/// Integration Hub configuration flags. <see cref="EnableWorkflowTrigger"/> gates whether an
/// allowlisted incoming webhook mapping may automatically signal/start a workflow. It is DISABLED
/// by default: untrusted webhook payloads must never start arbitrary workflows.
/// </summary>
public sealed class IntegrationOptions
{
    public const string SectionName = "Integration";

    /// <summary>When true, allowlisted incoming mappings may trigger workflow signals (default false).</summary>
    public bool EnableWorkflowTrigger { get; set; }
}

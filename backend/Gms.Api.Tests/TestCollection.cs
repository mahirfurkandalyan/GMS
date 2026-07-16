using Xunit;

namespace Gms.Api.Tests;

/// <summary>
/// All integration tests share a single factory (one isolated SQL Server DB, created
/// once) and run sequentially within this collection — deterministic, no cross-run
/// DB clobbering.
/// </summary>
[CollectionDefinition("gms")]
public sealed class GmsCollection : ICollectionFixture<GmsWebApplicationFactory> { }

/// <summary>Shared deterministic seed ids (from EF HasData).</summary>
public static class Seed
{
    public const string Requester = "requester@gms.local";
    public const string Architect = "architect@gms.local";
    public const string QA = "qa@gms.local";
    public const string ReleaseManager = "release.manager@gms.local";
    public const string Executor = "executor@gms.local";
    public const string Validator = "validator@gms.local";
    public const string Auditor = "auditor@gms.local";
    public const string Admin = "admin@gms.local";

    public static readonly Guid CustomerId = new("d4444444-4444-4444-4444-444444444401");
    public static readonly Guid ProjectId = new("e5555555-5555-5555-5555-555555555501");
    public static readonly Guid EnvironmentId = new("f6666666-6666-6666-6666-666666666604");
}

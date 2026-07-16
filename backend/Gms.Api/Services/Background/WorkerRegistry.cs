namespace Gms.Api.Services.Background;

/// <summary>
/// Maps worker name → worker singleton so the operations endpoint can trigger a controlled
/// single run (diagnostics/tests). Not used in normal operation.
/// </summary>
public sealed class WorkerRegistry
{
    private readonly Dictionary<string, BackgroundWorkerBase> _workers;

    public WorkerRegistry(IEnumerable<BackgroundWorkerBase> workers)
        => _workers = workers.ToDictionary(w => w.WorkerName, StringComparer.OrdinalIgnoreCase);

    public bool TryGet(string name, out BackgroundWorkerBase worker) => _workers.TryGetValue(name, out worker!);

    public IReadOnlyCollection<string> Names => _workers.Keys.ToList();
}

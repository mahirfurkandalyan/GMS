using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Services.Background;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gms.Api.Controllers;

/// <summary>
/// Operational visibility and controlled diagnostics for background processing. Status is
/// Admin/Auditor-readable; the run-once trigger is Admin-only and exists for diagnostics, not
/// normal operation (workers run automatically).
/// </summary>
[ApiController]
[Route("api/operations")]
[Tags("Operations")]
[Authorize]
public class OperationsController : ControllerBase
{
    private readonly IOperationalStatusService _status;
    private readonly WorkerRegistry _workers;

    public OperationsController(IOperationalStatusService status, WorkerRegistry workers)
    {
        _status = status;
        _workers = workers;
    }

    /// <summary>Arka plan işleme birikimleri ve worker heartbeat durumları.</summary>
    [HttpGet("status")]
    [Authorize(Policy = Permissions.OperationsRead)]
    public async Task<ActionResult<OperationalStatusDto>> Status(CancellationToken ct)
        => Ok(await _status.GetAsync(ct));

    /// <summary>Bir worker'ı kontrollü biçimde tek sefer çalıştırır (teşhis; normal işleyiş değil).</summary>
    [HttpPost("workers/{workerName}/run-once")]
    [Authorize(Policy = Permissions.OperationsManage)]
    public async Task<ActionResult<WorkerRunResultDto>> RunOnce(string workerName, CancellationToken ct)
    {
        if (!_workers.TryGet(workerName, out var worker))
            return NotFound(new { message = $"Worker bulunamadı. Geçerli: {string.Join(", ", _workers.Names)}" });

        var processed = await worker.RunOnceAsync(ct);
        return Ok(new WorkerRunResultDto { WorkerName = worker.WorkerName, Processed = processed });
    }
}

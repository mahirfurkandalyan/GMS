using Gms.Api.Services.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gms.Api.Controllers;

/// <summary>
/// Public incoming-webhook receiver. Anonymous because external systems call it, but hardened:
/// strict rate limiting, small body size limit, JSON content-type check, signature/secret
/// validation, replay/duplicate detection and no internal exception detail leakage (the domain
/// exception middleware maps failures to 400/401/409). Accepted deliveries return 202.
/// </summary>
[ApiController]
[Route("api/integrations/webhooks")]
[Tags("IntegrationWebhooks")]
public class IntegrationWebhooksController : ControllerBase
{
    private const int MaxBodyBytes = 64 * 1024; // 64 KB cap on incoming webhook payloads
    private readonly IIntegrationService _service;

    public IntegrationWebhooksController(IIntegrationService service) => _service = service;

    [HttpPost("{integrationCode}")]
    [AllowAnonymous]
    [EnableRateLimiting("integration-webhook")]
    [RequestSizeLimit(MaxBodyBytes)]
    public async Task<IActionResult> Receive(string integrationCode, CancellationToken ct)
    {
        // Read the raw body with a hard cap (defense-in-depth alongside RequestSizeLimit).
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var buffer = new char[MaxBodyBytes];
        var read = await reader.ReadBlockAsync(buffer, 0, MaxBodyBytes);
        var body = new string(buffer, 0, read);

        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        var result = await _service.HandleIncomingWebhookAsync(integrationCode, body, Request.ContentType, headers, ct);
        return StatusCode(result.StatusCode, new { message = result.Message, executionId = result.ExecutionId });
    }
}

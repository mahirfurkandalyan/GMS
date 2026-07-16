using System.Diagnostics;
using System.Net;
using System.Text;

namespace Gms.Api.Services.Integrations;

/// <summary>Sanitized outcome of an HTTP call (no secrets; response body length-limited).</summary>
public sealed record HttpCallResult(bool Success, int? StatusCode, string ResponseSnippet, long ElapsedMilliseconds,
    bool IsTransient, string? Error);

/// <summary>
/// Typed HTTP client for integration outgoing requests. Uses <see cref="IHttpClientFactory"/>
/// (never a static HttpClient), enforces a per-call timeout, disables automatic redirects, and
/// limits the captured response body. Non-success codes are classified as transient/non-transient.
/// </summary>
public sealed class IntegrationHttpClient
{
    public const string ClientName = "integration";
    private const int MaxResponseSnippet = 512;

    private readonly IHttpClientFactory _factory;
    public IntegrationHttpClient(IHttpClientFactory factory) => _factory = factory;

    public async Task<HttpCallResult> SendAsync(string method, string url,
        IReadOnlyDictionary<string, string>? headers, string? jsonBody, int timeoutSeconds, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var client = _factory.CreateClient(ClientName);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 1, 120)));

        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (headers is not null)
                foreach (var (k, v) in headers)
                    request.Headers.TryAddWithoutValidation(k, v);
            if (jsonBody is not null)
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            var body = await response.Content.ReadAsStringAsync(cts.Token);
            sw.Stop();

            var code = (int)response.StatusCode;
            var snippet = Sanitize(body);
            if (response.IsSuccessStatusCode)
                return new HttpCallResult(true, code, snippet, sw.ElapsedMilliseconds, false, null);

            var transient = Common.IntegrationRetry.TransientStatusCodes.Contains(code);
            return new HttpCallResult(false, code, snippet, sw.ElapsedMilliseconds, transient, $"HTTP {code}");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            // Timeout → transient.
            return new HttpCallResult(false, StatusCodes.Status408RequestTimeout, string.Empty, sw.ElapsedMilliseconds, true, "Zaman aşımı.");
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            // Network-level failure → transient.
            return new HttpCallResult(false, null, string.Empty, sw.ElapsedMilliseconds, true, $"Ağ hatası: {ex.Message}");
        }
    }

    /// <summary>Trims and truncates a response body for safe summary storage.</summary>
    public static string Sanitize(string? body)
    {
        if (string.IsNullOrEmpty(body)) return string.Empty;
        var trimmed = body.Trim();
        return trimmed.Length <= MaxResponseSnippet ? trimmed : trimmed[..MaxResponseSnippet] + "…";
    }
}

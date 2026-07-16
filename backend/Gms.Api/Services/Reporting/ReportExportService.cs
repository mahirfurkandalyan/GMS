using System.Text;
using Gms.Api.Contracts;

namespace Gms.Api.Services.Reporting;

/// <summary>CSV export abstraction (future: JSON/Excel). This sprint: UTF-8 CSV only.</summary>
public interface IReportExportService
{
    byte[] ToCsv(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> rows);
    byte[] AuditToCsv(IEnumerable<UnifiedAuditRecordDto> records);
}

/// <summary>
/// Produces UTF-8 (BOM) CSV. Hardened against CSV/formula injection: any value starting
/// with = + - @ (or tab/CR) is prefixed with a single quote so spreadsheet apps treat it
/// as text, and values are quote-escaped.
/// </summary>
public sealed class ReportExportService : IReportExportService
{
    public byte[] ToCsv(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(Field)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(Field)));

        // Prepend a UTF-8 BOM so spreadsheet apps render Turkish characters correctly.
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return encoding.GetPreamble().Concat(encoding.GetBytes(sb.ToString())).ToArray();
    }

    public byte[] AuditToCsv(IEnumerable<UnifiedAuditRecordDto> records)
    {
        var headers = new[]
        {
            "CreatedAt", "SourceModule", "SourceTable", "EventType", "Description",
            "ActorUserId", "ActorFullName", "ActorEmail", "ObjectType", "ObjectId",
            "ObjectNumber", "RelatedProjectId", "RelatedEnvironmentId", "Result", "IpAddress"
        };
        var rows = records.Select(r => (IReadOnlyList<string?>)new[]
        {
            r.CreatedAt.ToString("o"), r.SourceModule, r.SourceTable, r.EventType, r.Description,
            r.ActorUserId?.ToString(), r.ActorFullName, r.ActorEmail, r.ObjectType, r.ObjectId?.ToString(),
            r.ObjectNumber, r.RelatedProjectId?.ToString(), r.RelatedEnvironmentId?.ToString(), r.Result, r.IpAddress
        });
        return ToCsv(headers, rows);
    }

    /// <summary>Escapes a field: neutralises formula-injection triggers and quotes as needed.</summary>
    private static string Field(string? value)
    {
        var v = value ?? string.Empty;

        // CSV/formula injection guard: a leading = + - @ (or control char) becomes text.
        if (v.Length > 0 && (v[0] is '=' or '+' or '-' or '@' or '\t' or '\r'))
            v = "'" + v;

        // Quote if it contains a comma, quote, or newline.
        if (v.Contains(',') || v.Contains('"') || v.Contains('\n') || v.Contains('\r'))
            v = "\"" + v.Replace("\"", "\"\"") + "\"";

        return v;
    }
}

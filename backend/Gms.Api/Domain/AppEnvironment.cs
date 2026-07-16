namespace Gms.Api.Domain;

/// <summary>
/// Proje ortamı (DEV, TEST, UAT, PROD).
/// Not: Sınıf adı, System.Environment ile karışmaması için "AppEnvironment"tır;
/// veritabanı tablosu ve API yolu "Environments" olarak kalır.
/// </summary>
public class AppEnvironment
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
}

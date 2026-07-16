namespace Gms.Api.Domain;

/// <summary>Müşteriye ait proje.</summary>
public class Project
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }

    // İlişkiler
    public ICollection<AppEnvironment> Environments { get; set; } = new List<AppEnvironment>();
}

namespace Gms.Api.Domain;

/// <summary>Müşteri (örn. ilaç firması).</summary>
public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }

    // İlişkiler
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

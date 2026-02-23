using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public sealed class County
{
    [Key] public Guid CountyId { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string CountyName { get; set; } = "";

    [Required, MaxLength(100)]
    public string Country { get; set; } = "Ireland";

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<City> Cities { get; set; } = new();
}
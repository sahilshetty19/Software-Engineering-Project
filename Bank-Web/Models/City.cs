using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public sealed class City
{
    [Key] public Guid CityId { get; set; } = Guid.NewGuid();

    public Guid CountyId { get; set; }
    public County? County { get; set; }

    [Required, MaxLength(100)]
    public string CityName { get; set; } = "";

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
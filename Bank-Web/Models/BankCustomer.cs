using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public enum RiskRating { Low = 0, Medium = 1, High = 2 }

public sealed class BankCustomer
{
    [Key]
    public Guid BankCustomerId { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = "";

    [Required, MaxLength(100)]
    public string LastName { get; set; } = "";

    [Required]
    public DateOnly DateOfBirth { get; set; }

    [Required, MaxLength(80)]
    public string Nationality { get; set; } = "";

    [Required, MaxLength(20)]
    public string Gender { get; set; } = "";

    [Required, MaxLength(20)]
    public string PPSN { get; set; } = "";

    [Required, MaxLength(200)]
    public string AddressLine1 { get; set; } = "";

    [Required, MaxLength(100)]
    public string City { get; set; } = "";

    [Required, MaxLength(100)]
    public string County { get; set; } = "";

    [Required, MaxLength(10)]
    public string Eircode { get; set; } = "";

    [Required, MaxLength(100)]
    public string Country { get; set; } = "Ireland";

    [Required, MaxLength(200)]
    public string Email { get; set; } = "";

    [Required, MaxLength(30)]
    public string Phone { get; set; } = "";

    [Required, MaxLength(120)]
    public string Occupation { get; set; } = "";

    [Required, MaxLength(200)]
    public string EmployerName { get; set; } = "";

    [Required, MaxLength(120)]
    public string SourceOfFunds { get; set; } = "";

    [Required]
    public bool IsPEP { get; set; }

    [Required]
    public RiskRating RiskRating { get; set; } = RiskRating.Low;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

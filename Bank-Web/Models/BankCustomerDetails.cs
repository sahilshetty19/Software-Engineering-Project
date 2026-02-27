using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public sealed class BankCustomerDetails
{
    [Key] public Guid BankCustomerId { get; set; } = Guid.NewGuid();

    public Guid KycUploadId { get; set; }
    public KycUploadDetails? KycUpload { get; set; }

    [Required, MaxLength(50)]
    public string CkycNumber { get; set; } = "";

    [Required, MaxLength(100)] public string FirstName { get; set; } = "";
    [Required, MaxLength(100)] public string MiddleName { get; set; } = "";
    [Required, MaxLength(100)] public string LastName { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }

    [Required, MaxLength(80)] public string Nationality { get; set; } = "";
    [Required, MaxLength(20)] public string Gender { get; set; } = "";
    [Required, MaxLength(20)] public string PPSN { get; set; } = "";

    [Required, MaxLength(200)] public string Email { get; set; } = "";
    [Required, MaxLength(30)] public string Phone { get; set; } = "";

    [Required, MaxLength(200)] public string AddressLine1 { get; set; } = "";

    public Guid CountyId { get; set; }
    public County? County { get; set; }

    public Guid CityId { get; set; }
    public City? City { get; set; }

    [Required, MaxLength(10)] public string Eircode { get; set; } = "";
    [Required, MaxLength(100)] public string Country { get; set; } = "Ireland";

    [Required, MaxLength(120)] public string Occupation { get; set; } = "";
    [Required, MaxLength(200)] public string EmployerName { get; set; } = "";
    [Required, MaxLength(120)] public string SourceOfFunds { get; set; } = "";

    public bool IsPEP { get; set; }
    public RiskRating RiskRating { get; set; } = RiskRating.Low;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public List<CustomerImage> CustomerImages { get; set; } = new();
}
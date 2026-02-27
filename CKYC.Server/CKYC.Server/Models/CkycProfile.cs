using System.ComponentModel.DataAnnotations;

namespace CKYC.Server.Models;

public sealed class CkycProfile
{
    [Key]
    public Guid CkycProfileId { get; set; } = Guid.NewGuid();

    // Master identifier used by Bank APIs (unique)
    [Required, MaxLength(50)]
    public string CkycNumber { get; set; } = "";

    // Dedup key (same idea as Bank KycUploadDetails.IdentityHash)
    [Required, MaxLength(64)]
    public string IdentityHash { get; set; } = ""; // SHA-256 hex

    // Personal details
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = "";

    [Required, MaxLength(100)]
    public string MiddleName { get; set; } = "";

    [Required, MaxLength(100)]
    public string LastName { get; set; } = "";

    public DateOnly DateOfBirth { get; set; }

    [Required, MaxLength(80)]
    public string Nationality { get; set; } = "";

    [Required, MaxLength(20)]
    public string Gender { get; set; } = "";

    [Required, MaxLength(20)]
    public string PPSN { get; set; } = "";

    // Contact
    [Required, MaxLength(200)]
    public string Email { get; set; } = "";

    [Required, MaxLength(30)]
    public string Phone { get; set; } = "";

    // Address
    [Required, MaxLength(200)]
    public string AddressLine1 { get; set; } = "";

    // NOTE: In CKYC you are storing central/master data.
    // Your CKYC DB screenshot shows City/County as text, not FK IDs.
    [Required, MaxLength(100)]
    public string City { get; set; } = "";

    [Required, MaxLength(100)]
    public string County { get; set; } = "";

    [Required, MaxLength(10)]
    public string Eircode { get; set; } = "";

    [Required, MaxLength(100)]
    public string Country { get; set; } = "Ireland";

    // Work/finance (same as Bank)
    [Required, MaxLength(120)]
    public string Occupation { get; set; } = "";

    [Required, MaxLength(200)]
    public string EmployerName { get; set; } = "";

    [Required, MaxLength(120)]
    public string SourceOfFunds { get; set; } = "";

    public bool IsPEP { get; set; }

    public RiskRating RiskRating { get; set; } = RiskRating.Low;

    
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    
    public List<CkycDocument> Documents { get; set; } = new();
}
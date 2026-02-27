namespace CKYC.Server.DTOs;

public class CkycProfileDto
{
    public Guid CkycProfileId { get; set; }
    public string CkycNumber { get; set; } = "";

    public string FirstName { get; set; } = "";
    public string MiddleName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
    public string PPSN { get; set; } = "";

    public string Nationality { get; set; } = "";
    public string Gender { get; set; } = "";

    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";

    public string AddressLine1 { get; set; } = "";
    public string City { get; set; } = "";
    public string County { get; set; } = "";
    public string Eircode { get; set; } = "";
    public string Country { get; set; } = "";

    public string Occupation { get; set; } = "";
    public string EmployerName { get; set; } = "";
    public string SourceOfFunds { get; set; } = "";

    public bool IsPEP { get; set; }
    public short RiskRating { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public List<CkycDocumentDto> Documents { get; set; } = new();
}
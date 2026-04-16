using Bank.Web.Models;

namespace Bank.Web.Services.BulkUpload;

public sealed class BulkUploadValidatedRow
{
    public BulkUploadParsedRow ParsedRow { get; set; } = new();

    public bool IsValid { get; set; }

    public string? ErrorMessage { get; set; }

    public string? PscFrontFilePath { get; set; }
    public string? PscBackFilePath { get; set; }

    public Guid? CountyId { get; set; }
    public Guid? CityId { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public bool? IsPEP { get; set; }

    public RiskRating? RiskRating { get; set; }
}
namespace Bank.Web.Services.BulkUpload;

public sealed class BulkUploadParsedRow
{
    public int RowNumber { get; set; }

    public string RowRef { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string MiddleName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string DateOfBirth { get; set; } = "";
    public string PPSN { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string AddressLine1 { get; set; } = "";
    public string County { get; set; } = "";
    public string City { get; set; } = "";
    public string Eircode { get; set; } = "";
    public string IsPEP { get; set; } = "";
    public string RiskRating { get; set; } = "";
    public string PscFrontFileName { get; set; } = "";
    public string PscBackFileName { get; set; } = "";
}
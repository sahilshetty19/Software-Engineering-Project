namespace Bank.Web.Services.BulkUpload;

public sealed class BulkUploadValidationResult
{
    public List<BulkUploadValidatedRow> Rows { get; set; } = new();

    public int TotalRows => Rows.Count;
    public int ValidRows => Rows.Count(x => x.IsValid);
    public int InvalidRows => Rows.Count(x => !x.IsValid);
}
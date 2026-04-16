namespace Bank.Web.Services.BulkUpload;

public sealed class BulkUploadPackageReadResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public string ExtractedRootPath { get; set; } = "";
    public string ExcelFilePath { get; set; } = "";
    public string PscFrontFolderPath { get; set; } = "";
    public string PscBackFolderPath { get; set; } = "";

    public List<BulkUploadParsedRow> Rows { get; set; } = new();
}
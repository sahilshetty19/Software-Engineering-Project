namespace CKYC.Server.DTOs;

public class CkycDocumentDto
{
    public Guid CkycDocumentId { get; set; }
    public short DocumentType { get; set; }

    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public string FileHashSha256 { get; set; } = "";

    public string? OcrText { get; set; }
    public float? OcrConfidence { get; set; }

    public string? ExtractedDocNumber { get; set; }
    public DateOnly? ExtractedExpiryDate { get; set; }

    public DateTimeOffset UploadedAtUtc { get; set; }
}
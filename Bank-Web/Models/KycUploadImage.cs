using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public sealed class KycUploadImage
{
    [Key] public Guid KycUploadImageId { get; set; } = Guid.NewGuid();

    public Guid KycUploadId { get; set; }
    public KycUploadDetails? KycUpload { get; set; }

    public DocumentType DocumentType { get; set; } = DocumentType.Other;

    [Required, MaxLength(255)] public string FileName { get; set; } = "";
    [Required, MaxLength(100)] public string ContentType { get; set; } = "";

    public long FileSizeBytes { get; set; }

    [Required, MaxLength(64)]
    public string FileHashSha256 { get; set; } = "";

    public byte[] ImageBytes { get; set; } = Array.Empty<byte>();

    public string OcrText { get; set; } = "";
    public float OcrConfidence { get; set; }

    [Required, MaxLength(80)]
    public string ExtractedDocNumber { get; set; } = "";

    public DateOnly? ExtractedExpiryDate { get; set; }

    public DateTimeOffset UploadedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
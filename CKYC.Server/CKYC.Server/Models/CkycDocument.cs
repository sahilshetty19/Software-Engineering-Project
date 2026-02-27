using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CKYC.Server.Models;

public sealed class CkycDocument
{
    [Key]
    public Guid CkycDocumentId { get; set; } = Guid.NewGuid();

    // Foreign key to CKYC Profile
    [Required]
    public Guid CkycProfileId { get; set; }

    public CkycProfile? Profile { get; set; }

    // Same enum as bank (keep enum names identical across systems)
    public DocumentType DocumentType { get; set; } = DocumentType.Other;

    [Required, MaxLength(255)]
    public string FileName { get; set; } = "";

    [Required, MaxLength(100)]
    public string ContentType { get; set; } = "";

    public long FileSizeBytes { get; set; }

    [Required, MaxLength(64)]
    public string FileHashSha256 { get; set; } = "";


    [Required]
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();

  
    public string? OcrText { get; set; }

    public float? OcrConfidence { get; set; }

    [MaxLength(80)]
    public string? ExtractedDocNumber { get; set; }

    public DateOnly? ExtractedExpiryDate { get; set; }

    public DateTimeOffset UploadedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
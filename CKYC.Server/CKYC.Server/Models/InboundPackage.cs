using System.ComponentModel.DataAnnotations;

namespace CKYC.Server.Models;

public class InboundPackage
{
    [Key]
    public Guid InboundPackageId { get; set; }

    // FK to submission
    [Required]
    public Guid InboundSubmissionId { get; set; }
    public InboundSubmission? Submission { get; set; }

    
    [Required, MaxLength(255)]
    public string FileName { get; set; } = "";

    [Required, MaxLength(100)]
    public string ContentType { get; set; } = "application/zip";

    public long FileSizeBytes { get; set; }

    [Required, MaxLength(64)]
    public string FileHashSha256 { get; set; } = "";

    
    [Required]
    public byte[] ZipBytes { get; set; } = Array.Empty<byte>();

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
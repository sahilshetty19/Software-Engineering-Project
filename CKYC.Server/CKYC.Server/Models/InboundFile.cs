using System.ComponentModel.DataAnnotations;

namespace CKYC.Server.Models;

public class InboundFile
{
    [Key]
    public Guid InboundFileId { get; set; }

    [Required]
    public Guid InboundSubmissionId { get; set; }
    public InboundSubmission? Submission { get; set; }

    [Required, MaxLength(255)]
    public string OriginalFileName { get; set; } = "";

    [Required, MaxLength(100)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long FileSizeBytes { get; set; }

    [Required, StringLength(64)]
    public string FileHashSha256 { get; set; } = "";

    [Required, MaxLength(500)]
    public string StorageRef { get; set; } = "";

    public FileRole FileRole { get; set; } = FileRole.Other;

    public DateTime ExtractedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid? LinkedCkycDocumentId { get; set; }
    public CkycDocument? LinkedDocument { get; set; }
}
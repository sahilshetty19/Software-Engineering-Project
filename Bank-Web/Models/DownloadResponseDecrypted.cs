using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public sealed class DownloadResponseDecrypted
{
    [Key] public Guid DownloadRespDecId { get; set; } = Guid.NewGuid();

    public Guid KycUploadId { get; set; }
    public KycUploadDetails? KycUpload { get; set; }

    [Required, MaxLength(60)] public string RequestRef { get; set; } = "";

    [Required, MaxLength(50)] public string CkycNumber { get; set; } = "";

    public DownloadStatus DownloadStatus { get; set; } = DownloadStatus.Failed;

    // jsonb (simple mapping for demo)
    public string PayloadJson { get; set; } = "{}";

    public string? Message { get; set; }

    public DateTimeOffset DecryptedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
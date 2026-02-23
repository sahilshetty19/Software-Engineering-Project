using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public sealed class ZipFileUploadDetails
{
    [Key] public Guid ZipUploadId { get; set; } = Guid.NewGuid();

    public Guid KycUploadId { get; set; }
    public KycUploadDetails? KycUpload { get; set; }

    [Required, MaxLength(255)] public string ZipFileName { get; set; } = "";
    [Required, MaxLength(64)] public string ZipHashSha256 { get; set; } = "";

    public long ZipSizeBytes { get; set; }

    [Required, MaxLength(500)] public string SftpRemotePath { get; set; } = "";

    public UploadStatus UploadStatus { get; set; } = UploadStatus.Queued;

    public DateTimeOffset? UploadedAtUtc { get; set; }
    public string? FailureReason { get; set; }
}
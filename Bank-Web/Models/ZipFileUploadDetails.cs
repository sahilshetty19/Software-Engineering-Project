using Bank.Web.Models;
using System.ComponentModel.DataAnnotations;

public class ZipFileUploadDetails
{
    [Key]
    public Guid ZipUploadId { get; set; }

    public Guid KycUploadId { get; set; }
    public KycUploadDetails? KycUpload { get; set; }
    [Required, MaxLength(255)]
    public string ZipFileName { get; set; } = "";

    [Required, MaxLength(64)]
    public string ZipHashSha256 { get; set; } = "";

    public long ZipSizeBytes { get; set; }

    
    public byte[]? ZipBytes { get; set; }

    [MaxLength(500)]
    public string? SftpRemotePath { get; set; } 

    public short UploadStatus { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    public string? FailureReason { get; set; }
}
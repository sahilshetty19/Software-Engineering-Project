using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public sealed class DownloadResponseEncrypted
{
    [Key] public Guid DownloadResponseEncryptedId { get; set; } = Guid.NewGuid();

    public Guid KycUploadId { get; set; }
    public KycUploadDetails? KycUpload { get; set; }

    [Required, MaxLength(50)]
    public string CkycNumber { get; set; } = "";

    [Required, MaxLength(64)]
    public string ResponseHashSha256 { get; set; } = "";

    public byte[] EncryptedResponseBytes { get; set; } = Array.Empty<byte>();

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
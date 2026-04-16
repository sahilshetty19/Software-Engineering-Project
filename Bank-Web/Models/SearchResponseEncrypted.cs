using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public sealed class SearchResponseEncrypted
{
    [Key] public Guid SearchResponseEncryptedId { get; set; } = Guid.NewGuid();

    public Guid KycUploadId { get; set; }
    public KycUploadDetails? KycUpload { get; set; }

    [Required, MaxLength(64)]
    public string RequestHashSha256 { get; set; } = "";

    [Required, MaxLength(64)]
    public string ResponseHashSha256 { get; set; } = "";

    public byte[] EncryptedRequestBytes { get; set; } = Array.Empty<byte>();
    public byte[] EncryptedResponseBytes { get; set; } = Array.Empty<byte>();

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
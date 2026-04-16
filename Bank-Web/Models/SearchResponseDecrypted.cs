using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public sealed class SearchResponseDecrypted
{
    [Key] public Guid SearchResponseDecryptedId { get; set; } = Guid.NewGuid();

    public Guid KycUploadId { get; set; }
    public KycUploadDetails? KycUpload { get; set; }

    public Guid SearchResponseEncryptedId { get; set; }
    public SearchResponseEncrypted? Encrypted { get; set; }

    public string DecryptedJson { get; set; } = "";

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
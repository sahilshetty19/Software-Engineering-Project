using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public sealed class SearchResponseEncrypted
{
    [Key] public Guid SearchRespEncId { get; set; } = Guid.NewGuid();

    public Guid KycUploadId { get; set; }
    public KycUploadDetails? KycUpload { get; set; }

    [Required, MaxLength(60)] public string RequestRef { get; set; } = "";

    [Required, MaxLength(50)] public string CipherAlgorithm { get; set; } = "AES-256-GCM";
    [Required, MaxLength(50)] public string KeyId { get; set; } = "v1";
    [Required, MaxLength(64)] public string IvBase64 { get; set; } = "";

    public string CiphertextBase64 { get; set; } = "";

    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
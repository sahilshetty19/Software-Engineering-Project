using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public sealed class DownloadResponseDecrypted
{
    [Key] public Guid DownloadResponseDecryptedId { get; set; } = Guid.NewGuid();

    public Guid KycUploadId { get; set; }
    public KycUploadDetails? KycUpload { get; set; }

    [Required, MaxLength(50)]
    public string CkycNumber { get; set; } = "";

    [Required, MaxLength(64)]
    public string ResponseHashSha256 { get; set; } = "";

    // Store plain JSON (decrypted)
    public string ResponseJson { get; set; } = "";
   
    [Required]
    public string PayloadJson { get; set; } = "";


    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public sealed class SearchResponseDecrypted
{
    [Key] public Guid SearchRespDecId { get; set; } = Guid.NewGuid();

    public Guid KycUploadId { get; set; }
    public KycUploadDetails? KycUpload { get; set; }

    [Required, MaxLength(60)] public string RequestRef { get; set; } = "";

    public SearchStatus SearchStatus { get; set; } = SearchStatus.Failed;

    [MaxLength(50)] public string? MatchedCkycNumber { get; set; }
    public float? MatchScore { get; set; }

    public string? Message { get; set; }

    public DateTimeOffset DecryptedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
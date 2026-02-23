using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public sealed class KycUpdationResponse
{
    [Key] public Guid KycUpdateRespId { get; set; } = Guid.NewGuid();

    public Guid KycUploadId { get; set; }
    public KycUploadDetails? KycUpload { get; set; }

    [Required, MaxLength(60)] public string RequestRef { get; set; } = "";

    [MaxLength(50)] public string? CkycNumber { get; set; }

    public UpdateStatus UpdateStatus { get; set; } = UpdateStatus.Failed;

    public string? RejectionReason { get; set; }

    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
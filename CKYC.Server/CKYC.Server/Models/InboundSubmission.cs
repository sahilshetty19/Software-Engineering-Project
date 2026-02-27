using System.ComponentModel.DataAnnotations;

namespace CKYC.Server.Models;

public class InboundSubmission
{
    [Key]
    public Guid InboundSubmissionId { get; set; }

    [Required, MaxLength(30)]
    public string BankCode { get; set; } = "";

    [Required, MaxLength(60)]
    public string RequestRef { get; set; } = "";

    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Received;

    
    [MaxLength(300)]
    public string? StatusMessage { get; set; }

    // Keep this for failure details
    public string? FailureReason { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    
    [MaxLength(50)]
    public string? CkycNumber { get; set; }

    public Guid? LinkedCkycProfileId { get; set; }
    public CkycProfile? LinkedProfile { get; set; }

    public List<InboundPackage> Packages { get; set; } = new();
    public List<InboundFile> Files { get; set; } = new();
}
using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public enum KycWorkflowStepStatus
{
    Started = 0,
    Success = 1,
    Failed = 2,
    Skipped = 3
}

public sealed class KycWorkflowExecutionLog
{
    [Key]
    public Guid KycWorkflowExecutionLogId { get; set; } = Guid.NewGuid();

    public Guid KycUploadId { get; set; }
    public KycUploadDetails? KycUpload { get; set; }

    [Required, MaxLength(100)]
    public string StepName { get; set; } = "";

    public KycWorkflowStepStatus Status { get; set; } = KycWorkflowStepStatus.Started;

    public string? Message { get; set; }
    public string? ErrorDetails { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }
}
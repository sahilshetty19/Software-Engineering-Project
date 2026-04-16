using System.ComponentModel.DataAnnotations;

namespace Bank.Web.Models;

public sealed class KycUploadDetails
{
    [Key] public Guid KycUploadId { get; set; } = Guid.NewGuid();

    [Required, MaxLength(60)]
    public string RequestRef { get; set; } = "";

    public UploadSource Source { get; set; } = UploadSource.ManualForm;
    public KycWorkflowStatus Status { get; set; } = KycWorkflowStatus.Draft;
    public OcrStatus OcrStatus { get; set; } = OcrStatus.Pending;
    public ValidationStatus ValidationStatus { get; set; } = ValidationStatus.Pass;
    public DedupeStatus DedupeStatus { get; set; } = DedupeStatus.NotChecked;

    [Required, MaxLength(64)]
    public string IdentityHash { get; set; } = ""; // SHA-256 hex

    [MaxLength(50)]
    public string? CkycNumber { get; set; }

    public string? FailureReason { get; set; }

    public AutomationRunStatus AutomationStatus { get; set; } = AutomationRunStatus.Queued;
    public int RetryAttemptCount { get; set; }
    public int MaxRetryAttempts { get; set; } = 5;
    public DateTimeOffset? NextRetryAtUtc { get; set; }
    public DateTimeOffset? LastAutomationStartedAtUtc { get; set; }
    public DateTimeOffset? LastAutomationCompletedAtUtc { get; set; }
    public DateTimeOffset? AutomationLockedUntilUtc { get; set; }

    [MaxLength(100)]
    public string? LastFailedStep { get; set; }

    public string? LastAutomationError { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }

    // Customer fields (staging)
    [Required, MaxLength(100)] public string FirstName { get; set; } = "";
   
    [Required, MaxLength(100)] public string MiddleName { get; set; } = "";
    [Required, MaxLength(100)] public string LastName { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }

    [Required, MaxLength(80)] public string Nationality { get; set; } = "";
    [Required, MaxLength(20)] public string Gender { get; set; } = "";
    [Required, MaxLength(20)] public string PPSN { get; set; } = "";

    [Required, MaxLength(200)] public string Email { get; set; } = "";
    [Required, MaxLength(30)] public string Phone { get; set; } = "";

    [Required, MaxLength(200)] public string AddressLine1 { get; set; } = "";

    // County/City dropdowns (master data)
    public Guid CountyId { get; set; }
    public County? County { get; set; }

    public Guid CityId { get; set; }
    public City? City { get; set; }

    [Required, MaxLength(10)] public string Eircode { get; set; } = "";
    [Required, MaxLength(100)] public string Country { get; set; } = "Ireland";

    [Required, MaxLength(120)] public string Occupation { get; set; } = "";
    [Required, MaxLength(200)] public string EmployerName { get; set; } = "";
    [Required, MaxLength(120)] public string SourceOfFunds { get; set; } = "";

    public bool IsPEP { get; set; }
    public RiskRating RiskRating { get; set; } = RiskRating.Low;

    public List<KycUploadImage> Images { get; set; } = new();
    public bool SearchExecuted { get; set; } = false;
    public bool? SearchFound { get; set; } = null;
    public bool DedupeExecuted { get; set; }
    public bool? DedupePassed { get; set; }
    public DateTimeOffset? DedupeCheckedAtUtc { get; set; }
    public string? DedupeMessage { get; set; }
    public DateTimeOffset? CkycDownloadedAtUtc { get; set; } 
}

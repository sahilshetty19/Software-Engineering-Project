using System.ComponentModel.DataAnnotations;

namespace CKYC.Server.Models;

public class AuditEvent
{
    [Key]
    public Guid AuditEventId { get; set; }

    public DateTime EventTimeUtc { get; set; } = DateTime.UtcNow;

    public ActorType ActorType { get; set; } = ActorType.System;

    [Required, MaxLength(100)]
    public string ActorId { get; set; } = "system";

    [Required, MaxLength(80)]
    public string Action { get; set; } = "";

    [Required, MaxLength(50)]
    public string EntityType { get; set; } = "";

    public Guid? EntityId { get; set; }

    [MaxLength(60)]
    public string? RequestRef { get; set; }

    public AuditOutcome Outcome { get; set; } = AuditOutcome.Success;

    [Required]
    public string DetailsJson { get; set; } = "{}";
}
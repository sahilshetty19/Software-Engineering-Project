using CKYC.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CKYC.Server.Data;

public class CkycDbContext : DbContext
{
    public CkycDbContext(DbContextOptions<CkycDbContext> options) : base(options) { }

    public DbSet<CkycProfile> CkycProfiles => Set<CkycProfile>();
    public DbSet<CkycDocument> CkycDocuments => Set<CkycDocument>();
    public DbSet<InboundSubmission> InboundSubmissions => Set<InboundSubmission>();
    public DbSet<InboundPackage> InboundPackages => Set<InboundPackage>();
    public DbSet<InboundFile> InboundFiles => Set<InboundFile>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =========================
        // CKYC PROFILE
        // =========================
        modelBuilder.Entity<CkycProfile>()
            .HasIndex(x => x.CkycNumber)
            .IsUnique();

        modelBuilder.Entity<CkycProfile>()
            .HasIndex(x => x.IdentityHash);

        // (Recommended) Fast search filter support
        modelBuilder.Entity<CkycProfile>()
            .HasIndex(x => new { x.FirstName, x.LastName, x.DateOfBirth });

        // =========================
        // CKYC DOCUMENT
        // =========================
        modelBuilder.Entity<CkycDocument>()
            .HasIndex(x => x.DocumentType);

        modelBuilder.Entity<CkycDocument>()
            .HasIndex(x => x.FileHashSha256)
            .IsUnique();

        modelBuilder.Entity<CkycDocument>()
            .HasOne(d => d.Profile)
            .WithMany(p => p.Documents)
            .HasForeignKey(d => d.CkycProfileId)
            .OnDelete(DeleteBehavior.Cascade);

   
        modelBuilder.Entity<CkycDocument>()
            .Property(x => x.FileBytes)
            .HasColumnType("bytea");

      
        modelBuilder.Entity<InboundSubmission>()
            .HasIndex(x => x.RequestRef)
            .IsUnique();

        modelBuilder.Entity<InboundSubmission>()
            .HasOne(x => x.LinkedProfile)
            .WithMany()
            .HasForeignKey(x => x.LinkedCkycProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InboundPackage>()
            .HasOne(x => x.Submission)
            .WithMany(s => s.Packages)
            .HasForeignKey(x => x.InboundSubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

  
        modelBuilder.Entity<InboundFile>()
            .HasIndex(x => x.FileHashSha256);

        modelBuilder.Entity<InboundFile>()
            .HasOne(x => x.Submission)
            .WithMany(s => s.Files)
            .HasForeignKey(x => x.InboundSubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InboundFile>()
            .HasOne(x => x.LinkedDocument)
            .WithMany()
            .HasForeignKey(x => x.LinkedCkycDocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        
        modelBuilder.Entity<AuditEvent>()
            .HasIndex(x => x.EventTimeUtc);

        modelBuilder.Entity<AuditEvent>()
            .HasIndex(x => x.Action);

        modelBuilder.Entity<AuditEvent>()
            .HasIndex(x => x.EntityType);

        modelBuilder.Entity<AuditEvent>()
            .HasIndex(x => x.RequestRef);
    }
}
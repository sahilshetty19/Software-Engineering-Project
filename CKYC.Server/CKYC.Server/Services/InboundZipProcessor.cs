using CKYC.Server.Data;
using CKYC.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace CKYC.Server.Services;

public sealed class InboundZipProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public InboundZipProcessor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("InboundZipProcessor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOne(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("InboundZipProcessor fatal: " + ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessOne(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CkycDbContext>();

        var submission = await db.InboundSubmissions
            .Include(s => s.Packages)
            .Where(s => s.Status == SubmissionStatus.Received)
            .OrderBy(s => s.ReceivedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (submission == null) return;

        submission.Status = SubmissionStatus.Processing;
        submission.StatusMessage = "Processing ZIP (extract + profile + docs)...";
        submission.FailureReason = null;
        await db.SaveChangesAsync(ct);

        try
        {
            var pkg = submission.Packages
                .OrderByDescending(p => p.UploadedAtUtc)
                .FirstOrDefault();

            if (pkg == null) throw new Exception("No package found for this submission.");
            if (pkg.ZipBytes == null || pkg.ZipBytes.Length == 0) throw new Exception("ZIP bytes are empty.");

            var (kyc, files) = ExtractZip(pkg.ZipBytes);

            ValidateKyc(kyc);

            var ppsn = Normalize(kyc.PPSN);

            var existing = await db.CkycProfiles
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p =>
                    p.PPSN.ToUpper() == ppsn &&
                    p.DateOfBirth == kyc.DateOfBirth, ct);

            CkycProfile profile;

            if (existing == null)
            {
                profile = new CkycProfile
                {
                    CkycProfileId = Guid.NewGuid(),
                    CkycNumber = await GenerateUniqueCkycNumber(db, ct),
                    IdentityHash = "",

                    FirstName = kyc.FirstName.Trim(),
                    MiddleName = (kyc.MiddleName ?? "").Trim(),
                    LastName = kyc.LastName.Trim(),
                    DateOfBirth = kyc.DateOfBirth,
                    PPSN = kyc.PPSN.Trim(),

                    Nationality = (kyc.Nationality ?? "Ireland").Trim(),
                    Gender = (kyc.Gender ?? "NA").Trim(),
                    Email = (kyc.Email ?? "").Trim(),
                    Phone = (kyc.Phone ?? "").Trim(),

                    AddressLine1 = (kyc.AddressLine1 ?? "").Trim(),
                    City = (kyc.City ?? "").Trim(),
                    County = (kyc.County ?? "").Trim(),
                    Eircode = (kyc.Eircode ?? "").Trim(),
                    Country = (kyc.Country ?? "Ireland").Trim(),

                    Occupation = (kyc.Occupation ?? "").Trim(),
                    EmployerName = (kyc.EmployerName ?? "").Trim(),
                    SourceOfFunds = (kyc.SourceOfFunds ?? "").Trim(),

                    IsPEP = kyc.IsPEP,
                    RiskRating = (RiskRating)kyc.RiskRating,

                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                };

                db.CkycProfiles.Add(profile);
            }
            else
            {
                profile = existing;

                profile.FirstName = kyc.FirstName.Trim();
                profile.MiddleName = (kyc.MiddleName ?? "").Trim();
                profile.LastName = kyc.LastName.Trim();
                profile.Email = (kyc.Email ?? "").Trim();
                profile.Phone = (kyc.Phone ?? "").Trim();
                profile.AddressLine1 = (kyc.AddressLine1 ?? "").Trim();
                profile.City = (kyc.City ?? "").Trim();
                profile.County = (kyc.County ?? "").Trim();
                profile.Eircode = (kyc.Eircode ?? "").Trim();
                profile.Country = (kyc.Country ?? "Ireland").Trim();
                profile.Occupation = (kyc.Occupation ?? "").Trim();
                profile.EmployerName = (kyc.EmployerName ?? "").Trim();
                profile.SourceOfFunds = (kyc.SourceOfFunds ?? "").Trim();
                profile.IsPEP = kyc.IsPEP;
                profile.RiskRating = (RiskRating)kyc.RiskRating;
                profile.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }

            foreach (var f in files.Where(x => !x.Name.Equals("kyc.json", StringComparison.OrdinalIgnoreCase)))
            {
                if (f.Bytes == null || f.Bytes.Length == 0) continue;

                var hash = Sha256Hex(f.Bytes);

                var docExists = await db.CkycDocuments
                    .AsNoTracking()
                    .AnyAsync(d => d.FileHashSha256 == hash, ct);

                if (docExists) continue;

                profile.Documents.Add(new CkycDocument
                {
                    CkycDocumentId = Guid.NewGuid(),
                    CkycProfileId = profile.CkycProfileId,
                    DocumentType = MapDocumentTypeFromFileName(f.Name),
                    FileName = f.Name,
                    ContentType = GuessContentType(f.Name),
                    FileSizeBytes = f.Bytes.LongLength,
                    FileHashSha256 = hash,
                    FileBytes = f.Bytes,
                    UploadedAtUtc = DateTimeOffset.UtcNow
                });
            }

            submission.LinkedCkycProfileId = profile.CkycProfileId;
            submission.CkycNumber = profile.CkycNumber;
            submission.Status = SubmissionStatus.Success;
            submission.StatusMessage = "Processed OK (profile + docs saved).";
            submission.ProcessedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            Console.WriteLine($"Processed submission {submission.RequestRef} => CKYC={submission.CkycNumber}");
        }
        catch (Exception ex)
        {
            submission.Status = SubmissionStatus.Failed;
            submission.StatusMessage = "Processing failed.";
            submission.FailureReason = ex.Message;
            submission.ProcessedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            Console.WriteLine($"Submission {submission.RequestRef} FAILED: {ex.Message}");
        }
    }

    private static (KycJson Kyc, List<(string Name, byte[] Bytes)> Files) ExtractZip(byte[] zipBytes)
    {
        using var ms = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        KycJson? kyc = null;
        var files = new List<(string Name, byte[] Bytes)>();

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name)) continue;

            using var es = entry.Open();
            using var outMs = new MemoryStream();
            es.CopyTo(outMs);
            var bytes = outMs.ToArray();

            files.Add((entry.Name, bytes));

            if (entry.Name.Equals("kyc.json", StringComparison.OrdinalIgnoreCase))
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                kyc = JsonSerializer.Deserialize<KycJson>(bytes, opts)
                      ?? throw new Exception("kyc.json invalid JSON.");
            }
        }

        if (kyc == null) throw new Exception("kyc.json not found inside ZIP.");
        return (kyc, files);
    }

    private static void ValidateKyc(KycJson kyc)
    {
        if (string.IsNullOrWhiteSpace(kyc.FirstName)) throw new Exception("kyc.json missing firstName.");
        if (string.IsNullOrWhiteSpace(kyc.LastName)) throw new Exception("kyc.json missing lastName.");
        if (string.IsNullOrWhiteSpace(kyc.PPSN)) throw new Exception("kyc.json missing ppsn.");
        if (kyc.DateOfBirth == default) throw new Exception("kyc.json missing dateOfBirth.");
    }

    private static async Task<string> GenerateUniqueCkycNumber(CkycDbContext db, CancellationToken ct)
    {
        for (int i = 0; i < 5; i++)
        {
            var candidate = "CKYC-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var exists = await db.CkycProfiles.AnyAsync(p => p.CkycNumber == candidate, ct);
            if (!exists) return candidate;

            await Task.Delay(5, ct);
        }

        return "CKYC-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
    }

    // ✅ UPDATED: mapping based on filename (ZipBuilder naming)
    private static DocumentType MapDocumentTypeFromFileName(string fileName)
    {
        var n = (fileName ?? "").Trim().ToLowerInvariant();

        if (n.StartsWith("pscfront")) return DocumentType.PSCFront;
        if (n.StartsWith("pscback")) return DocumentType.PSCBack;

        // fallback if you ever use other patterns
        if (n.Contains("psc") && n.Contains("front")) return DocumentType.PSCFront;
        if (n.Contains("psc") && n.Contains("back")) return DocumentType.PSCBack;

        return DocumentType.Other;
    }

    private static string GuessContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".json" => "application/json",
            _ => "application/octet-stream"
        };
    }

    private static string Normalize(string s) => (s ?? "").Trim().ToUpperInvariant();

    private static string Sha256Hex(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed class KycJson
    {
        public string FirstName { get; set; } = "";
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = "";
        public DateOnly DateOfBirth { get; set; }
        public string PPSN { get; set; } = "";

        public string? Nationality { get; set; }
        public string? Gender { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }

        public string? AddressLine1 { get; set; }
        public string? City { get; set; }
        public string? County { get; set; }
        public string? Eircode { get; set; }
        public string? Country { get; set; }

        public string? Occupation { get; set; }
        public string? EmployerName { get; set; }
        public string? SourceOfFunds { get; set; }

        public bool IsPEP { get; set; }
        public short RiskRating { get; set; }
    }
}
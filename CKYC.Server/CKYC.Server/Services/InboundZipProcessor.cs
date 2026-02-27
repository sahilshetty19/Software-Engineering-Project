using CKYC.Server.Data;
using CKYC.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
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
            Console.WriteLine("InboundZipProcessor tick...");

            try
            {
                await ProcessOne(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("InboundZipProcessor fatal error: " + ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
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

        Console.WriteLine($"Processing submission RequestRef={submission.RequestRef}");

        submission.Status = SubmissionStatus.Processing;
        submission.StatusMessage = "Processing ZIP (unzipping + creating CKYC profile)...";
        submission.FailureReason = null;
        await db.SaveChangesAsync(ct);

        try
        {
            var pkg = submission.Packages
                .OrderByDescending(p => p.UploadedAtUtc)
                .FirstOrDefault();

            if (pkg == null) throw new Exception("No ZIP package found for submission.");
            if (pkg.ZipBytes == null || pkg.ZipBytes.Length == 0) throw new Exception("ZIP bytes are empty.");

            var parsed = ExtractKycJsonAndFiles(pkg.ZipBytes);

            var fn = Normalize(parsed.Kyc.FirstName);
            var mn = Normalize(parsed.Kyc.MiddleName);
            var ln = Normalize(parsed.Kyc.LastName);
            var pn = Normalize(parsed.Kyc.PPSN);

            var existing = await db.CkycProfiles
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p =>
                    p.DateOfBirth == parsed.Kyc.DateOfBirth &&
                    p.FirstName.ToUpper() == fn &&
                    p.MiddleName.ToUpper() == mn &&
                    p.LastName.ToUpper() == ln &&
                    p.PPSN.ToUpper() == pn, ct);

            CkycProfile profile;

            if (existing == null)
            {
                var newCkycNumber = await GenerateUniqueCkycNumber(db, ct);

                profile = new CkycProfile
                {
                    CkycProfileId = Guid.NewGuid(),
                    CkycNumber = newCkycNumber,
                    IdentityHash = "",

                    FirstName = parsed.Kyc.FirstName.Trim(),
                    MiddleName = parsed.Kyc.MiddleName.Trim(),
                    LastName = parsed.Kyc.LastName.Trim(),
                    DateOfBirth = parsed.Kyc.DateOfBirth,
                    PPSN = parsed.Kyc.PPSN.Trim(),

                    Nationality = parsed.Kyc.Nationality.Trim(),
                    Gender = parsed.Kyc.Gender.Trim(),
                    Email = parsed.Kyc.Email.Trim(),
                    Phone = parsed.Kyc.Phone.Trim(),

                    AddressLine1 = parsed.Kyc.AddressLine1.Trim(),
                    City = parsed.Kyc.City.Trim(),
                    County = parsed.Kyc.County.Trim(),
                    Eircode = parsed.Kyc.Eircode.Trim(),
                    Country = parsed.Kyc.Country.Trim(),

                    Occupation = parsed.Kyc.Occupation.Trim(),
                    EmployerName = parsed.Kyc.EmployerName.Trim(),
                    SourceOfFunds = parsed.Kyc.SourceOfFunds.Trim(),

                    IsPEP = parsed.Kyc.IsPEP,
                    RiskRating = (RiskRating)parsed.Kyc.RiskRating,

                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                };

                db.CkycProfiles.Add(profile);
            }
            else
            {
                profile = existing;

                profile.MiddleName = parsed.Kyc.MiddleName.Trim();
                profile.Email = parsed.Kyc.Email.Trim();
                profile.Phone = parsed.Kyc.Phone.Trim();
                profile.AddressLine1 = parsed.Kyc.AddressLine1.Trim();
                profile.City = parsed.Kyc.City.Trim();
                profile.County = parsed.Kyc.County.Trim();
                profile.Eircode = parsed.Kyc.Eircode.Trim();
                profile.Country = parsed.Kyc.Country.Trim();
                profile.Occupation = parsed.Kyc.Occupation.Trim();
                profile.EmployerName = parsed.Kyc.EmployerName.Trim();
                profile.SourceOfFunds = parsed.Kyc.SourceOfFunds.Trim();
                profile.IsPEP = parsed.Kyc.IsPEP;
                profile.RiskRating = (RiskRating)parsed.Kyc.RiskRating;
                profile.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }

            foreach (var f in parsed.Files.Where(x => !x.Name.Equals("kyc.json", StringComparison.OrdinalIgnoreCase)))
            {
                if (f.Bytes == null || f.Bytes.Length == 0) continue;

                var fileHash = Sha256Hex(f.Bytes);

                var docExists = await db.CkycDocuments.AnyAsync(d => d.FileHashSha256 == fileHash, ct);
                if (docExists) continue;

                profile.Documents.Add(new CkycDocument
                {
                    CkycDocumentId = Guid.NewGuid(),
                    CkycProfileId = profile.CkycProfileId,
                    DocumentType = DocumentType.Other,
                    FileName = f.Name,
                    ContentType = GuessContentType(f.Name),
                    FileSizeBytes = f.Bytes.LongLength,
                    FileHashSha256 = fileHash,
                    FileBytes = f.Bytes,
                    UploadedAtUtc = DateTimeOffset.UtcNow
                });
            }

            submission.LinkedCkycProfileId = profile.CkycProfileId;
            submission.CkycNumber = profile.CkycNumber;
            submission.Status = SubmissionStatus.Success;
            submission.StatusMessage = "Profile created/updated successfully.";
            submission.ProcessedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            Console.WriteLine($"Submission {submission.RequestRef} -> Success, CKYC={submission.CkycNumber}");
        }
        catch (Exception ex)
        {
            submission.Status = SubmissionStatus.Failed;
            submission.FailureReason = ex.Message;
            submission.StatusMessage = "Processing failed.";
            submission.ProcessedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            Console.WriteLine($"Submission {submission.RequestRef} -> Failed: {ex.Message}");
        }
    }

    private static (KycJson Kyc, List<(string Name, byte[] Bytes)> Files) ExtractKycJsonAndFiles(byte[] zipBytes)
    {
        using var ms = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);

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
                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                kyc = JsonSerializer.Deserialize<KycJson>(bytes, opts)
                    ?? throw new Exception("kyc.json is invalid JSON.");
            }
        }

        if (kyc != null &&
            (string.IsNullOrWhiteSpace(kyc.FirstName) ||
             string.IsNullOrWhiteSpace(kyc.MiddleName) ||
             string.IsNullOrWhiteSpace(kyc.LastName) ||
             string.IsNullOrWhiteSpace(kyc.PPSN) ||
             kyc.DateOfBirth == default))
        {
            throw new Exception("kyc.json missing required fields (firstName/middleName/lastName/ppsn/dateOfBirth).");
        }

        if (kyc == null) throw new Exception("kyc.json not found inside ZIP.");
        return (kyc, files);
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

    private static string Normalize(string s)
        => (s ?? "").Trim().ToUpperInvariant();

    private static string Sha256Hex(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed class KycJson
    {
        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public string LastName { get; set; } = "";
        public DateOnly DateOfBirth { get; set; }
        public string PPSN { get; set; } = "";

        public string Nationality { get; set; } = "Ireland";
        public string Gender { get; set; } = "NA";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";

        public string AddressLine1 { get; set; } = "";
        public string City { get; set; } = "";
        public string County { get; set; } = "";
        public string Eircode { get; set; } = "";
        public string Country { get; set; } = "Ireland";

        public string Occupation { get; set; } = "";
        public string EmployerName { get; set; } = "";
        public string SourceOfFunds { get; set; } = "";

        public bool IsPEP { get; set; }
        public short RiskRating { get; set; }
    }
}
using CKYC.Server.Data;
using CKYC.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Renci.SshNet;
using System.Security.Cryptography;

namespace CKYC.Server.Services;

public sealed class SftpInboundPuller : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SftpInboundOptions _opt;

    public SftpInboundPuller(IServiceScopeFactory scopeFactory, IOptions<SftpInboundOptions> opt)
    {
        _scopeFactory = scopeFactory;
        _opt = opt.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("SftpInboundPuller started (persistent connection).");

        SftpClient? sftp = null;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (sftp == null)
                    {
                        sftp = new SftpClient(_opt.Host, _opt.Port, _opt.Username, _opt.Password);
                    }

                    if (!sftp.IsConnected)
                    {
                        Console.WriteLine($"SFTP connecting to {_opt.Host}:{_opt.Port} as {_opt.Username} ...");
                        sftp.Connect();
                        Console.WriteLine("SFTP connected.");
                    }

                    EnsureRemoteDir(sftp, _opt.RemoteIncoming);
                    if (_opt.MoveRemoteAfterDownload)
                        EnsureRemoteDir(sftp, _opt.RemoteProcessed);

                    await TickOnce(sftp, stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SftpInboundPuller tick error: " + ex.Message);

                    try
                    {
                        if (sftp != null)
                        {
                            sftp.Disconnect();
                        }
                    }
                    catch { }

                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Max(2, _opt.PollSeconds)), stoppingToken);
            }
        }
        finally
        {
            try
            {
                if (sftp != null)
                {
                    Console.WriteLine("SFTP disconnecting (service stopping)...");
                    sftp.Disconnect();
                    sftp.Dispose();
                }
            }
            catch { }
        }
    }

    private async Task TickOnce(SftpClient sftp, CancellationToken ct)
    {
        var incoming = _opt.RemoteIncoming.Replace('\\', '/').TrimEnd('/');
        var entries = sftp.ListDirectory(incoming)
            .Where(e => e.IsRegularFile && e.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.LastWriteTimeUtc)
            .ToList();

        if (entries.Count == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CkycDbContext>();

        foreach (var file in entries)
        {
            ct.ThrowIfCancellationRequested();

            var remotePath = $"{incoming}/{file.Name}";

            byte[] zipBytes;
            using (var ms = new MemoryStream())
            {
                sftp.DownloadFile(remotePath, ms);
                zipBytes = ms.ToArray();
            }

            if (zipBytes.Length == 0) continue;

            var zipHash = Sha256Hex(zipBytes);

            var already = await db.InboundPackages
                .AsNoTracking()
                .AnyAsync(p => p.FileHashSha256 == zipHash, ct);

            if (already)
            {
                if (_opt.MoveRemoteAfterDownload)
                {
                    var processed = _opt.RemoteProcessed.Replace('\\', '/').TrimEnd('/');
                    var target = $"{processed}/{file.Name}";
                    SafeMoveRemote(sftp, remotePath, target);
                }
                continue;
            }

            var submission = new InboundSubmission
            {
                InboundSubmissionId = Guid.NewGuid(),
                RequestRef = Path.GetFileNameWithoutExtension(file.Name),
                Status = SubmissionStatus.Received,
                StatusMessage = "Received via SFTP puller.",
                FailureReason = null,
                ReceivedAtUtc = DateTime.UtcNow,
                ProcessedAtUtc = null,
                LinkedCkycProfileId = null,
                CkycNumber = null
            };

            var pkg = new InboundPackage
            {
                InboundPackageId = Guid.NewGuid(),
                InboundSubmissionId = submission.InboundSubmissionId,
                FileName = file.Name,
                FileHashSha256 = zipHash,
                FileSizeBytes = zipBytes.LongLength,
                ZipBytes = zipBytes,
                UploadedAtUtc = DateTime.UtcNow
            };

            submission.Packages.Add(pkg);
            db.InboundSubmissions.Add(submission);

            await db.SaveChangesAsync(ct);

            Console.WriteLine($"SFTP pulled ZIP => DB saved. file={file.Name} hash={zipHash}");

            if (_opt.MoveRemoteAfterDownload)
            {
                var processed = _opt.RemoteProcessed.Replace('\\', '/').TrimEnd('/');
                var target = $"{processed}/{file.Name}";
                SafeMoveRemote(sftp, remotePath, target);
            }
        }
    }

    private static void EnsureRemoteDir(SftpClient sftp, string path)
    {
        var p = path.Replace('\\', '/').TrimEnd('/');
        if (string.IsNullOrWhiteSpace(p) || p == "/") return;

        var parts = p.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var current = "";
        foreach (var part in parts)
        {
            current += "/" + part;
            if (!sftp.Exists(current))
                sftp.CreateDirectory(current);
        }
    }

    private static void SafeMoveRemote(SftpClient sftp, string from, string to)
    {
        try
        {
            if (sftp.Exists(to))
                sftp.DeleteFile(to);

            sftp.RenameFile(from, to);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Remote move failed: {from} -> {to} : {ex.Message}");
        }
    }

    private static string Sha256Hex(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class SftpInboundOptions
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 22;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string RemoteIncoming { get; set; } = "/incoming";
    public string RemoteProcessed { get; set; } = "/incoming/processed";
    public int PollSeconds { get; set; } = 5;
    public bool MoveRemoteAfterDownload { get; set; } = true;
}
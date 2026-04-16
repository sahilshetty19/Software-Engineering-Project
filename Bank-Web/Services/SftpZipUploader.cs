using Microsoft.Extensions.Options;
using Renci.SshNet;
using System.Text;

namespace Bank.Web.Services;

public sealed class SftpZipUploader
{
    private readonly SftpOptions _opt;

    public SftpZipUploader(IOptions<SftpOptions> opt)
    {
        _opt = opt.Value;
    }

    public Task<(bool ok, string remotePath, string error)> UploadBytesAsync(
        byte[] bytes,
        string fileName,
        CancellationToken ct = default)
    {
        if (bytes == null || bytes.Length == 0)
            return Task.FromResult((false, "", "ZIP bytes are empty."));

        if (string.IsNullOrWhiteSpace(fileName))
            return Task.FromResult((false, "", "File name is empty."));

        var remoteDir = NormalizeRemoteDir(_opt.RemoteIncomingDir);
        var remotePath = $"{remoteDir}/{SanitizeFileName(fileName)}";

        try
        {
            using var client = new SftpClient(_opt.Host, _opt.Port, _opt.Username, _opt.Password);
            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(15);

            client.Connect();
            EnsureRemoteDir(client, remoteDir);

            using var ms = new MemoryStream(bytes);
            client.UploadFile(ms, remotePath, true);

            client.Disconnect();

            return Task.FromResult((true, remotePath, ""));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, "", ex.Message));
        }
    }

    private static string NormalizeRemoteDir(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir)) return "/incoming";
        dir = dir.Replace("\\", "/").Trim();
        if (!dir.StartsWith("/")) dir = "/" + dir;
        return dir.TrimEnd('/');
    }

    private static string SanitizeFileName(string name)
    {
        name = name.Replace("\\", "_").Replace("/", "_").Trim();
        return string.IsNullOrWhiteSpace(name) ? "upload.zip" : name;
    }

    private static void EnsureRemoteDir(SftpClient client, string remoteDir)
    {
        var parts = remoteDir.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var cur = "/";
        foreach (var p in parts)
        {
            cur = cur == "/" ? "/" + p : cur + "/" + p;
            if (!client.Exists(cur))
                client.CreateDirectory(cur);
        }
    }
}
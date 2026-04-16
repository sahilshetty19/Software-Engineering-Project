namespace Bank.Web.Services;

public sealed class SftpOptions
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 22;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string RemoteIncomingDir { get; set; } = "/incoming";
}
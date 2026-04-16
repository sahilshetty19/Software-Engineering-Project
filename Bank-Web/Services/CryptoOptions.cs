namespace Bank.Web.Services;

public sealed class CryptoOptions
{
    public string AesKeyB64 { get; set; } = "";
    public string Aad { get; set; } = "";
}
namespace CKYC.Server.DTOs;

public sealed class EncryptedEnvelopeDto
{
    public string Alg { get; set; } = "AES-256-GCM";
    public string IvB64 { get; set; } = "";
    public string CiphertextB64 { get; set; } = "";
    public string TagB64 { get; set; } = "";
}
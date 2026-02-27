namespace CKYC.Server.DTOs;

public class CkycSearchResponse
{
    public bool Found { get; set; }
    public string Message { get; set; } = "";
    public string? CkycNumber { get; set; }   // filled only if Found = true
}
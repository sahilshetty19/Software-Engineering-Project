namespace CKYC.Server.DTOs;

public class CkycUploadStatusDto
{
    public string RequestRef { get; set; } = "";
    public string Status { get; set; } = ""; 
    public string Message { get; set; } = "";
    public string? CkycNumber { get; set; }
}
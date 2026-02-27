namespace CKYC.Server.DTOs;

public class CkycZipUploadReceiptDto
{
    public bool Received { get; set; }
    public string Message { get; set; } = "";
    public string SubmissionRef { get; set; } = "";
}
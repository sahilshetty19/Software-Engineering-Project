using Microsoft.AspNetCore.Http;

namespace CKYC.Server.DTOs;

public class CkycZipUploadRequestDto
{
    public IFormFile ZipFile { get; set; } = null!;
    public string BankCode { get; set; } = "";
    public string RequestRef { get; set; } = "";
}
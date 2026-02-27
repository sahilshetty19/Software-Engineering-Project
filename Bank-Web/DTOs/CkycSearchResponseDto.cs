namespace Bank.Web.DTOs;

public sealed class CkycSearchResponseDto
{
    public bool Found { get; set; }
    public string Message { get; set; } = "";
    public string? CkycNumber { get; set; }
}
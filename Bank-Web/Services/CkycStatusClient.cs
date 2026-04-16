using System.Net;
using System.Net.Http.Json;

namespace Bank.Web.Services;

public sealed class CkycStatusClient
{
    private readonly IHttpClientFactory _factory;

    public CkycStatusClient(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<CkycInboundStatusDto?> GetInboundStatus(string requestRef, CancellationToken ct = default)
    {
        requestRef = (requestRef ?? "").Trim();
        if (requestRef.Length == 0) return null;

        var http = _factory.CreateClient("CKYC");

        var url = $"/api/inbound/status/{Uri.EscapeDataString(requestRef)}";
        var res = await http.GetAsync(url, ct);

        if (res.StatusCode == HttpStatusCode.NotFound) return null;

        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<CkycInboundStatusDto>(cancellationToken: ct);
    }
}

public sealed class CkycInboundStatusDto
{
    public string RequestRef { get; set; } = "";
    public int StatusCode { get; set; }
    public string Status { get; set; } = "";
    public string? StatusMessage { get; set; }
    public string? FailureReason { get; set; }
    public string? CkycNumber { get; set; }
    public DateTimeOffset? ReceivedAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
}
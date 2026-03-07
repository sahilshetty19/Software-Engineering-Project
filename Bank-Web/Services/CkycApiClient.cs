using Bank.Web.DTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bank.Web.Services;

public sealed class CkycApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CkycApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(bool ok, string message, string submissionRef)> UploadZipAsync(
        byte[] zipBytes,
        string zipFileName,
        string bankCode,
        string requestRef,
        CancellationToken ct = default)
    {
        var http = _httpClientFactory.CreateClient("CKYC");

        using var form = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/zip");

        form.Add(fileContent, "ZipFile", zipFileName);
        form.Add(new StringContent(bankCode), "BankCode");
        form.Add(new StringContent(requestRef), "RequestRef");

        var resp = await http.PostAsync("/api/ckyc/uploads/zip", form, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            return (false, json, "");

        using var doc = JsonDocument.Parse(json);
        var received = doc.RootElement.GetProperty("received").GetBoolean();
        var msg = doc.RootElement.GetProperty("message").GetString() ?? "";
        var subRef = doc.RootElement.GetProperty("submissionRef").GetString() ?? "";

        return (received, msg, subRef);
    }

    public async Task<(string status, string message, string? ckycNumber)> GetStatusAsync(
        string requestRef,
        CancellationToken ct = default)
    {
        var http = _httpClientFactory.CreateClient("CKYC");

        var resp = await http.GetAsync($"/api/ckyc/uploads/{Uri.EscapeDataString(requestRef)}/status", ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            return ("Error", json, null);

        using var doc = JsonDocument.Parse(json);
        var status = doc.RootElement.GetProperty("status").GetString() ?? "";
        var message = doc.RootElement.GetProperty("message").GetString() ?? "";
        string? ckycNumber = null;

        if (doc.RootElement.TryGetProperty("ckycNumber", out var cn) && cn.ValueKind != JsonValueKind.Null)
            ckycNumber = cn.GetString();

        return (status, message, ckycNumber);
    }

    public async Task<string> DownloadProfileJsonAsync(string ckycNumber, CancellationToken ct = default)
    {
        var http = _httpClientFactory.CreateClient("CKYC");
        var resp = await http.GetAsync($"/api/ckyc/profiles/{Uri.EscapeDataString(ckycNumber)}", ct);
        return await resp.Content.ReadAsStringAsync(ct);
    }

    public async Task<(bool ok, string message, bool found, string? ckycNumber)> SearchAsync(
        string firstName,
        string middleName,
        string lastName,
        DateOnly dob,
        string ppsn,
        CancellationToken ct = default)
    {
        var http = _httpClientFactory.CreateClient("CKYC");

        var req = new CkycSearchRequestDto
        {
            FirstName = firstName,
            MiddleName = middleName,
            LastName = lastName,
            DateOfBirth = dob,
            PPSN = ppsn
        };

        using var resp = await http.PostAsJsonAsync("/api/ckyc/search", req, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            return (false, $"CKYC search failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}", false, null);
        }

        var data = await resp.Content.ReadFromJsonAsync<CkycSearchResponseDto>(cancellationToken: ct);
        if (data == null)
            return (false, "CKYC search returned empty response.", false, null);

        return (true, data.Message, data.Found, data.CkycNumber);
    }
}
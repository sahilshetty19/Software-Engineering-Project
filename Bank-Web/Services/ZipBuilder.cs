using System.IO.Compression;
using System.Text.Json;
using Bank.Web.Models;

namespace Bank.Web.Services;

public static class ZipBuilder
{
    public static byte[] BuildKycZip(KycUploadDetails upload)
    {
        using var ms = new MemoryStream();

        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var kyc = new
            {
                firstName = upload.FirstName,
                middleName = upload.MiddleName,
                lastName = upload.LastName,
                dateOfBirth = upload.DateOfBirth.ToString("yyyy-MM-dd"),
                ppsn = upload.PPSN,
                nationality = upload.Nationality,
                gender = upload.Gender,
                email = upload.Email,
                phone = upload.Phone,
                addressLine1 = upload.AddressLine1,
                city = upload.City?.CityName ?? "",
                county = upload.County?.CountyName ?? "",
                eircode = upload.Eircode,
                country = upload.Country,
                occupation = upload.Occupation,
                employerName = upload.EmployerName,
                sourceOfFunds = upload.SourceOfFunds,
                isPEP = upload.IsPEP,
                riskRating = (short)upload.RiskRating
            };

            var kycJson = JsonSerializer.Serialize(kyc, new JsonSerializerOptions { WriteIndented = true });

            var kycEntry = zip.CreateEntry("kyc.json");
            using (var entryStream = kycEntry.Open())
            using (var writer = new StreamWriter(entryStream))
            {
                writer.Write(kycJson);
            }

            foreach (var img in upload.Images)
            {
                if (img.ImageBytes == null || img.ImageBytes.Length == 0) continue;

                var ext = Path.GetExtension(img.FileName ?? "").Trim();
                if (string.IsNullOrWhiteSpace(ext))
                    ext = GuessExtFromContentType(img.ContentType);

                var entryName = img.DocumentType switch
                {
                    DocumentType.PSCFront => $"PSCFront{ext}",
                    DocumentType.PSCBack => $"PSCBack{ext}",
                    _ => $"{img.DocumentType}_{img.KycUploadImageId}{ext}"
                };

                var docEntry = zip.CreateEntry(entryName);
                using var entryStream = docEntry.Open();
                entryStream.Write(img.ImageBytes, 0, img.ImageBytes.Length);
            }
        }

        return ms.ToArray();
    }

    private static string GuessExtFromContentType(string? contentType)
    {
        var ct = (contentType ?? "").ToLowerInvariant();
        if (ct.Contains("jpeg")) return ".jpg";
        if (ct.Contains("png")) return ".png";
        if (ct.Contains("pdf")) return ".pdf";
        return ".bin";
    }
}
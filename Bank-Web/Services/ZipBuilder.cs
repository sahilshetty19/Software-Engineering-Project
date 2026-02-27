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
            // 1) kyc.json (camelCase keys; CKYC processor supports it)
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
                writer.Write(kycJson);

            // 2) documents
            foreach (var img in upload.Images)
            {
                var safeName = string.IsNullOrWhiteSpace(img.FileName)
                    ? $"{img.KycUploadImageId}.bin"
                    : img.FileName;

                var docEntry = zip.CreateEntry(safeName);
                using var entryStream = docEntry.Open();
                entryStream.Write(img.ImageBytes, 0, img.ImageBytes.Length);
            }
        }

        return ms.ToArray();
    }
}
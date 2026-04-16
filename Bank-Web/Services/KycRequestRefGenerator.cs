using Bank.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Bank.Web.Services;

public static class KycRequestRefGenerator
{
    public static async Task<string> GenerateAsync(BankDbContext context, CancellationToken cancellationToken = default)
    {
        var prefix = $"KYC-{DateTime.UtcNow:yyyyMMdd}-";

        var latestRequestRef = await context.KycUploadDetails
            .AsNoTracking()
            .Where(x => x.RequestRef.StartsWith(prefix))
            .OrderByDescending(x => x.RequestRef)
            .Select(x => x.RequestRef)
            .FirstOrDefaultAsync(cancellationToken);

        var nextSequence = 1;

        if (!string.IsNullOrWhiteSpace(latestRequestRef) &&
            latestRequestRef.Length > prefix.Length &&
            int.TryParse(latestRequestRef[prefix.Length..], out var currentSequence))
        {
            nextSequence = currentSequence + 1;
        }

        return $"{prefix}{nextSequence:D6}";
    }
}

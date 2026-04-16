using Bank.Web.Data;
using Bank.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bank.Web.Services.Automation;

public sealed class KycAutomationRetryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AutomationRetryOptions _options;

    public KycAutomationRetryWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<AutomationRetryOptions> options)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_options.Enabled)
                {
                    await ProcessDueRecordsAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("KycAutomationRetryWorker error: " + ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, _options.PollSeconds)), stoppingToken);
        }
    }

    private async Task ProcessDueRecordsAsync(CancellationToken ct)
    {
        List<Guid> dueRecordIds;

        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
            var now = DateTimeOffset.UtcNow;

            dueRecordIds = await db.KycUploadDetails
                .AsNoTracking()
                .Where(x =>
                    x.AutomationStatus == AutomationRunStatus.Queued ||
                    (x.AutomationStatus == AutomationRunStatus.WaitingRetry &&
                     x.NextRetryAtUtc != null &&
                     x.NextRetryAtUtc <= now))
                .Where(x => x.Status != KycWorkflowStatus.Completed && x.Status != KycWorkflowStatus.KycDone)
                .Where(x => x.AutomationLockedUntilUtc == null || x.AutomationLockedUntilUtc < now)
                .OrderBy(x => x.NextRetryAtUtc ?? x.CreatedAtUtc)
                .Select(x => x.KycUploadId)
                .Take(Math.Max(1, _options.BatchSize))
                .ToListAsync(ct);
        }

        foreach (var recordId in dueRecordIds)
        {
            ct.ThrowIfCancellationRequested();

            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<KycAutomationService>();
            await service.ProcessRecordAsync(recordId, ct);
        }
    }
}

namespace Bank.Web.Services.Automation;

public sealed class AutomationRetryOptions
{
    public bool Enabled { get; set; } = true;
    public int PollSeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 10;
    public int DefaultMaxRetryAttempts { get; set; } = 5;
    public int[] RetryDelaySeconds { get; set; } = new[] { 60, 300, 900, 1800, 3600 };
}

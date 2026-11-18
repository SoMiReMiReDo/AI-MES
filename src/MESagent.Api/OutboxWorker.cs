using System.Diagnostics.Metrics;
using MESagent.Infrastructure.ApplicationAuditReliability;

namespace MESagent.Api;

internal sealed class OutboxWorker(
    IServiceScopeFactory scopes, WorkbenchSignals signals, ILogger<OutboxWorker> logger) : BackgroundService
{
    private static readonly Meter Meter = new("MESagent.Outbox");
    private static readonly Counter<long> Attempts = Meter.CreateCounter<long>("mes.outbox.attempts");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var databaseUnavailable = false;
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeSpan.FromMilliseconds(500);
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var result = await scope.ServiceProvider.GetRequiredService<OutboxDelivery>()
                    .DeliverNext(signals.Publish, stoppingToken);
                if (databaseUnavailable)
                {
                    databaseUnavailable = false;
                    logger.LogInformation("Outbox polling recovered");
                }
                if (result != null)
                {
                    Attempts.Add(1, new KeyValuePair<string, object?>("outcome", result.Outcome));
                    if (result.ErrorCode != null)
                        logger.LogWarning(
                            "Outbox {Outcome} {MessageId} trace {TraceId} attempt {Attempt} code {ErrorCode}",
                            result.Outcome, result.Id, result.CorrelationId, result.Attempts, result.ErrorCode);
                    continue;
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                if (!databaseUnavailable)
                    logger.LogWarning("Outbox polling unavailable ({ErrorType})", ex.GetType().Name);
                databaseUnavailable = true;
                delay = TimeSpan.FromSeconds(5);
            }
            await Task.Delay(delay, stoppingToken);
        }
    }
}

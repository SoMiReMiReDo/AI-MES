using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using MESagent.Application;
using Microsoft.AspNetCore.SignalR;

namespace MESagent.Api;

internal sealed class WorkbenchSignals
{
    private readonly object sync = new();
    private readonly Dictionary<string, long> revisions = new();
    private readonly HashSet<Guid> recent = [];
    private readonly Queue<Guid> recentOrder = new();
    private TaskCompletionSource pulse = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public (Dictionary<string, long> Revisions, Task Pulse) Subscribe()
    {
        lock (sync)
            return (new(revisions), pulse.Task);
    }

    public void Publish(Guid eventId, string[] sources)
    {
        lock (sync)
        {
            if (!recent.Add(eventId))
                return;
            recentOrder.Enqueue(eventId);
            if (recentOrder.Count > 1024)
                recent.Remove(recentOrder.Dequeue());
            foreach (var source in sources.Distinct())
                revisions[source] = revisions.GetValueOrDefault(source) + 1;
            pulse.TrySetResult();
            pulse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}

internal sealed class WorkbenchLiveHub(WorkbenchSignals signals, IServiceScopeFactory scopes) : Hub
{
    private const string WatchKey = "WatchingWorkbench";

    public override async Task OnConnectedAsync()
    {
        if ((await Access()).Length == 0)
            Context.Abort();
        else
            await base.OnConnectedAsync();
    }

    public async IAsyncEnumerable<bool> Watch([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        lock (Context.Items)
        {
            if (!Context.Items.TryAdd(WatchKey, true))
                throw new HubException("每个连接只允许一个工作台订阅");
        }

        try
        {
            using var cancel = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, Context.ConnectionAborted);
            var token = cancel.Token;
            var previous = new Dictionary<string, long>();
            var access = Array.Empty<string>();
            var recheckAt = DateTime.UtcNow;
            while (!token.IsCancellationRequested)
            {
                var current = signals.Subscribe();
                var now = DateTime.UtcNow;
                var periodic = now >= recheckAt;
                if (periodic || access.Any(p =>
                    current.Revisions.GetValueOrDefault(p) != previous.GetValueOrDefault(p)))
                {
                    var next = await Access();
                    if (next.Length == 0)
                    {
                        Context.Abort();
                        yield break;
                    }
                    if (periodic || next.Any(p =>
                        current.Revisions.GetValueOrDefault(p) != previous.GetValueOrDefault(p))
                        || !access.SequenceEqual(next))
                        yield return true;

                    access = next;
                    if (periodic)
                        recheckAt = now.AddSeconds(15);
                }
                previous = current.Revisions;
                var remaining = recheckAt - DateTime.UtcNow;
                if (remaining > TimeSpan.Zero)
                {
                    try
                    {
                        await current.Pulse.WaitAsync(remaining, token);
                    }
                    catch (TimeoutException)
                    {
                        // Recheck session and time-based alert visibility.
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        // A disposed stream or disconnected client ends the loop.
                    }
                }
            }
        }
        finally
        {
            lock (Context.Items)
                Context.Items.Remove(WatchKey);
        }
    }

    private async Task<string[]> Access()
    {
        if (!Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var user)
            || !Guid.TryParse(Context.User?.FindFirstValue("sid"), out var session))
            return [];

        await using var scope = scopes.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<OrganizationIdentityAccessControlService>();
        using var body = JsonDocument.Parse("{}");
        try
        {
            var access = await service.Execute(new(user, session, Guid.NewGuid()),
                new("GET", "workbench/live-access", body.RootElement, new Dictionary<string, string>(), null));
            return (string[])access.Data!;
        }
        catch (AppException ex) when (ex.Status is 401 or 403)
        {
            return [];
        }
    }
}

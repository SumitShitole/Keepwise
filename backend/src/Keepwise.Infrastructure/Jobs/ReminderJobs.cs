using Hangfire;
using Keepwise.Application.Reminders;

namespace Keepwise.Infrastructure.Jobs;

public sealed class ReminderJobs(ReminderEngine engine)
{
    [DisableConcurrentExecution(timeoutInSeconds: 60 * 10)]
    public Task Generate(CancellationToken cancellationToken) => engine.GenerateAsync(cancellationToken);

    [DisableConcurrentExecution(timeoutInSeconds: 60 * 10)]
    public Task Dispatch(CancellationToken cancellationToken) => engine.DispatchDueAsync(cancellationToken);

    [DisableConcurrentExecution(timeoutInSeconds: 60 * 10)]
    public Task RefreshStatuses(CancellationToken cancellationToken) => engine.RefreshStatusesAsync(cancellationToken);
}

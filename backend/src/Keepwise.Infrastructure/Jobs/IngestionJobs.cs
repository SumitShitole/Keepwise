using Hangfire;
using Keepwise.Application.Ingestion;

namespace Keepwise.Infrastructure.Jobs;

public sealed class IngestionJobs(ExtractionPipeline pipeline)
{
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 60 * 5)]
    public Task Process(Guid jobId, CancellationToken cancellationToken) =>
        pipeline.ProcessJobAsync(jobId, cancellationToken);
}

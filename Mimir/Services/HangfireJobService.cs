using Hangfire;
using Mimir.HangfireWorker.Jobs;

namespace Mimir.Services;

public class HangfireJobService : IHangfireJobService
{
    public void EnqueueAgentDataCompletion(string address)
    {
        BackgroundJob.Enqueue<DataCompletionJobs>(x => x.CompleteAgentData(address));
    }

    public void EnqueueAvatarDataCompletion(string address)
    {
        BackgroundJob.Enqueue<DataCompletionJobs>(x => x.CompleteAvatarData(address));
    }

    public void EnqueueBatchAgentDataCompletion(List<string> addresses)
    {
        BackgroundJob.Enqueue<DataCompletionJobs>(x => x.BatchCompleteAgentData(addresses));
    }

    public void EnqueueBatchAvatarDataCompletion(List<string> addresses)
    {
        BackgroundJob.Enqueue<DataCompletionJobs>(x => x.BatchCompleteAvatarData(addresses));
    }
} 
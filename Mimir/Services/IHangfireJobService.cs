namespace Mimir.Services;

public interface IHangfireJobService
{
    void EnqueueAgentDataCompletion(string address);
    void EnqueueAvatarDataCompletion(string address);
    void EnqueueBatchAgentDataCompletion(List<string> addresses);
    void EnqueueBatchAvatarDataCompletion(List<string> addresses);
} 
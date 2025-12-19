
namespace Tolltech.Counter;

public class CounterService(CounterHandler counterHandler) : ICounterService
{
    private static readonly object locker = new();

    public Task Increment(string userName, long chatId, int delta)
    {
        lock (locker)
        {
            var existent = counterHandler.Find(userName, chatId);
            if (existent == null)
            {
                counterHandler.Create(new CounterDbo
                {
                    Id = CounterDbo.GetId(userName, chatId),
                    UserName = userName,
                    ChatId = chatId,
                    Counter = delta,
                    Timestamp = DateTime.UtcNow.Ticks,
                });
            }
            else
            {
                existent.Counter += delta;
                counterHandler.UpdateCounter(existent.Id, existent.Counter + delta);
            }

            return Task.CompletedTask;
        }
    }

    public Task<int?> GetCounter(string userName, long chatId)
    {
        var existent = counterHandler.Find(userName, chatId);
        return Task.FromResult(existent?.Counter);
    }

    public Task<(string Username, int Score)[]> GetCounters(long chatId)
    {
        var counters = counterHandler.Select(chatId);
        return Task.FromResult(counters.Select(x => (x.UserName, x.Counter)).ToArray());
    }
}
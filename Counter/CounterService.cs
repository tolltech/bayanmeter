using Tolltech.SqlEF;

namespace Tolltech.Counter;

public class CounterService(IQueryExecutorFactory queryExecutorFactory) : ICounterService
{
    private static readonly object locker = new();

    public Task Increment(string userName, long chatId, int delta)
    {
        lock (locker)
        {
            var queryExecutor = queryExecutorFactory.Create<CounterHandler, CounterDbo>();
            var existent = queryExecutor.Execute(f => f.Find(userName, chatId));
            if (existent == null)
            {
                queryExecutor.Execute(f => f.Create(new CounterDbo
                {
                    Id = CounterDbo.GetId(userName, chatId),
                    UserName = userName,
                    ChatId = chatId,
                    Counter = delta,
                    Timestamp = DateTime.UtcNow.Ticks,
                }));
            }
            else
            {
                existent.Counter += delta;
                queryExecutor.Execute(f => f.Update());
            }

            return Task.CompletedTask;
        }
    }

    public Task<int?> GetCounter(string userName, long chatId)
    {
        var queryExecutor = queryExecutorFactory.Create<CounterHandler, CounterDbo>();
        var existent = queryExecutor.Execute(f => f.Find(userName, chatId));
        return Task.FromResult(existent?.Counter);
    }
}